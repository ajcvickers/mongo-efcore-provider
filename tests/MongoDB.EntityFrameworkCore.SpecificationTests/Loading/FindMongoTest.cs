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

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Loading;

public abstract class FindMongoTest(FindMongoTest.FindMongoFixture fixture)
    : FindTestBase<FindMongoTest.FindMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(typeof(FindMongoTest));

    public override void Find_int_key_tracked()
        => base.Find_int_key_tracked();

    public override void Find_int_key_from_store()
        => base.Find_int_key_from_store();

    public override void Returns_null_for_int_key_not_in_store()
        => base.Returns_null_for_int_key_not_in_store();

    public override void Find_nullable_int_key_tracked()
        => base.Find_nullable_int_key_tracked();

    public override void Find_nullable_int_key_from_store()
        => base.Find_nullable_int_key_from_store();

    public override void Returns_null_for_nullable_int_key_not_in_store()
        => base.Returns_null_for_nullable_int_key_not_in_store();

    public override void Find_string_key_tracked()
        => base.Find_string_key_tracked();

    public override void Find_string_key_from_store()
        => base.Find_string_key_from_store();

    public override void Returns_null_for_string_key_not_in_store()
        => base.Returns_null_for_string_key_not_in_store();

    public override void Find_composite_key_tracked()
        => base.Find_composite_key_tracked();

    public override void Find_composite_key_from_store()
        => base.Find_composite_key_from_store();

    public override void Returns_null_for_composite_key_not_in_store()
        => base.Returns_null_for_composite_key_not_in_store();

    public override void Find_base_type_tracked()
        => base.Find_base_type_tracked();

    public override void Find_base_type_from_store()
        => base.Find_base_type_from_store();

    public override void Returns_null_for_base_type_not_in_store()
        => base.Returns_null_for_base_type_not_in_store();

    public override void Find_derived_type_tracked()
        => base.Find_derived_type_tracked();

    public override void Find_derived_type_from_store()
        => base.Find_derived_type_from_store();

    public override void Returns_null_for_derived_type_not_in_store()
        => base.Returns_null_for_derived_type_not_in_store();

    public override void Find_base_type_using_derived_set_tracked()
        => base.Find_base_type_using_derived_set_tracked();

    public override void Find_base_type_using_derived_set_from_store()
        => base.Find_base_type_using_derived_set_from_store();

    public override void Find_derived_type_using_base_set_tracked()
        => base.Find_derived_type_using_base_set_tracked();

    public override void Find_derived_using_base_set_type_from_store()
        => base.Find_derived_using_base_set_type_from_store();

    public override void Find_shadow_key_tracked()
        => base.Find_shadow_key_tracked();

    public override void Find_shadow_key_from_store()
        => base.Find_shadow_key_from_store();

    public override void Returns_null_for_shadow_key_not_in_store()
        => base.Returns_null_for_shadow_key_not_in_store();

    public override void Returns_null_for_null_key_values_array()
        => base.Returns_null_for_null_key_values_array();

    public override void Returns_null_for_null_key()
        => base.Returns_null_for_null_key();

    public override void Returns_null_for_null_nullable_key()
        => base.Returns_null_for_null_nullable_key();

    public override void Returns_null_for_null_in_composite_key()
        => base.Returns_null_for_null_in_composite_key();

    public override void Throws_for_multiple_values_passed_for_simple_key()
        => base.Throws_for_multiple_values_passed_for_simple_key();

    public override void Throws_for_wrong_number_of_values_for_composite_key()
        => base.Throws_for_wrong_number_of_values_for_composite_key();

    public override void Throws_for_bad_type_for_simple_key()
        => base.Throws_for_bad_type_for_simple_key();

    public override void Throws_for_bad_type_for_composite_key()
        => base.Throws_for_bad_type_for_composite_key();

    public override void Throws_for_bad_entity_type()
        => base.Throws_for_bad_entity_type();

    public override void Throws_for_bad_entity_type_with_different_namespace()
        => base.Throws_for_bad_entity_type_with_different_namespace();

    public override Task Find_int_key_tracked_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_int_key_tracked_async(cancellationType);

    public override Task Find_int_key_from_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_int_key_from_store_async(cancellationType);

    public override Task Returns_null_for_int_key_not_in_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Returns_null_for_int_key_not_in_store_async(cancellationType);

    public override Task Find_nullable_int_key_tracked_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_nullable_int_key_tracked_async(cancellationType);

    public override Task Find_nullable_int_key_from_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_nullable_int_key_from_store_async(cancellationType);

    public override Task Returns_null_for_nullable_int_key_not_in_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Returns_null_for_nullable_int_key_not_in_store_async(cancellationType);

    public override Task Find_string_key_tracked_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_string_key_tracked_async(cancellationType);

    public override Task Find_string_key_from_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_string_key_from_store_async(cancellationType);

    public override Task Returns_null_for_string_key_not_in_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Returns_null_for_string_key_not_in_store_async(cancellationType);

    public override Task Find_composite_key_tracked_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_composite_key_tracked_async(cancellationType);

    public override Task Find_composite_key_from_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_composite_key_from_store_async(cancellationType);

    public override Task Returns_null_for_composite_key_not_in_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Returns_null_for_composite_key_not_in_store_async(cancellationType);

    public override Task Find_base_type_tracked_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_base_type_tracked_async(cancellationType);

    public override Task Find_base_type_from_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_base_type_from_store_async(cancellationType);

    public override Task Returns_null_for_base_type_not_in_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Returns_null_for_base_type_not_in_store_async(cancellationType);

    public override Task Find_derived_type_tracked_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_derived_type_tracked_async(cancellationType);

    public override Task Find_derived_type_from_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_derived_type_from_store_async(cancellationType);

    public override Task Returns_null_for_derived_type_not_in_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Returns_null_for_derived_type_not_in_store_async(cancellationType);

    public override Task Find_base_type_using_derived_set_tracked_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_base_type_using_derived_set_tracked_async(cancellationType);

    public override Task Find_base_type_using_derived_set_from_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_base_type_using_derived_set_from_store_async(cancellationType);

    public override Task Find_derived_type_using_base_set_tracked_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_derived_type_using_base_set_tracked_async(cancellationType);

    public override Task Find_derived_using_base_set_type_from_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_derived_using_base_set_type_from_store_async(cancellationType);

    public override Task Find_shadow_key_tracked_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_shadow_key_tracked_async(cancellationType);

    public override Task Find_shadow_key_from_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Find_shadow_key_from_store_async(cancellationType);

    public override Task Returns_null_for_shadow_key_not_in_store_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Returns_null_for_shadow_key_not_in_store_async(cancellationType);

    public override Task Returns_null_for_null_key_values_array_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Returns_null_for_null_key_values_array_async(cancellationType);

    public override Task Returns_null_for_null_key_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Returns_null_for_null_key_async(cancellationType);

    public override Task Returns_null_for_null_in_composite_key_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Returns_null_for_null_in_composite_key_async(cancellationType);

    public override Task Throws_for_multiple_values_passed_for_simple_key_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Throws_for_multiple_values_passed_for_simple_key_async(cancellationType);

    public override Task Throws_for_wrong_number_of_values_for_composite_key_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Throws_for_wrong_number_of_values_for_composite_key_async(cancellationType);

    public override Task Throws_for_bad_type_for_simple_key_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Throws_for_bad_type_for_simple_key_async(cancellationType);

    public override Task Throws_for_bad_type_for_composite_key_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Throws_for_bad_type_for_composite_key_async(cancellationType);

    public override Task Throws_for_bad_entity_type_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Throws_for_bad_entity_type_async(cancellationType);

    public override Task Throws_for_bad_entity_type_with_different_namespace_async(FindTestBase<FindMongoFixture>.CancellationType cancellationType)
        => base.Throws_for_bad_entity_type_with_different_namespace_async(cancellationType);

    public class FindMongoTestSet(FindMongoFixture fixture) : FindMongoTest(fixture)
    {
        protected override TestFinder Finder { get; } = new FindViaSetFinder();
    }

    public class FindMongoTestContext(FindMongoFixture fixture) : FindMongoTest(fixture)
    {
        protected override TestFinder Finder { get; } = new FindViaContextFinder();
    }

    public class FindMongoTestNonGeneric(FindMongoFixture fixture) : FindMongoTest(fixture)
    {
        protected override TestFinder Finder { get; } = new FindViaNonGenericContextFinder();
    }

    public class FindMongoFixture : FindFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("FindTest");

        public TestServer TestServer { get; private set; }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }
    }
}
