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

// ExecuteUpdate / ExecuteDelete bulk operations are EF9+ only.
#if !EF8

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.BulkUpdates;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class NonSharedModelBulkUpdatesMongoTest : NonSharedModelBulkUpdatesTestBase
{
#if EF10 || EF11
    public NonSharedModelBulkUpdatesMongoTest(NonSharedFixture fixture)
        : base(fixture)
    {
        // The class-level fixture would reuse the first test's (already seeded) test store for every test;
        // give each test its own uniquely named database instead.
#if EF11
        NonSharedFixture = null;
#else
        Fixture = null;
#endif
    }

#endif
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Delete_aggregate_root_when_eager_loaded_owned_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Delete_aggregate_root_when_eager_loaded_owned_collection(async));

    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Delete_with_owned_collection_and_non_natively_translatable_query(bool async)
        => SpecFails.ThrowsAsync(() => base.Delete_with_owned_collection_and_non_natively_translatable_query(async));

    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Delete_aggregate_root_when_table_sharing_with_owned(bool async)
        => SpecFails.ThrowsAsync(() => base.Delete_aggregate_root_when_table_sharing_with_owned(async));

    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Replace_ColumnExpression_in_column_setter(bool async)
        => SpecFails.ThrowsAsync(() => base.Replace_ColumnExpression_in_column_setter(async));

#if EF9
    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Update_non_owned_property_on_entity_with_owned(bool async)
        => SpecFails.ThrowsAsync(() => base.Update_non_owned_property_on_entity_with_owned(async));
#else
    public override Task Update_non_owned_property_on_entity_with_owned(bool async)
        => base.Update_non_owned_property_on_entity_with_owned(async);
#endif

#if EF9
    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Update_non_owned_property_on_entity_with_owned2(bool async)
        => SpecFails.ThrowsAsync(() => base.Update_non_owned_property_on_entity_with_owned2(async));
#else
    public override Task Update_non_owned_property_on_entity_with_owned2(bool async)
        => base.Update_non_owned_property_on_entity_with_owned2(async);
#endif

    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Update_non_owned_property_on_entity_with_owned_in_join(bool async)
        => SpecFails.ThrowsAsync(() => base.Update_non_owned_property_on_entity_with_owned_in_join(async));

    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Update_owned_and_non_owned_properties_with_table_sharing(bool async)
        => SpecFails.ThrowsAsync(() => base.Update_owned_and_non_owned_properties_with_table_sharing(async));

    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Delete_entity_with_auto_include(bool async)
        => SpecFails.ThrowsAsync(() => base.Delete_entity_with_auto_include(async));

    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Delete_predicate_based_on_optional_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Delete_predicate_based_on_optional_navigation(async));

    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Update_with_alias_uniquification_in_setter_subquery(bool async)
        => SpecFails.ThrowsAsync(() => base.Update_with_alias_uniquification_in_setter_subquery(async));

    private ITestStoreFactory? _testStoreFactory;

#if EF11
    protected override ITestStoreFactory NonSharedTestStoreFactory
#else
    protected override ITestStoreFactory TestStoreFactory
#endif
        => _testStoreFactory!;

#if EF11
    protected override string NonSharedStoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("NonSharedBulk");
#else
    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("NonSharedBulk");
#endif

    // The asserter begins a rolled-back transaction on one context and runs the bulk operation on a second;
    // enlist the second in the same session so DeleteMany/UpdateMany roll back with it.
    public override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => ((MongoTransactionManager)facade.GetService<IDbContextTransactionManager>()).CurrentTransaction = transaction;

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server);

        await base.InitializeAsync();
    }
}

#endif
