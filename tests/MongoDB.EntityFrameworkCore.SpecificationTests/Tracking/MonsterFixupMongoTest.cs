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
using Microsoft.EntityFrameworkCore.TestModels;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Tracking;

public class MonsterFixupMongoTest(MonsterFixupMongoTest.MonsterFixupMongoFixture fixture)
    : MonsterFixupTestBase<MonsterFixupMongoTest.MonsterFixupMongoFixture>(fixture)
{
#if EF11
    private readonly object? _ef11Skips = Ef11TestSkips.SkipIfListed("xUnit v3 (EF11) cannot hide this non-virtual upstream test; it fails here for the reasons in EF-X111 (see the New-method override on EF8 to EF10)", "Can_build_monster_model_and_seed_data_using_dependent_navigations", "Can_build_monster_model_and_seed_data_using_navigations_with_deferred_add", "Can_build_monster_model_and_seed_data_using_principal_navigations", "Composite_fixup_happens_when_FKs_change_test", "Fixup_with_binary_keys_happens_when_FKs_or_navigations_change_test", "One_to_many_fixup_happens_when_FKs_change_test", "One_to_many_fixup_happens_when_collection_changes", "One_to_many_fixup_happens_when_reference_changes", "One_to_one_fixup_happens_when_FKs_change_test", "One_to_one_fixup_happens_when_reference_change_test");
#endif

    public class MonsterFixupMongoFixture : MonsterFixupFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        public override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("MonsterFixup");

        public override bool UseDetectChanges => true;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory ??= new MongoTestStoreFactory(
                TestServer.GetOrInitializeTestServerAsync(MongoCondition.None).GetAwaiter().GetResult(), pinDefaultQueryMode: true);

        public override MonsterContext CreateContext(DbContextOptions options)
            => new SnapshotMonsterContext(options);
    }

#if (EF10 || EF11) || EF9
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Can_build_monster_model_and_seed_data_using_FKs()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Can_build_monster_model_and_seed_data_using_FKs(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#elif EF8
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override void Can_build_monster_model_and_seed_data_using_FKs()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.Can_build_monster_model_and_seed_data_using_FKs(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override Task Can_build_monster_model_and_seed_data_using_all_navigations()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Can_build_monster_model_and_seed_data_using_all_navigations(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#elif EF8
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override void Can_build_monster_model_and_seed_data_using_all_navigations()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.Can_build_monster_model_and_seed_data_using_all_navigations(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X111")]
    public new Task Can_build_monster_model_and_seed_data_using_dependent_navigations()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF8
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override void Can_build_monster_model_and_seed_data_using_dependent_navigations()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.Can_build_monster_model_and_seed_data_using_dependent_navigations(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X111")]
    public new Task Can_build_monster_model_and_seed_data_using_navigations_with_deferred_add()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF8
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override void Can_build_monster_model_and_seed_data_using_navigations_with_deferred_add()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.Can_build_monster_model_and_seed_data_using_navigations_with_deferred_add(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X111")]
    public new Task Can_build_monster_model_and_seed_data_using_principal_navigations()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF8
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override void Can_build_monster_model_and_seed_data_using_principal_navigations()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.Can_build_monster_model_and_seed_data_using_principal_navigations(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X111")]
    public new Task Composite_fixup_happens_when_FKs_change_test()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF8
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override void Composite_fixup_happens_when_FKs_change_test()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.Composite_fixup_happens_when_FKs_change_test(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X111")]
    public new Task Fixup_with_binary_keys_happens_when_FKs_or_navigations_change_test()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF8
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override void Fixup_with_binary_keys_happens_when_FKs_or_navigations_change_test()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.Fixup_with_binary_keys_happens_when_FKs_or_navigations_change_test(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X111")]
    public new Task One_to_many_fixup_happens_when_FKs_change_test()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF8
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override void One_to_many_fixup_happens_when_FKs_change_test()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.One_to_many_fixup_happens_when_FKs_change_test(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X111")]
    public new Task One_to_many_fixup_happens_when_collection_changes()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF8
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override void One_to_many_fixup_happens_when_collection_changes()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.One_to_many_fixup_happens_when_collection_changes(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X111")]
    public new Task One_to_many_fixup_happens_when_reference_changes()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF8
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override void One_to_many_fixup_happens_when_reference_changes()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.One_to_many_fixup_happens_when_reference_changes(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X111")]
    public new Task One_to_one_fixup_happens_when_FKs_change_test()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF8
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override void One_to_one_fixup_happens_when_FKs_change_test()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.One_to_one_fixup_happens_when_FKs_change_test(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111 (upstream test is not virtual, so the failure cannot be asserted; skipped)
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact(Skip = "Upstream test is not virtual so the known failure cannot be asserted. EF-X111")]
    public new Task One_to_one_fixup_happens_when_reference_change_test()
        => Task.CompletedTask;
#pragma warning restore xUnit1024, xUnit1003
#endif
#elif EF8
    // Fails: Concurrency token on an owned entity property is rejected at model validation EF-X111
    public override void One_to_one_fixup_happens_when_reference_change_test()
        => SpecFails.Throws<global::System.NotSupportedException>(() => base.One_to_one_fixup_happens_when_reference_change_test(), "Property 'Token' on entity type 'AnOrder.Concurrency#ConcurrencyInfo' is configured as a c");
#endif
}
