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
    : StoreGeneratedFixupTestBase<StoreGeneratedFixupMongoTest.StoreGeneratedFixupMongoFixture>(fixture)
{
    protected override bool EnforcesFKs => false;

    public class StoreGeneratedFixupMongoFixture : StoreGeneratedFixupFixtureBase
    {
        private readonly InitFailureGuard _initFailure = new();
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("StoreGeneratedFixup");

        public TestServer TestServer { get; private set; } = null!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await _initFailure.RunAsync(base.InitializeAsync);
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override PoolableDbContext CreateContext()
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

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_FK_not_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_FK_not_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_FK_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_FK_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_no_navs_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_no_navs_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_many_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_FK_not_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_FK_not_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_FK_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_FK_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_no_navs_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_no_navs_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_but_not_principal_one_to_one_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_FK_not_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_FK_not_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_FK_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_FK_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_dep_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_dep_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_no_navs_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_no_navs_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_prin_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_prin_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_many_prin_uni_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_many_prin_uni_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_many_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_FK_not_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_FK_not_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_FK_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_FK_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_dep_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_dep_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_no_navs_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_no_navs_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_prin_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_prin_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_dependent_then_principal_one_to_one_prin_uni_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_dependent_then_principal_one_to_one_prin_uni_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_dependent_then_principal_one_to_one_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_overlapping_graph_from_game()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_overlapping_graph_from_game(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_overlapping_graph_from_game()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_overlapping_graph_from_game(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_overlapping_graph_from_item()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_overlapping_graph_from_item(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_overlapping_graph_from_item()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_overlapping_graph_from_item(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_overlapping_graph_from_level()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_overlapping_graph_from_level(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_overlapping_graph_from_level()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_overlapping_graph_from_level(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_FK_not_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_FK_not_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_FK_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_FK_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_no_navs_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_no_navs_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_many_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_FK_not_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_FK_not_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_FK_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_FK_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_no_navs_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_no_navs_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_but_not_dependent_one_to_one_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_FK_not_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_FK_not_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_FK_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_FK_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_dep_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_dep_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_no_navs_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_no_navs_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_prin_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_prin_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_many_prin_uni_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_many_prin_uni_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_many_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_FK_not_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_FK_not_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_not_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_FK_set_both_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_FK_set_both_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_set_both_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_dep_uni_FK_not_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_dep_uni_FK_set_dependent_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_dep_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_dep_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_dep_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_no_navs_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_no_navs_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_no_navs_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_prin_uni_FK_not_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_prin_uni_FK_set_no_navs_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_prin_uni_FK_set_no_navs_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_prin_uni_FK_set_no_navs_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Add_principal_then_dependent_one_to_one_prin_uni_FK_set_principal_nav_set()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Add_principal_then_dependent_one_to_one_prin_uni_FK_set_principal_nav_set()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Add_principal_then_dependent_one_to_one_prin_uni_FK_set_principal_nav_set(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

#if EF10 || EF9
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override Task Multi_level_add_replace_and_save()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Multi_level_add_replace_and_save(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#elif EF8
    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Multi_level_add_replace_and_save()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Multi_level_add_replace_and_save(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
#endif

    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Remove_overlapping_principal()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Remove_overlapping_principal(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");

    // Fails: Relationship discovery between root entity types fails at model build EF-X102
    public override void Temporary_value_equals_database_generated_value()
        => SpecFails.Throws<global::System.InvalidOperationException>(() => base.Temporary_value_equals_database_generated_value(), "Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Level");
}
