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
#if EF11
using Microsoft.EntityFrameworkCore.BulkUpdates.Inheritance;
#endif
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

// Conformance coverage for EF Core's inheritance bulk-update spec suite. Bulk operations are supported only for a
// single collection scoped by Where with scalar setters; the cases that fall outside that subset assert their failure.
public class InheritanceBulkUpdatesMongoTest : InheritanceBulkUpdatesTestBase<InheritanceBulkUpdatesMongoTest.InheritanceBulkUpdatesMongoFixture>
{
    public InheritanceBulkUpdatesMongoTest(InheritanceBulkUpdatesMongoFixture fixture)
        : base(fixture)
    {
    }

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override Task Delete_where_hierarchy(bool async)
        => base.Delete_where_hierarchy(async);

    public override Task Delete_where_hierarchy_subquery(bool async)
        => base.Delete_where_hierarchy_subquery(async);

    public override Task Delete_where_hierarchy_derived(bool async)
        => base.Delete_where_hierarchy_derived(async);

    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Delete_where_using_hierarchy(bool async)
        => SpecFails.ThrowsAsync(() => base.Delete_where_using_hierarchy(async));

    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Delete_where_using_hierarchy_derived(bool async)
        => SpecFails.ThrowsAsync(() => base.Delete_where_using_hierarchy_derived(async));

    public override Task Delete_GroupBy_Where_Select_First(bool async)
        => base.Delete_GroupBy_Where_Select_First(async);

    public override Task Delete_GroupBy_Where_Select_First_2(bool async)
        => base.Delete_GroupBy_Where_Select_First_2(async);

    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Delete_GroupBy_Where_Select_First_3(bool async)
        => SpecFails.ThrowsAsync(() => base.Delete_GroupBy_Where_Select_First_3(async));

    public override Task Update_base_type(bool async)
        => base.Update_base_type(async);

    public override Task Update_base_type_with_OfType(bool async)
        => base.Update_base_type_with_OfType(async);

    public override Task Update_where_hierarchy_subquery(bool async)
        => base.Update_where_hierarchy_subquery(async);

    public override Task Update_base_property_on_derived_type(bool async)
        => base.Update_base_property_on_derived_type(async);

    public override Task Update_derived_property_on_derived_type(bool async)
        => base.Update_derived_property_on_derived_type(async);

    public override Task Update_base_and_derived_types(bool async)
        => base.Update_base_and_derived_types(async);

    // Fails: Bulk ExecuteUpdate/ExecuteDelete source restricted to a single collection scoped by Where EF-X016
    public override Task Update_where_using_hierarchy(bool async)
        => SpecFails.ThrowsAsync(() => base.Update_where_using_hierarchy(async));

    // Fails: Cross-document navigation access issue EF-216
    public override Task Update_where_using_hierarchy_derived(bool async)
        => SpecFails.ThrowsAsync(() => base.Update_where_using_hierarchy_derived(async));

    public override Task Update_with_interface_in_property_expression(bool async)
        => base.Update_with_interface_in_property_expression(async);

    public override Task Update_with_interface_in_EF_Property_in_property_expression(bool async)
        => base.Update_with_interface_in_EF_Property_in_property_expression(async);

    public class InheritanceBulkUpdatesMongoFixture : InheritanceBulkUpdatesFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("InhBulk");

        protected override bool UsePooling
            => false;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            // EF-X201: a single-property key must be mapped to _id; the integer keys are generated on the client (EF-X205).
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
        }

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        // The asserter begins a rolled-back transaction on one context and runs the bulk operation on a second;
        // enlist the second in the same session so DeleteMany/UpdateMany roll back with it.
        public override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
            => facade.UseTransaction(transaction);
    }
}

#endif
