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

public class StoreGeneratedMongoTest(StoreGeneratedMongoTest.StoreGeneratedMongoFixture fixture)
    : StoreGeneratedTestBase<StoreGeneratedMongoTest.StoreGeneratedMongoFixture>(fixture), IAsyncLifetime
{
    // The tests write to the shared database and there is no rollback, so which test sees which rows depends on the order they run in.
    public async LifetimeTask InitializeAsync()
        => await Fixture.ReseedAsync();

    public LifetimeTask DisposeAsync()
        => LifetimeTask.CompletedTask;

#if EF11
    private readonly object? _ef11Skips = Ef11TestSkips.SkipIfListed("xUnit v3 (EF11) cannot hide this non-virtual upstream test; it fails here for the reasons in EF-X600 (see the New-method override on EF8 to EF10)", "Change_state_of_entity_with_temp_non_key_does_not_throw", "Clearing_optional_FK_does_not_leave_temporary_value");
#endif

    public class StoreGeneratedMongoFixture : StoreGeneratedFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("StoreGenerated");

        public TestServer TestServer { get; private set; } = null!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true, temporaryIntegerKeys: e => e.ClrType.Name == "Gumball");
            MongoSpecModelHelpers.RelaxNullableUniqueIndexes(modelBuilder);
            modelBuilder.RelaxUniqueForeignKeyIndexes();
        }
    }

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Other unexpected exception from query execution EF-X605 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task After_save_ignore_ignores_value_even_if_modified(string propertyName, string expectedValue)
        => SpecFails.AnyOutcomeAsync(() => base.After_save_ignore_ignores_value_even_if_modified(propertyName, expectedValue));
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task After_save_ignore_ignores_value_even_if_modified(string propertyName, string expectedValue)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.After_save_ignore_ignores_value_even_if_modified(propertyName, expectedValue));
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void After_save_ignore_ignores_value_even_if_modified(string propertyName, string expectedValue)
        => SpecFails.Throws<global::Xunit.Sdk.EqualException>(() => base.After_save_ignore_ignores_value_even_if_modified(propertyName, expectedValue));
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task After_save_ignore_ignores_value_if_not_modified(string propertyName, string expectedValue)
        => SpecFails.AnyOutcomeAsync(() => base.After_save_ignore_ignores_value_if_not_modified(propertyName, expectedValue), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task After_save_ignore_ignores_value_if_not_modified(string propertyName, string expectedValue)
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.After_save_ignore_ignores_value_if_not_modified(propertyName, expectedValue), string.Empty);
#endif
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void After_save_ignore_ignores_value_if_not_modified(string propertyName, string expectedValue)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.After_save_ignore_ignores_value_if_not_modified(propertyName, expectedValue), string.Empty);
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task After_save_throw_ignores_value_if_not_modified(string propertyName, string expectedValue)
        => SpecFails.AnyOutcomeAsync(() => base.After_save_throw_ignores_value_if_not_modified(propertyName, expectedValue), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task After_save_throw_ignores_value_if_not_modified(string propertyName, string expectedValue)
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.After_save_throw_ignores_value_if_not_modified(propertyName, expectedValue), string.Empty);
#endif
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void After_save_throw_ignores_value_if_not_modified(string propertyName, string expectedValue)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.After_save_throw_ignores_value_if_not_modified(propertyName, expectedValue), string.Empty);
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task After_save_use_ignores_value_if_not_modified(string propertyName, string expectedValue)
        => SpecFails.AnyOutcomeAsync(() => base.After_save_use_ignores_value_if_not_modified(propertyName, expectedValue), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task After_save_use_ignores_value_if_not_modified(string propertyName, string expectedValue)
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.After_save_use_ignores_value_if_not_modified(propertyName, expectedValue), string.Empty);
#endif
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void After_save_use_ignores_value_if_not_modified(string propertyName, string expectedValue)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.After_save_use_ignores_value_if_not_modified(propertyName, expectedValue), string.Empty);
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task After_save_use_uses_value_if_modified(string propertyName, string expectedValue)
        => SpecFails.AnyOutcomeAsync(() => base.After_save_use_uses_value_if_modified(propertyName, expectedValue), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#else
    public override Task After_save_use_uses_value_if_modified(string propertyName, string expectedValue)
        => base.After_save_use_uses_value_if_modified(propertyName, expectedValue);
#endif
#elif EF8
    public override void After_save_use_uses_value_if_modified(string propertyName, string expectedValue)
        => base.After_save_use_uses_value_if_modified(propertyName, expectedValue);
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Always_computed_property_on_Added_entity_with_default_value_gets_value_from_store()
        => SpecFails.AnyOutcomeAsync(() => base.Always_computed_property_on_Added_entity_with_default_value_gets_value_from_store(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Always_computed_property_on_Added_entity_with_default_value_gets_value_from_store()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Always_computed_property_on_Added_entity_with_default_value_gets_value_from_store());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Always_computed_property_on_Added_entity_with_default_value_gets_value_from_store()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Always_computed_property_on_Added_entity_with_default_value_gets_value_from_store());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Always_computed_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set()
        => SpecFails.AnyOutcomeAsync(() => base.Always_computed_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Always_computed_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Always_computed_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set());
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Always_computed_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set()
        => SpecFails.Throws<global::Xunit.Sdk.EqualException>(() => base.Always_computed_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Always_computed_property_on_Added_entity_with_temporary_value_gets_value_from_store()
        => SpecFails.AnyOutcomeAsync(() => base.Always_computed_property_on_Added_entity_with_temporary_value_gets_value_from_store(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Always_computed_property_on_Added_entity_with_temporary_value_gets_value_from_store()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Always_computed_property_on_Added_entity_with_temporary_value_gets_value_from_store());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Always_computed_property_on_Added_entity_with_temporary_value_gets_value_from_store()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Always_computed_property_on_Added_entity_with_temporary_value_gets_value_from_store());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Always_computed_property_on_Modified_entity_is_read_from_store_when_not_modified()
        => SpecFails.AnyOutcomeAsync(() => base.Always_computed_property_on_Modified_entity_is_read_from_store_when_not_modified(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Always_computed_property_on_Modified_entity_is_read_from_store_when_not_modified()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Always_computed_property_on_Modified_entity_is_read_from_store_when_not_modified());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Always_computed_property_on_Modified_entity_is_read_from_store_when_not_modified()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Always_computed_property_on_Modified_entity_is_read_from_store_when_not_modified());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Always_computed_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state()
        => SpecFails.AnyOutcomeAsync(() => base.Always_computed_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Always_computed_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Always_computed_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Always_computed_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Always_computed_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Always_identity_property_on_Added_entity_with_default_value_gets_value_from_store()
        => SpecFails.AnyOutcomeAsync(() => base.Always_identity_property_on_Added_entity_with_default_value_gets_value_from_store(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Always_identity_property_on_Added_entity_with_default_value_gets_value_from_store()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Always_identity_property_on_Added_entity_with_default_value_gets_value_from_store());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Always_identity_property_on_Added_entity_with_default_value_gets_value_from_store()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Always_identity_property_on_Added_entity_with_default_value_gets_value_from_store());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Always_identity_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set()
        => SpecFails.AnyOutcomeAsync(() => base.Always_identity_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Always_identity_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Always_identity_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set());
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Always_identity_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set()
        => SpecFails.Throws<global::Xunit.Sdk.EqualException>(() => base.Always_identity_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Always_identity_property_on_Added_entity_with_temporary_value_gets_value_from_store()
        => SpecFails.AnyOutcomeAsync(() => base.Always_identity_property_on_Added_entity_with_temporary_value_gets_value_from_store(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Always_identity_property_on_Added_entity_with_temporary_value_gets_value_from_store()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Always_identity_property_on_Added_entity_with_temporary_value_gets_value_from_store());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Always_identity_property_on_Added_entity_with_temporary_value_gets_value_from_store()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Always_identity_property_on_Added_entity_with_temporary_value_gets_value_from_store());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Always_identity_property_on_Modified_entity_is_not_included_in_the_update_when_not_modified()
        => SpecFails.AnyOutcomeAsync(() => base.Always_identity_property_on_Modified_entity_is_not_included_in_the_update_when_not_modified(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Always_identity_property_on_Modified_entity_is_not_included_in_the_update_when_not_modified()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Always_identity_property_on_Modified_entity_is_not_included_in_the_update_when_not_modified());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Always_identity_property_on_Modified_entity_is_not_included_in_the_update_when_not_modified()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Always_identity_property_on_Modified_entity_is_not_included_in_the_update_when_not_modified());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Always_identity_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state()
        => SpecFails.AnyOutcomeAsync(() => base.Always_identity_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Always_identity_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Always_identity_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Always_identity_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Always_identity_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Before_save_ignore_ignores_value_even_if_set(string propertyName, string expectedValue)
        => SpecFails.AnyOutcomeAsync(() => base.Before_save_ignore_ignores_value_even_if_set(propertyName, expectedValue), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Before_save_ignore_ignores_value_even_if_set(string propertyName, string expectedValue)
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Before_save_ignore_ignores_value_even_if_set(propertyName, expectedValue));
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Before_save_ignore_ignores_value_even_if_set(string propertyName, string expectedValue)
        => SpecFails.Throws<global::Xunit.Sdk.EqualException>(() => base.Before_save_ignore_ignores_value_even_if_set(propertyName, expectedValue));
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Before_save_ignore_ignores_value_if_not_set(string propertyName, string expectedValue)
        => SpecFails.AnyOutcomeAsync(() => base.Before_save_ignore_ignores_value_if_not_set(propertyName, expectedValue), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Before_save_ignore_ignores_value_if_not_set(string propertyName, string expectedValue)
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Before_save_ignore_ignores_value_if_not_set(propertyName, expectedValue), string.Empty);
#endif
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Before_save_ignore_ignores_value_if_not_set(string propertyName, string expectedValue)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Before_save_ignore_ignores_value_if_not_set(propertyName, expectedValue), string.Empty);
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Before_save_throw_ignores_value_if_not_set(string propertyName, string expectedValue)
        => SpecFails.AnyOutcomeAsync(() => base.Before_save_throw_ignores_value_if_not_set(propertyName, expectedValue), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Before_save_throw_ignores_value_if_not_set(string propertyName, string expectedValue)
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Before_save_throw_ignores_value_if_not_set(propertyName, expectedValue), string.Empty);
#endif
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Before_save_throw_ignores_value_if_not_set(string propertyName, string expectedValue)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Before_save_throw_ignores_value_if_not_set(propertyName, expectedValue), string.Empty);
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Bulk write failure (duplicate _id) when saving entities with default int keys EF-X105 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Before_save_use_always_uses_value_if_set(string propertyName)
        => SpecFails.AnyOutcomeAsync(() => base.Before_save_use_always_uses_value_if_set(propertyName), "A bulk write operation resulted in one or more errors. WriteErrors: [ { Category : \"Duplic");
#else
    public override Task Before_save_use_always_uses_value_if_set(string propertyName)
        => base.Before_save_use_always_uses_value_if_set(propertyName);
#endif
#elif EF8
    public override void Before_save_use_always_uses_value_if_set(string propertyName)
        => base.Before_save_use_always_uses_value_if_set(propertyName);
#endif

#if (EF10 || EF11) || EF9
#if EF11
#if EF11
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Before_save_use_ignores_value_if_not_set(string propertyName, string expectedValue)
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Before_save_use_ignores_value_if_not_set(propertyName, expectedValue), string.Empty);
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Before_save_use_ignores_value_if_not_set(string propertyName, string expectedValue)
        => SpecFails.ThrowsAsync<global::MongoDB.Driver.MongoBulkWriteException<global::MongoDB.Bson.BsonDocument>>(() => base.Before_save_use_ignores_value_if_not_set(propertyName, expectedValue));
#endif
#else
    // Fails: Other unexpected exception from query execution EF-X605
    public override Task Before_save_use_ignores_value_if_not_set(string propertyName, string expectedValue)
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Before_save_use_ignores_value_if_not_set(propertyName, expectedValue), string.Empty);
#endif
#elif EF8
    // Fails: Other unexpected exception from query execution EF-X605
    public override void Before_save_use_ignores_value_if_not_set(string propertyName, string expectedValue)
        => SpecFails.SyncSomeRows<global::Xunit.Sdk.EqualException>(() => base.Before_save_use_ignores_value_if_not_set(propertyName, expectedValue), string.Empty);
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Store-generated/computed value or update semantics differ (no database defaults, computed columns or generated keys) EF-X108 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X108")]
    public new Task Change_state_of_entity_with_temp_non_key_does_not_throw(global::Microsoft.EntityFrameworkCore.EntityState targetState)
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF8
#if !EF11
    // Fails: Store-generated/computed value or update semantics differ (no database defaults, computed columns or generated keys) EF-X108 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalTheory(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X108")]
    public new void Change_state_of_entity_with_temp_non_key_does_not_throw(global::Microsoft.EntityFrameworkCore.EntityState targetState)
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Store-generated/computed value or update semantics differ (no database defaults, computed columns or generated keys) EF-X108 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X108")]
    public new Task Clearing_optional_FK_does_not_leave_temporary_value()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF8
#if !EF11
    // Fails: Store-generated/computed value or update semantics differ (no database defaults, computed columns or generated keys) EF-X108 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X108")]
    public new void Clearing_optional_FK_does_not_leave_temporary_value()
    { }
#pragma warning restore xUnit1024, xUnit1003
#endif
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Computed_property_on_Added_entity_can_have_value_set_explicitly()
        => SpecFails.AnyOutcomeAsync(() => base.Computed_property_on_Added_entity_can_have_value_set_explicitly(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Computed_property_on_Added_entity_can_have_value_set_explicitly()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Computed_property_on_Added_entity_can_have_value_set_explicitly());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Computed_property_on_Added_entity_can_have_value_set_explicitly()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Computed_property_on_Added_entity_can_have_value_set_explicitly());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Computed_property_on_Added_entity_with_default_value_gets_value_from_store()
        => SpecFails.AnyOutcomeAsync(() => base.Computed_property_on_Added_entity_with_default_value_gets_value_from_store(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Computed_property_on_Added_entity_with_default_value_gets_value_from_store()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Computed_property_on_Added_entity_with_default_value_gets_value_from_store());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Computed_property_on_Added_entity_with_default_value_gets_value_from_store()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Computed_property_on_Added_entity_with_default_value_gets_value_from_store());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Computed_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set()
        => SpecFails.AnyOutcomeAsync(() => base.Computed_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Computed_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Computed_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set());
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Computed_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set()
        => SpecFails.Throws<global::Xunit.Sdk.EqualException>(() => base.Computed_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Computed_property_on_Added_entity_with_temporary_value_gets_value_from_store()
        => SpecFails.AnyOutcomeAsync(() => base.Computed_property_on_Added_entity_with_temporary_value_gets_value_from_store(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Computed_property_on_Added_entity_with_temporary_value_gets_value_from_store()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Computed_property_on_Added_entity_with_temporary_value_gets_value_from_store());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Computed_property_on_Added_entity_with_temporary_value_gets_value_from_store()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Computed_property_on_Added_entity_with_temporary_value_gets_value_from_store());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Computed_property_on_Modified_entity_is_included_in_update_when_modified()
        => SpecFails.AnyOutcomeAsync(() => base.Computed_property_on_Modified_entity_is_included_in_update_when_modified(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Computed_property_on_Modified_entity_is_included_in_update_when_modified()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Computed_property_on_Modified_entity_is_included_in_update_when_modified());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Computed_property_on_Modified_entity_is_included_in_update_when_modified()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Computed_property_on_Modified_entity_is_included_in_update_when_modified());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Computed_property_on_Modified_entity_is_read_from_store_when_not_modified()
        => SpecFails.AnyOutcomeAsync(() => base.Computed_property_on_Modified_entity_is_read_from_store_when_not_modified(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Computed_property_on_Modified_entity_is_read_from_store_when_not_modified()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Computed_property_on_Modified_entity_is_read_from_store_when_not_modified());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Computed_property_on_Modified_entity_is_read_from_store_when_not_modified()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Computed_property_on_Modified_entity_is_read_from_store_when_not_modified());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Computed_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state()
        => SpecFails.AnyOutcomeAsync(() => base.Computed_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Computed_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Computed_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Computed_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Computed_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Store-generated/computed value or update semantics differ (no database defaults, computed columns or generated keys) EF-X108 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Fields_used_correctly_for_store_generated_values()
        => SpecFails.AnyOutcomeAsync(() => base.Fields_used_correctly_for_store_generated_values(), "Assert.Equal() Failure: Values differ");
#else
    // Fails: Store-generated/computed value or update semantics differ (no database defaults, computed columns or generated keys) EF-X108
    public override Task Fields_used_correctly_for_store_generated_values()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.XunitException>(() => base.Fields_used_correctly_for_store_generated_values(), "Assert.Equal() Failure: Values differ");
#endif
#elif EF8
    // Fails: Store-generated/computed value or update semantics differ (no database defaults, computed columns or generated keys) EF-X108
    public override void Fields_used_correctly_for_store_generated_values()
        => SpecFails.Throws<global::Xunit.Sdk.XunitException>(() => base.Fields_used_correctly_for_store_generated_values(), "Assert.Equal() Failure: Values differ");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Identity_key_with_read_only_before_save_throws_if_explicit_values_set()
        => SpecFails.AnyOutcomeAsync(() => base.Identity_key_with_read_only_before_save_throws_if_explicit_values_set(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    public override Task Identity_key_with_read_only_before_save_throws_if_explicit_values_set()
        => base.Identity_key_with_read_only_before_save_throws_if_explicit_values_set();
#endif
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Identity_property_on_Added_entity_can_have_value_set_explicitly()
        => SpecFails.AnyOutcomeAsync(() => base.Identity_property_on_Added_entity_can_have_value_set_explicitly(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Identity_property_on_Added_entity_can_have_value_set_explicitly()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Identity_property_on_Added_entity_can_have_value_set_explicitly());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Identity_property_on_Added_entity_can_have_value_set_explicitly()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Identity_property_on_Added_entity_can_have_value_set_explicitly());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Identity_property_on_Added_entity_with_default_value_gets_value_from_store()
        => SpecFails.AnyOutcomeAsync(() => base.Identity_property_on_Added_entity_with_default_value_gets_value_from_store(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Identity_property_on_Added_entity_with_default_value_gets_value_from_store()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Identity_property_on_Added_entity_with_default_value_gets_value_from_store());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Identity_property_on_Added_entity_with_default_value_gets_value_from_store()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Identity_property_on_Added_entity_with_default_value_gets_value_from_store());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Identity_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set()
        => SpecFails.AnyOutcomeAsync(() => base.Identity_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override Task Identity_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Identity_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set());
#endif
#elif EF8
    // Fails: Query translates natively but returns different data than the expected result EF-X600
    public override void Identity_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set()
        => SpecFails.Throws<global::Xunit.Sdk.EqualException>(() => base.Identity_property_on_Added_entity_with_read_only_before_save_throws_if_explicit_values_set());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Identity_property_on_Added_entity_with_temporary_value_gets_value_from_store()
        => SpecFails.AnyOutcomeAsync(() => base.Identity_property_on_Added_entity_with_temporary_value_gets_value_from_store(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Identity_property_on_Added_entity_with_temporary_value_gets_value_from_store()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Identity_property_on_Added_entity_with_temporary_value_gets_value_from_store());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Identity_property_on_Added_entity_with_temporary_value_gets_value_from_store()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Identity_property_on_Added_entity_with_temporary_value_gets_value_from_store());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Identity_property_on_Added_entity_with_temporary_value_gets_value_from_store_even_if_same()
        => SpecFails.AnyOutcomeAsync(() => base.Identity_property_on_Added_entity_with_temporary_value_gets_value_from_store_even_if_same(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Identity_property_on_Added_entity_with_temporary_value_gets_value_from_store_even_if_same()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Identity_property_on_Added_entity_with_temporary_value_gets_value_from_store_even_if_same());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Identity_property_on_Added_entity_with_temporary_value_gets_value_from_store_even_if_same()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Identity_property_on_Added_entity_with_temporary_value_gets_value_from_store_even_if_same());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Identity_property_on_Modified_entity_is_included_in_update_when_modified()
        => SpecFails.AnyOutcomeAsync(() => base.Identity_property_on_Modified_entity_is_included_in_update_when_modified(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Identity_property_on_Modified_entity_is_included_in_update_when_modified()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Identity_property_on_Modified_entity_is_included_in_update_when_modified());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Identity_property_on_Modified_entity_is_included_in_update_when_modified()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Identity_property_on_Modified_entity_is_included_in_update_when_modified());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Identity_property_on_Modified_entity_is_not_included_in_update_when_not_modified()
        => SpecFails.AnyOutcomeAsync(() => base.Identity_property_on_Modified_entity_is_not_included_in_update_when_not_modified(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Identity_property_on_Modified_entity_is_not_included_in_update_when_not_modified()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Identity_property_on_Modified_entity_is_not_included_in_update_when_not_modified());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Identity_property_on_Modified_entity_is_not_included_in_update_when_not_modified()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Identity_property_on_Modified_entity_is_not_included_in_update_when_not_modified());
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Identity_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state()
        => SpecFails.AnyOutcomeAsync(() => base.Identity_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state(), "The property 'Gumball.Id' does not have a value set and no value generator is available fo");
#else
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Identity_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Identity_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state());
#endif
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Identity_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Identity_property_on_Modified_entity_with_read_only_after_save_throws_if_value_is_in_modified_state());
#endif

#if EF9
    public override Task Insert_update_and_delete_with_GuidAsString_key()
        => base.Insert_update_and_delete_with_GuidAsString_key();
#elif EF8
    public override void Insert_update_and_delete_with_GuidAsString_key()
        => base.Insert_update_and_delete_with_GuidAsString_key();
#endif

#if EF9
    public override Task Insert_update_and_delete_with_StringAsGuid_key()
        => base.Insert_update_and_delete_with_StringAsGuid_key();
#elif EF8
    public override void Insert_update_and_delete_with_StringAsGuid_key()
        => base.Insert_update_and_delete_with_StringAsGuid_key();
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Principal key not generated so dependent key is null EF-X104 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Insert_update_and_delete_with_Uri_key()
        => SpecFails.AnyOutcomeAsync(() => base.Insert_update_and_delete_with_Uri_key(), "Unable to track an entity of type 'UriDependentOptional' because its primary key property ");
#else
    // Fails: Principal key not generated so dependent key is null EF-X104
    public override Task Insert_update_and_delete_with_Uri_key()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Insert_update_and_delete_with_Uri_key(), "Unable to track an entity of type 'UriDependentOptional' because its primary key property ");
#endif
#elif EF8
    // Fails: Principal key not generated so dependent key is null EF-X104
    public override void Insert_update_and_delete_with_Uri_key()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Insert_update_and_delete_with_Uri_key(), "Unable to track an entity of type 'UriDependentOptional' because its primary key property ");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Insert_update_and_delete_with_enum_key()
        => SpecFails.AnyOutcomeAsync(() => base.Insert_update_and_delete_with_enum_key(), "The property 'EnumPrincipal.Id' does not have a value set and no value generator is availa");
#else
    // Fails: No value generator for int/long/enum/converted keys EF-X103
    public override Task Insert_update_and_delete_with_enum_key()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Insert_update_and_delete_with_enum_key(), "The property 'EnumPrincipal.Id' does not have a value set and no value generator is availa");
#endif
#elif EF8
    // Fails: No value generator for int/long/enum/converted keys EF-X103
    public override void Insert_update_and_delete_with_enum_key()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.Insert_update_and_delete_with_enum_key(), "The property 'EnumPrincipal.Id' does not have a value set and no value generator is availa");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Insert_update_and_delete_with_long_to_int_conversion()
        => SpecFails.AnyOutcomeAsync(() => base.Insert_update_and_delete_with_long_to_int_conversion(), "The property 'LongToIntPrincipal.Id' does not have a value set and no value generator is a");
#else
    // Fails: No value generator for int/long/enum/converted keys EF-X103
    public override Task Insert_update_and_delete_with_long_to_int_conversion()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Insert_update_and_delete_with_long_to_int_conversion(), "The property 'LongToIntPrincipal.Id' does not have a value set and no value generator is a");
#endif
#elif EF8
    // Fails: No value generator for int/long/enum/converted keys EF-X103
    public override void Insert_update_and_delete_with_long_to_int_conversion()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.Insert_update_and_delete_with_long_to_int_conversion(), "The property 'LongToIntPrincipal.Id' does not have a value set and no value generator is a");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Principal key not generated so dependent key is null EF-X104 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Insert_update_and_delete_with_wrapped_Guid_key()
        => SpecFails.AnyOutcomeAsync(() => base.Insert_update_and_delete_with_wrapped_Guid_key(), "Unable to track an entity of type 'WrappedGuidClassDependentOptional' because its primary ");
#else
    // Fails: Principal key not generated so dependent key is null EF-X104
    public override Task Insert_update_and_delete_with_wrapped_Guid_key()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Insert_update_and_delete_with_wrapped_Guid_key(), "Unable to track an entity of type 'WrappedGuidClassDependentOptional' because its primary ");
#endif
#elif EF8
    // Fails: Principal key not generated so dependent key is null EF-X104
    public override void Insert_update_and_delete_with_wrapped_Guid_key()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Insert_update_and_delete_with_wrapped_Guid_key(), "Unable to track an entity of type 'WrappedGuidClassDependentOptional' because its primary ");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Principal key not generated so dependent key is null EF-X104 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Insert_update_and_delete_with_wrapped_Uri_key()
        => SpecFails.AnyOutcomeAsync(() => base.Insert_update_and_delete_with_wrapped_Uri_key(), "Unable to track an entity of type 'WrappedUriClassDependentOptional' because its primary k");
#else
    // Fails: Principal key not generated so dependent key is null EF-X104
    public override Task Insert_update_and_delete_with_wrapped_Uri_key()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Insert_update_and_delete_with_wrapped_Uri_key(), "Unable to track an entity of type 'WrappedUriClassDependentOptional' because its primary k");
#endif
#elif EF8
    // Fails: Principal key not generated so dependent key is null EF-X104
    public override void Insert_update_and_delete_with_wrapped_Uri_key()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Insert_update_and_delete_with_wrapped_Uri_key(), "Unable to track an entity of type 'WrappedUriClassDependentOptional' because its primary k");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Insert_update_and_delete_with_wrapped_int_key()
        => SpecFails.AnyOutcomeAsync(() => base.Insert_update_and_delete_with_wrapped_int_key(), "The property 'WrappedIntClassPrincipal.Id' does not have a value set and no value generato");
#else
    // Fails: No value generator for int/long/enum/converted keys EF-X103
    public override Task Insert_update_and_delete_with_wrapped_int_key()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Insert_update_and_delete_with_wrapped_int_key(), "The property 'WrappedIntClassPrincipal.Id' does not have a value set and no value generato");
#endif
#elif EF8
    // Fails: No value generator for int/long/enum/converted keys EF-X103
    public override void Insert_update_and_delete_with_wrapped_int_key()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.Insert_update_and_delete_with_wrapped_int_key(), "The property 'WrappedIntClassPrincipal.Id' does not have a value set and no value generato");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Principal key not generated so dependent key is null EF-X104 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Insert_update_and_delete_with_wrapped_string_key()
        => SpecFails.AnyOutcomeAsync(() => base.Insert_update_and_delete_with_wrapped_string_key(), "Unable to track an entity of type 'WrappedStringClassDependentOptional' because its primar");
#else
    // Fails: Principal key not generated so dependent key is null EF-X104
    public override Task Insert_update_and_delete_with_wrapped_string_key()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Insert_update_and_delete_with_wrapped_string_key(), "Unable to track an entity of type 'WrappedStringClassDependentOptional' because its primar");
#endif
#elif EF8
    // Fails: Principal key not generated so dependent key is null EF-X104
    public override void Insert_update_and_delete_with_wrapped_string_key()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Insert_update_and_delete_with_wrapped_string_key(), "Unable to track an entity of type 'WrappedStringClassDependentOptional' because its primar");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Entity type used by the test is missing from the model EF-X107 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Nullable_fields_get_defaults_when_not_set()
        => SpecFails.AnyOutcomeAsync(() => base.Nullable_fields_get_defaults_when_not_set(), "The entity type 'WithNullableBackingFields' was not found. Ensure that the entity type has");
#else
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override Task Nullable_fields_get_defaults_when_not_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Nullable_fields_get_defaults_when_not_set(), "The entity type 'WithNullableBackingFields' was not found. Ensure that the entity type has");
#endif
#elif EF8
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override void Nullable_fields_get_defaults_when_not_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Nullable_fields_get_defaults_when_not_set(), "The entity type 'WithNullableBackingFields' was not found. Ensure that the entity type has");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Entity type used by the test is missing from the model EF-X107 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Nullable_fields_store_any_value_when_set()
        => SpecFails.AnyOutcomeAsync(() => base.Nullable_fields_store_any_value_when_set(), "The entity type 'WithNullableBackingFields' was not found. Ensure that the entity type has");
#else
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override Task Nullable_fields_store_any_value_when_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Nullable_fields_store_any_value_when_set(), "The entity type 'WithNullableBackingFields' was not found. Ensure that the entity type has");
#endif
#elif EF8
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override void Nullable_fields_store_any_value_when_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Nullable_fields_store_any_value_when_set(), "The entity type 'WithNullableBackingFields' was not found. Ensure that the entity type has");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Entity type used by the test is missing from the model EF-X107 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Nullable_fields_store_non_defaults_when_set()
        => SpecFails.AnyOutcomeAsync(() => base.Nullable_fields_store_non_defaults_when_set(), "The entity type 'WithNullableBackingFields' was not found. Ensure that the entity type has");
#else
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override Task Nullable_fields_store_non_defaults_when_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Nullable_fields_store_non_defaults_when_set(), "The entity type 'WithNullableBackingFields' was not found. Ensure that the entity type has");
#endif
#elif EF8
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override void Nullable_fields_store_non_defaults_when_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Nullable_fields_store_non_defaults_when_set(), "The entity type 'WithNullableBackingFields' was not found. Ensure that the entity type has");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Entity type used by the test is missing from the model EF-X107 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Object_fields_get_defaults_when_not_set()
        => SpecFails.AnyOutcomeAsync(() => base.Object_fields_get_defaults_when_not_set(), "The entity type 'WithObjectBackingFields' was not found. Ensure that the entity type has b");
#else
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override Task Object_fields_get_defaults_when_not_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Object_fields_get_defaults_when_not_set(), "The entity type 'WithObjectBackingFields' was not found. Ensure that the entity type has b");
#endif
#elif EF8
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override void Object_fields_get_defaults_when_not_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Object_fields_get_defaults_when_not_set(), "The entity type 'WithObjectBackingFields' was not found. Ensure that the entity type has b");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Entity type used by the test is missing from the model EF-X107 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Object_fields_store_any_value_when_set()
        => SpecFails.AnyOutcomeAsync(() => base.Object_fields_store_any_value_when_set(), "The entity type 'WithObjectBackingFields' was not found. Ensure that the entity type has b");
#else
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override Task Object_fields_store_any_value_when_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Object_fields_store_any_value_when_set(), "The entity type 'WithObjectBackingFields' was not found. Ensure that the entity type has b");
#endif
#elif EF8
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override void Object_fields_store_any_value_when_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Object_fields_store_any_value_when_set(), "The entity type 'WithObjectBackingFields' was not found. Ensure that the entity type has b");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Entity type used by the test is missing from the model EF-X107 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Object_fields_store_non_defaults_when_set()
        => SpecFails.AnyOutcomeAsync(() => base.Object_fields_store_non_defaults_when_set(), "The entity type 'WithObjectBackingFields' was not found. Ensure that the entity type has b");
#else
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override Task Object_fields_store_non_defaults_when_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Object_fields_store_non_defaults_when_set(), "The entity type 'WithObjectBackingFields' was not found. Ensure that the entity type has b");
#endif
#elif EF8
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override void Object_fields_store_non_defaults_when_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Object_fields_store_non_defaults_when_set(), "The entity type 'WithObjectBackingFields' was not found. Ensure that the entity type has b");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Entity type used by the test is missing from the model EF-X107 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Properties_get_database_defaults_when_set_to_sentinel_values()
        => SpecFails.AnyOutcomeAsync(() => base.Properties_get_database_defaults_when_set_to_sentinel_values(), "The entity type 'WithNoBackingFields' was not found. Ensure that the entity type has been ");
#else
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override Task Properties_get_database_defaults_when_set_to_sentinel_values()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Properties_get_database_defaults_when_set_to_sentinel_values(), "The entity type 'WithNoBackingFields' was not found. Ensure that the entity type has been ");
#endif
#elif EF8
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override void Properties_get_database_defaults_when_set_to_sentinel_values()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Properties_get_database_defaults_when_set_to_sentinel_values(), "The entity type 'WithNoBackingFields' was not found. Ensure that the entity type has been ");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: Entity type used by the test is missing from the model EF-X107 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Properties_get_set_values_when_not_set_to_sentinel_values()
        => SpecFails.AnyOutcomeAsync(() => base.Properties_get_set_values_when_not_set_to_sentinel_values(), "The entity type 'WithNoBackingFields' was not found. Ensure that the entity type has been ");
#else
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override Task Properties_get_set_values_when_not_set_to_sentinel_values()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Properties_get_set_values_when_not_set_to_sentinel_values(), "The entity type 'WithNoBackingFields' was not found. Ensure that the entity type has been ");
#endif
#elif EF8
    // Fails: Entity type used by the test is missing from the model EF-X107
    public override void Properties_get_set_values_when_not_set_to_sentinel_values()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Properties_get_set_values_when_not_set_to_sentinel_values(), "The entity type 'WithNoBackingFields' was not found. Ensure that the entity type has been ");
#endif

#if (EF10 || EF11) || EF9
#if EF11
    // Fails: No value generator for int/long/enum/converted keys EF-X103 (non-deterministic: depends on the graph traversal order) EF-X721
    public override Task Store_generated_values_are_propagated_with_composite_key_cycles()
        => SpecFails.AnyOutcomeAsync(() => base.Store_generated_values_are_propagated_with_composite_key_cycles(), "The property 'CompositePrincipal.Id' does not have a value set and no value generator is a");
#else
    public override Task Store_generated_values_are_propagated_with_composite_key_cycles()
        => base.Store_generated_values_are_propagated_with_composite_key_cycles();
#endif
#elif EF8
    public override void Store_generated_values_are_propagated_with_composite_key_cycles()
        => base.Store_generated_values_are_propagated_with_composite_key_cycles();
#endif
}
