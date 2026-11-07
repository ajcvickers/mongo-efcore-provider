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

#if EF10
    public override void Include_address()
    {
        base.Include_address();
    }
#endif

#if EF10
    public override void Include_address_EF_Property()
    {
        base.Include_address_EF_Property();
    }
#endif

#if EF10
    public override void Include_address_shadow()
    {
        base.Include_address_shadow();
    }
#endif

#if EF10
    public override void Include_address_no_tracking()
    {
        base.Include_address_no_tracking();
    }
#endif

#if EF10
    public override void Include_address_no_tracking_EF_Property()
    {
        base.Include_address_no_tracking_EF_Property();
    }
#endif

    public override void Include_person()
    {
        base.Include_person();
    }

    public override void Include_person_EF_Property()
    {
        base.Include_person_EF_Property();
    }

    public override void Include_person_shadow()
    {
        base.Include_person_shadow();
    }

    public override void Include_person_no_tracking()
    {
        base.Include_person_no_tracking();
    }

    public override void Include_person_no_tracking_EF_Property()
    {
        base.Include_person_no_tracking_EF_Property();
    }

#if EF10
    public override void Include_address_when_person_already_tracked()
    {
        base.Include_address_when_person_already_tracked();
    }
#endif

    public override void Include_person_when_address_already_tracked()
    {
        base.Include_person_when_address_already_tracked();
    }

#if EF8 || EF9
    public override void Include_address()
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        SpecFails.Throws(() => base.Include_address(), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override void Include_address_EF_Property()
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        SpecFails.Throws(() => base.Include_address_EF_Property(), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override void Include_address_shadow()
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        SpecFails.Throws(() => base.Include_address_shadow(), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override void Include_address_no_tracking()
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        SpecFails.Throws(() => base.Include_address_no_tracking(), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override void Include_address_no_tracking_EF_Property()
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        SpecFails.Throws(() => base.Include_address_no_tracking_EF_Property(), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override void Include_address_when_person_already_tracked()
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        SpecFails.Throws(() => base.Include_address_when_person_already_tracked(), "could not be translated");
    }
#endif

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

        public override async Task InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server);

            await base.InitializeAsync();
        }
    }
}
