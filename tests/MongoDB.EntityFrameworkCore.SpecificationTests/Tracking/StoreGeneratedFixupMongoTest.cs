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

public class StoreGeneratedFixupMongoTest(StoreGeneratedFixupMongoTest.StoreGeneratedFixupMongoFixture fixture)
    : StoreGeneratedFixupTestBase<StoreGeneratedFixupMongoTest.StoreGeneratedFixupMongoFixture>(fixture), IAsyncLifetime
{
    // The tests write to the shared database and the contexts cannot yet enlist in a rolled-back transaction (#77), so each test reseeds it; otherwise which test sees which rows depends on the order they run in.
    public async LifetimeTask InitializeAsync()
        => await Fixture.ReseedAsync();

    public LifetimeTask DisposeAsync()
        => LifetimeTask.CompletedTask;

    protected override bool EnforcesFKs => false;

    public class StoreGeneratedFixupMongoFixture : StoreGeneratedFixupFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("StoreGeneratedFixup");

        public TestServer TestServer { get; private set; } = null!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            // EF-X102: the provider owns, by convention, any type it reaches through a navigation that is not yet in the model, which breaks the
            // upstream relationships; here every such type is an independent entity type.
            configurationBuilder.DoNotOwnTypes((_, _) => true);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
            MongoSpecModelHelpers.RelaxNullableUniqueIndexes(modelBuilder);
            modelBuilder.RelaxUniqueForeignKeyIndexes();
        }
    }

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_FK_not_set_both_navs_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_not_set_both_navs_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_FK_not_set_both_navs_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_not_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_FK_not_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_FK_not_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_FK_not_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_FK_not_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_FK_set_both_navs_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_set_both_navs_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_FK_set_both_navs_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_FK_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_FK_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_FK_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_FK_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_no_navs_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_many_no_navs_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_no_navs_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_many_no_navs_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_FK_not_set_both_navs_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_not_set_both_navs_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_FK_not_set_both_navs_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_not_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_FK_not_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_FK_not_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_FK_not_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_FK_not_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_FK_set_both_navs_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_set_both_navs_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_FK_set_both_navs_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_FK_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_FK_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_FK_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_FK_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_no_navs_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_one_no_navs_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_no_navs_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_one_no_navs_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_no_navs_set()
        => base.Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_principal_nav_set()
        => base.Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_FK_not_set_both_navs_set()
        => base.Add_dependent_then_principal_one_to_many_FK_not_set_both_navs_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_FK_not_set_both_navs_set()
        => base.Add_dependent_then_principal_one_to_many_FK_not_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_FK_not_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_many_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_FK_not_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_many_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_FK_not_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_many_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_FK_not_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_many_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_FK_set_both_navs_set()
        => base.Add_dependent_then_principal_one_to_many_FK_set_both_navs_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_FK_set_both_navs_set()
        => base.Add_dependent_then_principal_one_to_many_FK_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_FK_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_many_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_FK_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_many_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_many_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_many_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_FK_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_many_FK_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_FK_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_many_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_many_dep_uni_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_many_dep_uni_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_dep_uni_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_many_dep_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_dep_uni_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_many_dep_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_no_navs_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_many_no_navs_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_no_navs_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_many_no_navs_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_prin_uni_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_many_prin_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_prin_uni_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_many_prin_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_many_prin_uni_FK_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_many_prin_uni_FK_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_many_prin_uni_FK_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_many_prin_uni_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_FK_not_set_both_navs_set()
        => base.Add_dependent_then_principal_one_to_one_FK_not_set_both_navs_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_FK_not_set_both_navs_set()
        => base.Add_dependent_then_principal_one_to_one_FK_not_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_FK_not_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_one_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_FK_not_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_one_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_FK_not_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_one_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_FK_not_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_one_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_FK_set_both_navs_set()
        => base.Add_dependent_then_principal_one_to_one_FK_set_both_navs_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_FK_set_both_navs_set()
        => base.Add_dependent_then_principal_one_to_one_FK_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_FK_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_one_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_FK_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_one_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_one_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_one_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_FK_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_one_FK_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_FK_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_one_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_one_dep_uni_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => base.Add_dependent_then_principal_one_to_one_dep_uni_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_dep_uni_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_one_dep_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_dep_uni_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_one_dep_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_no_navs_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_one_no_navs_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_no_navs_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_one_no_navs_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_prin_uni_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_one_prin_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_prin_uni_FK_set_no_navs_set()
        => base.Add_dependent_then_principal_one_to_one_prin_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_dependent_then_principal_one_to_one_prin_uni_FK_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_one_prin_uni_FK_set_principal_nav_set();
#elif EF8
    public override void Add_dependent_then_principal_one_to_one_prin_uni_FK_set_principal_nav_set()
        => base.Add_dependent_then_principal_one_to_one_prin_uni_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_overlapping_graph_from_game()
        => base.Add_overlapping_graph_from_game();
#elif EF8
    public override void Add_overlapping_graph_from_game()
        => base.Add_overlapping_graph_from_game();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_overlapping_graph_from_item()
        => base.Add_overlapping_graph_from_item();
#elif EF8
    public override void Add_overlapping_graph_from_item()
        => base.Add_overlapping_graph_from_item();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_overlapping_graph_from_level()
        => base.Add_overlapping_graph_from_level();
#elif EF8
    public override void Add_overlapping_graph_from_level()
        => base.Add_overlapping_graph_from_level();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_FK_not_set_both_navs_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_not_set_both_navs_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_FK_not_set_both_navs_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_not_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_FK_not_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_FK_not_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_FK_not_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_FK_not_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_FK_set_both_navs_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_set_both_navs_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_FK_set_both_navs_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_FK_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_FK_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_FK_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_set_principal_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_FK_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_no_navs_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_many_no_navs_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_no_navs_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_many_no_navs_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_principal_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_FK_not_set_both_navs_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_not_set_both_navs_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_FK_not_set_both_navs_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_not_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_FK_not_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_FK_not_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_FK_not_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_FK_not_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_FK_set_both_navs_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_set_both_navs_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_FK_set_both_navs_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_FK_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_FK_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_FK_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_set_principal_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_FK_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_no_navs_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_one_no_navs_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_no_navs_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_one_no_navs_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_no_navs_set()
        => base.Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_principal_nav_set();
#elif EF8
    public override void Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_principal_nav_set()
        => base.Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_FK_not_set_both_navs_set()
        => base.Add_principal_then_dependent_one_to_many_FK_not_set_both_navs_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_FK_not_set_both_navs_set()
        => base.Add_principal_then_dependent_one_to_many_FK_not_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_FK_not_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_many_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_FK_not_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_many_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_FK_not_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_many_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_FK_not_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_many_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_FK_set_both_navs_set()
        => base.Add_principal_then_dependent_one_to_many_FK_set_both_navs_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_FK_set_both_navs_set()
        => base.Add_principal_then_dependent_one_to_many_FK_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_FK_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_many_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_FK_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_many_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_many_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_many_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_FK_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_many_FK_set_principal_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_FK_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_many_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_dep_uni_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_many_dep_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_dep_uni_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_many_dep_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_no_navs_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_many_no_navs_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_no_navs_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_many_no_navs_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_prin_uni_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_many_prin_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_prin_uni_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_many_prin_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_many_prin_uni_FK_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_many_prin_uni_FK_set_principal_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_many_prin_uni_FK_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_many_prin_uni_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_FK_not_set_both_navs_set()
        => base.Add_principal_then_dependent_one_to_one_FK_not_set_both_navs_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_FK_not_set_both_navs_set()
        => base.Add_principal_then_dependent_one_to_one_FK_not_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_FK_not_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_one_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_FK_not_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_one_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_FK_not_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_one_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_FK_not_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_one_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_FK_set_both_navs_set()
        => base.Add_principal_then_dependent_one_to_one_FK_set_both_navs_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_FK_set_both_navs_set()
        => base.Add_principal_then_dependent_one_to_one_FK_set_both_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_FK_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_one_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_FK_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_one_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_one_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_one_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_FK_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_one_FK_set_principal_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_FK_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_one_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => base.Add_principal_then_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_dep_uni_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_one_dep_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_dep_uni_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_one_dep_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_no_navs_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_one_no_navs_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_no_navs_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_one_no_navs_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_prin_uni_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_one_prin_uni_FK_set_no_navs_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_prin_uni_FK_set_no_navs_set()
        => base.Add_principal_then_dependent_one_to_one_prin_uni_FK_set_no_navs_set();
#endif

#if (EF10 || EF11) || EF9
    public override Task Add_principal_then_dependent_one_to_one_prin_uni_FK_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_one_prin_uni_FK_set_principal_nav_set();
#elif EF8
    public override void Add_principal_then_dependent_one_to_one_prin_uni_FK_set_principal_nav_set()
        => base.Add_principal_then_dependent_one_to_one_prin_uni_FK_set_principal_nav_set();
#endif

#if (EF10 || EF11) || EF9
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Multi_level_add_replace_and_save()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Multi_level_add_replace_and_save());
#elif EF8
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override void Multi_level_add_replace_and_save()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Multi_level_add_replace_and_save());
#endif

    public override void Remove_overlapping_principal()
        => base.Remove_overlapping_principal();

    public override void Temporary_value_equals_database_generated_value()
        => base.Temporary_value_equals_database_generated_value();
}
