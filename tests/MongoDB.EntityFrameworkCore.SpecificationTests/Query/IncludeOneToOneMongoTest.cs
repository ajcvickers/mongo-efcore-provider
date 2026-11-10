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
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class IncludeOneToOneMongoTest : IncludeOneToOneTestBase<IncludeOneToOneMongoTest.IncludeOneToOneMongoFixture>
{
    public IncludeOneToOneMongoTest(IncludeOneToOneMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

#if (EF10 || EF11)
    public override void Include_address()
    {
        base.Include_address();

        AssertMql(
            """
Person.{ "$lookup" : { "from" : "Address", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_Address" } }, { "$set" : { "_lookup_Address" : { "$arrayElemAt" : ["$_lookup_Address", 0] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override void Include_address_EF_Property()
    {
        base.Include_address_EF_Property();

        AssertMql(
            """
Person.{ "$lookup" : { "from" : "Address", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_Address" } }, { "$set" : { "_lookup_Address" : { "$arrayElemAt" : ["$_lookup_Address", 0] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override void Include_address_shadow()
    {
        base.Include_address_shadow();

        AssertMql(
            """
Person2.{ "$lookup" : { "from" : "Address2", "localField" : "_id", "foreignField" : "PersonId", "as" : "_lookup_Address" } }, { "$unwind" : { "path" : "$_lookup_Address", "preserveNullAndEmptyArrays" : true } }
""");
    }
#endif

#if (EF10 || EF11)
    public override void Include_address_no_tracking()
    {
        base.Include_address_no_tracking();

        AssertMql(
            """
Person.{ "$lookup" : { "from" : "Address", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_Address" } }, { "$set" : { "_lookup_Address" : { "$arrayElemAt" : ["$_lookup_Address", 0] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override void Include_address_no_tracking_EF_Property()
    {
        base.Include_address_no_tracking_EF_Property();

        AssertMql(
            """
Person.{ "$lookup" : { "from" : "Address", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_Address" } }, { "$set" : { "_lookup_Address" : { "$arrayElemAt" : ["$_lookup_Address", 0] } } }
""");
    }
#endif

    public override void Include_person()
    {
        base.Include_person();

        AssertMql(
            """
Address.{ "$lookup" : { "from" : "Person", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_Resident" } }
""");
    }

    public override void Include_person_EF_Property()
    {
        base.Include_person_EF_Property();

        AssertMql(
            """
Address.{ "$lookup" : { "from" : "Person", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_Resident" } }
""");
    }

    public override void Include_person_shadow()
    {
        base.Include_person_shadow();

        AssertMql(
            """
Address2.{ "$lookup" : { "from" : "Person2", "localField" : "PersonId", "foreignField" : "_id", "as" : "_lookup_Resident" } }
""");
    }

    public override void Include_person_no_tracking()
    {
        base.Include_person_no_tracking();

        AssertMql(
            """
Address.{ "$lookup" : { "from" : "Person", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_Resident" } }
""");
    }

    public override void Include_person_no_tracking_EF_Property()
    {
        base.Include_person_no_tracking_EF_Property();

        AssertMql(
            """
Address.{ "$lookup" : { "from" : "Person", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_Resident" } }
""");
    }

#if (EF10 || EF11)
    public override void Include_address_when_person_already_tracked()
    {
        base.Include_address_when_person_already_tracked();

        AssertMql(
            """
Person.{ "$match" : { "Name" : "John Snow" } }, { "$limit" : 2 }
""",
            //
            """
Person.{ "$lookup" : { "from" : "Address", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_Address" } }, { "$set" : { "_lookup_Address" : { "$arrayElemAt" : ["$_lookup_Address", 0] } } }
""");
    }
#endif

    public override void Include_person_when_address_already_tracked()
    {
        base.Include_person_when_address_already_tracked();

        AssertMql(
            """
Address.{ "$match" : { "City" : "Meereen" } }, { "$limit" : 2 }
""",
            //
            """
Address.{ "$lookup" : { "from" : "Person", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_Resident" } }
""");
    }

#if EF8 || EF9
    public override void Include_address()
        => base.Include_address();
#endif

#if EF8 || EF9
    public override void Include_address_EF_Property()
        => base.Include_address_EF_Property();
#endif

#if EF8 || EF9
    public override void Include_address_shadow()
        => base.Include_address_shadow();
#endif

#if EF8 || EF9
    public override void Include_address_no_tracking()
        => base.Include_address_no_tracking();
#endif

#if EF8 || EF9
    public override void Include_address_no_tracking_EF_Property()
        => base.Include_address_no_tracking_EF_Property();
#endif

#if EF8 || EF9
    public override void Include_address_when_person_already_tracked()
        => base.Include_address_when_person_already_tracked();
#endif

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    public class IncludeOneToOneMongoFixture : OneToOneQueryFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("InclOneToOne");

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
