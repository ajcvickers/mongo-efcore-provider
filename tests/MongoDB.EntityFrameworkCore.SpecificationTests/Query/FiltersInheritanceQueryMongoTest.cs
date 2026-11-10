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

        AssertMql(
            """
Animals.{ "$sort" : { "Species" : 1 } }
""");
    }

    public override async Task Can_use_is_kiwi(bool async)
    {
        await base.Can_use_is_kiwi(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }
""");
    }

    public override async Task Can_use_is_kiwi_with_other_predicate(bool async)
    {
        await base.Can_use_is_kiwi_with_other_predicate(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi", "CountryId" : 1 } }
""");
    }

    public override async Task Can_use_is_kiwi_in_projection(bool async)
    {
        await base.Can_use_is_kiwi_in_projection(async);

        // Review finding: the projection runs on the driver-LINQ fallback (the whole document is fetched, no $project); NativeOnly refuses it ("Query projects a non-entity result").
        AssertMql(
            """
Animals.
""");
    }

    public override async Task Can_use_of_type_bird(bool async)
    {
        await base.Can_use_of_type_bird(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : { "$in" : ["Eagle", "Kiwi"] } } }, { "$sort" : { "Species" : 1 } }
""");
    }

    public override async Task Can_use_of_type_bird_predicate(bool async)
    {
        await base.Can_use_of_type_bird_predicate(async);

        AssertMql(
            """
Animals.{ "$match" : { "CountryId" : 1, "_t" : { "$in" : ["Eagle", "Kiwi"] } } }, { "$sort" : { "Species" : 1 } }
""");
    }

    public override async Task Can_use_of_type_bird_with_projection(bool async)
    {
        await base.Can_use_of_type_bird_with_projection(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : { "$in" : ["Eagle", "Kiwi"] } } }, { "$project" : { "Name" : "$Name", "_id" : 0 } }
""");
    }

    public override async Task Can_use_of_type_bird_first(bool async)
    {
        await base.Can_use_of_type_bird_first(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : { "$in" : ["Eagle", "Kiwi"] } } }, { "$sort" : { "Species" : 1 } }, { "$limit" : 1 }
""");
    }

    public override async Task Can_use_of_type_kiwi(bool async)
    {
        await base.Can_use_of_type_kiwi(async);

        AssertMql(
            """
Animals.{ "$match" : { "_t" : "Kiwi" } }
""");
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

        // The second statement embeds a client-generated integer key (TestIntegerValueGenerator), which depends on how many keys
        // were generated earlier in the process, so generated keys are normalized before comparing.
        AssertMqlIgnoringGeneratedKeys(
            """
Animals.{ "$match" : { "_t" : "Eagle" } }, { "$limit" : 2 }
""",
            //
            """
Animals.{ "$match" : { "_t" : "Eagle", "_id" : 1000019 } }, { "$limit" : 1 }, { "$project" : { "_ctorArg0" : "$_id", "_ctorArg1" : "$CountryId", "_ctorArg2" : "$_t", "_ctorArg3" : "$Name", "_ctorArg4" : "$Species", "_ctorArg5" : "$EagleId", "_ctorArg6" : "$IsFlightless", "_ctorArg7" : "$Group", "_id" : 0 } }
""");
    }
#else
    public override async Task Can_use_IgnoreQueryFilters_and_GetDatabaseValues(bool async)
    {
        // Fails: Single/First over query results finds the wrong number of rows (duplicate or missing rows) EF-X537
        await SpecFails.ThrowsAsync(() => base.Can_use_IgnoreQueryFilters_and_GetDatabaseValues(async), "Sequence contains");
    }
#endif

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    private void AssertMqlIgnoringGeneratedKeys(params string[] expected)
    {
        static string Normalize(string mql)
            => System.Text.RegularExpressions.Regex.Replace(mql, """_id" : [1-9][0-9]{6,}(?![0-9])""", """_id" : <generated>""");

        var actual = Fixture.TestMqlLoggerFactory.MqlStatements.Select(Normalize).ToArray();
        Fixture.TestMqlLoggerFactory.Clear();
        Assert.Equal(expected.Select(Normalize).ToArray(), actual);
    }

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
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }
    }
}
