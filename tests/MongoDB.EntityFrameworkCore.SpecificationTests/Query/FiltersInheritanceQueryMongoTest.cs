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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
#if EF11
using Microsoft.EntityFrameworkCore.Query.Inheritance;
#endif
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class FiltersInheritanceQueryMongoTest : FiltersInheritanceQueryTestBase<FiltersInheritanceQueryMongoTest.FiltersInheritanceQueryMongoFixture>
{
    public FiltersInheritanceQueryMongoTest(FiltersInheritanceQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Can_use_of_type_animal(bool async)
    {
        await base.Can_use_of_type_animal(async);
    }

    public override async Task Can_use_is_kiwi(bool async)
    {
        await base.Can_use_is_kiwi(async);
    }

    public override async Task Can_use_is_kiwi_with_other_predicate(bool async)
    {
        await base.Can_use_is_kiwi_with_other_predicate(async);
    }

    public override async Task Can_use_is_kiwi_in_projection(bool async)
    {
        await base.Can_use_is_kiwi_in_projection(async);
    }

    public override async Task Can_use_of_type_bird(bool async)
    {
        await base.Can_use_of_type_bird(async);
    }

    public override async Task Can_use_of_type_bird_predicate(bool async)
    {
        await base.Can_use_of_type_bird_predicate(async);
    }

    public override async Task Can_use_of_type_bird_with_projection(bool async)
    {
        await base.Can_use_of_type_bird_with_projection(async);
    }

    public override async Task Can_use_of_type_bird_first(bool async)
    {
        await base.Can_use_of_type_bird_first(async);
    }

    public override async Task Can_use_of_type_kiwi(bool async)
    {
        await base.Can_use_of_type_kiwi(async);
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Can_use_derived_set(bool async)
    {
        // Fails: InvalidOperationException from query execution or materialization EF-X604
        await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Can_use_derived_set(async));
    }
#else
    public override async Task Can_use_derived_set(bool async)
    {
        // Fails: Hierarchy materialization fails: discriminator value does not match the requested type EF-X519
        await SpecFails.ThrowsAsync(() => base.Can_use_derived_set(async), "No discriminators matched");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Can_use_IgnoreQueryFilters_and_GetDatabaseValues(bool async)
    {
        await base.Can_use_IgnoreQueryFilters_and_GetDatabaseValues(async);
    }
#else
    public override async Task Can_use_IgnoreQueryFilters_and_GetDatabaseValues(bool async)
    {
        // Fails: Single/First over query results finds the wrong number of rows (duplicate or missing rows) EF-X537
        await SpecFails.ThrowsAsync(() => base.Can_use_IgnoreQueryFilters_and_GetDatabaseValues(async), "Sequence contains");
    }
#endif

    public class FiltersInheritanceQueryMongoFixture : InheritanceQueryFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("FiltInherit");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
            MongoSpecModelHelpers.RelaxNullableUniqueIndexes(modelBuilder);
        }

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server);

            await base.InitializeAsync();
        }
    }
}
