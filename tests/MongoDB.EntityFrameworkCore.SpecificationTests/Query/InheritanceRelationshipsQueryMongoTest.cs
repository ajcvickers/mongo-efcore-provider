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

public class InheritanceRelationshipsQueryMongoTest : InheritanceRelationshipsQueryTestBase<InheritanceRelationshipsQueryMongoTest.InheritanceRelationshipsQueryMongoFixture>
{
    public InheritanceRelationshipsQueryMongoTest(InheritanceRelationshipsQueryMongoFixture fixture)
        : base(fixture)
    {
    }

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override void Changes_in_derived_related_entities_are_detected()
        => SpecFails.Throws(() => base.Changes_in_derived_related_entities_are_detected());

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override void Entity_can_make_separate_relationships_with_base_type_and_derived_type_both()
        => SpecFails.Throws(() => base.Entity_can_make_separate_relationships_with_base_type_and_derived_type_both());

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_with_inheritance(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_with_inheritance(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_with_inheritance_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_with_inheritance_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_self_reference_with_inheritance(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_self_reference_with_inheritance(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_self_reference_with_inheritance_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_self_reference_with_inheritance_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_with_inheritance_with_filter(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_with_inheritance_with_filter(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_with_inheritance_with_filter_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_with_inheritance_with_filter_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_without_inheritance(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_without_inheritance(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_without_inheritance_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_without_inheritance_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_without_inheritance_with_filter(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_without_inheritance_with_filter(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_without_inheritance_with_filter_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_without_inheritance_with_filter_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_collection_with_inheritance(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_collection_with_inheritance(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_collection_with_inheritance_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_collection_with_inheritance_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_collection_with_inheritance_with_filter(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_collection_with_inheritance_with_filter(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_collection_with_inheritance_with_filter_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_collection_with_inheritance_with_filter_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_collection_without_inheritance(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_collection_without_inheritance(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_collection_without_inheritance_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_collection_without_inheritance_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_collection_without_inheritance_with_filter(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_collection_without_inheritance_with_filter(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_collection_without_inheritance_with_filter_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_collection_without_inheritance_with_filter_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_with_inheritance_on_derived1(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_with_inheritance_on_derived1(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_with_inheritance_on_derived2(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_with_inheritance_on_derived2(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_with_inheritance_on_derived4(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_with_inheritance_on_derived4(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_with_inheritance_on_derived_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_with_inheritance_on_derived_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_with_inheritance_on_derived_with_filter1(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_with_inheritance_on_derived_with_filter1(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_with_inheritance_on_derived_with_filter2(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_with_inheritance_on_derived_with_filter2(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_with_inheritance_on_derived_with_filter4(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_with_inheritance_on_derived_with_filter4(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_with_inheritance_on_derived_with_filter_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_with_inheritance_on_derived_with_filter_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_without_inheritance_on_derived1(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_without_inheritance_on_derived1(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_without_inheritance_on_derived2(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_without_inheritance_on_derived2(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_reference_without_inheritance_on_derived_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_reference_without_inheritance_on_derived_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_collection_with_inheritance_on_derived1(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_collection_with_inheritance_on_derived1(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_collection_with_inheritance_on_derived2(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_collection_with_inheritance_on_derived2(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_collection_with_inheritance_on_derived3(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_collection_with_inheritance_on_derived3(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_collection_with_inheritance_on_derived_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_collection_with_inheritance_on_derived_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Nested_include_with_inheritance_reference_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_reference_reference(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Nested_include_with_inheritance_reference_reference_on_base(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_reference_reference_on_base(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Nested_include_with_inheritance_reference_reference_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_reference_reference_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Nested_include_with_inheritance_reference_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_reference_collection(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Nested_include_with_inheritance_reference_collection_on_base(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_reference_collection_on_base(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Nested_include_with_inheritance_reference_collection_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_reference_collection_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Nested_include_with_inheritance_collection_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_collection_reference(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Nested_include_with_inheritance_collection_reference_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_collection_reference_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Nested_include_with_inheritance_collection_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_collection_collection(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Nested_include_with_inheritance_collection_collection_reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_collection_collection_reverse(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Nested_include_collection_reference_on_non_entity_base(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_collection_reference_on_non_entity_base(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Collection_projection_on_base_type(bool async)
        => SpecFails.ThrowsAsync(() => base.Collection_projection_on_base_type(async));

    // Fails: Model validation: the dependent side of a one-to-one relationship in the fixture model cannot be determined, so no test runs EF-X538
    public override Task Include_on_derived_type_with_queryable_Cast(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_on_derived_type_with_queryable_Cast(async));

    public class InheritanceRelationshipsQueryMongoFixture : InheritanceRelationshipsQueryFixtureBase
    {
        private readonly InitFailureGuard _initFailure = new();
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("InhRel");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

            await _initFailure.RunAsync(base.InitializeAsync);
        }

        public override Microsoft.EntityFrameworkCore.TestModels.InheritanceRelationshipsModel.InheritanceRelationshipsContext CreateContext()
        {
            _initFailure.ThrowIfFailed();
            return base.CreateContext();
        }
    }
}
