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
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Mapping;

public class WithConstructorsMongoTest(WithConstructorsMongoTest.WithConstructorsMongoFixture fixture)
    : WithConstructorsTestBase<WithConstructorsMongoTest.WithConstructorsMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

#if EF8
    public override void Query_and_update_using_constructors_with_property_parameters()
        => base.Query_and_update_using_constructors_with_property_parameters();
#endif

    // Fails: Keyless query type maps to its own empty collection (relational view/SQL query over Blogs not available) EF-X217
    public override void Query_with_keyless_type()
        => Assert.Contains("The collection was empty", Assert.ThrowsAny<System.Exception>(() => base.Query_with_keyless_type()).Message);

    public override void Query_with_context_injected()
        => base.Query_with_context_injected();

    public override void Query_with_context_injected_into_property()
        => base.Query_with_context_injected_into_property();

    public override void Query_with_context_injected_into_constructor_with_property()
        => base.Query_with_context_injected_into_constructor_with_property();

    public override void Attaching_entity_sets_context()
        => base.Attaching_entity_sets_context();

    public override void Query_with_EntityType_injected()
        => base.Query_with_EntityType_injected();

    public override void Query_with_EntityType_injected_into_property()
        => base.Query_with_EntityType_injected_into_property();

    public override void Query_with_EntityType_injected_into_constructor_with_property()
        => base.Query_with_EntityType_injected_into_constructor_with_property();

    public override void Attaching_entity_sets_EntityType()
        => base.Attaching_entity_sets_EntityType();

    public override void Query_with_StateManager_injected()
        => base.Query_with_StateManager_injected();

    public override void Query_with_StateManager_injected_into_property()
        => base.Query_with_StateManager_injected_into_property();

    public override void Query_with_StateManager_injected_into_constructor_with_property()
        => base.Query_with_StateManager_injected_into_constructor_with_property();

    public override void Attaching_entity_sets_StateManager()
        => base.Attaching_entity_sets_StateManager();

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override void Query_with_loader_injected_for_reference()
        => Assert.Contains("configured as an owned entity type", Assert.Throws<global::System.InvalidOperationException>(() => base.Query_with_loader_injected_for_reference()).Message);

    public override void Query_with_loader_injected_for_collections()
        => base.Query_with_loader_injected_for_collections();

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override async Task Query_with_loader_injected_for_reference_async()
        => Assert.Contains("configured as an owned entity type", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Query_with_loader_injected_for_reference_async())).Message);

    public override Task Query_with_loader_injected_for_collections_async()
        => base.Query_with_loader_injected_for_collections_async();

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override void Query_with_POCO_loader_injected_for_reference()
        => Assert.Contains("configured as an owned entity type", Assert.Throws<global::System.InvalidOperationException>(() => base.Query_with_POCO_loader_injected_for_reference()).Message);

    public override void Query_with_POCO_loader_injected_for_collections()
        => base.Query_with_POCO_loader_injected_for_collections();

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override async Task Query_with_loader_delegate_injected_for_reference_async()
        => Assert.Contains("configured as an owned entity type", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Query_with_loader_delegate_injected_for_reference_async())).Message);

    public override Task Query_with_loader_delegate_injected_for_collections_async()
        => base.Query_with_loader_delegate_injected_for_collections_async();

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override void Query_with_loader_injected_into_property_for_reference()
        => Assert.Contains("configured as an owned entity type", Assert.Throws<global::System.InvalidOperationException>(() => base.Query_with_loader_injected_into_property_for_reference()).Message);

    public override void Query_with_loader_injected_into_property_for_collections()
        => base.Query_with_loader_injected_into_property_for_collections();

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override void Attaching_entity_sets_lazy_loader()
        => Assert.Contains("configured as an owned entity type", Assert.Throws<global::System.InvalidOperationException>(() => base.Attaching_entity_sets_lazy_loader()).Message);

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override void Detaching_entity_resets_lazy_loader_so_it_can_be_reattached()
        => Assert.Contains("configured as an owned entity type", Assert.Throws<global::System.InvalidOperationException>(() => base.Detaching_entity_resets_lazy_loader_so_it_can_be_reattached()).Message);

    public override void Query_with_loader_injected_into_field_for_reference()
        => base.Query_with_loader_injected_into_field_for_reference();

    public override void Query_with_loader_injected_into_field_for_collections()
        => base.Query_with_loader_injected_into_field_for_collections();

    public override void Attaching_entity_sets_lazy_loader_field()
        => base.Attaching_entity_sets_lazy_loader_field();

    public override void Detaching_entity_resets_lazy_loader_field_so_it_can_be_reattached()
        => base.Detaching_entity_resets_lazy_loader_field_so_it_can_be_reattached();

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override void Attaching_entity_sets_lazy_loader_delegate()
        => Assert.Contains("configured as an owned entity type", Assert.Throws<global::System.InvalidOperationException>(() => base.Attaching_entity_sets_lazy_loader_delegate()).Message);

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override void Detaching_entity_resets_lazy_loader_delegate_so_it_can_be_reattached()
        => Assert.Contains("configured as an owned entity type", Assert.Throws<global::System.InvalidOperationException>(() => base.Detaching_entity_resets_lazy_loader_delegate_so_it_can_be_reattached()).Message);

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override void Query_with_loader_delegate_injected_into_property_for_reference()
        => Assert.Contains("configured as an owned entity type", Assert.Throws<global::System.InvalidOperationException>(() => base.Query_with_loader_delegate_injected_into_property_for_reference()).Message);

    public override void Query_with_loader_delgate_injected_into_property_for_collections()
        => base.Query_with_loader_delgate_injected_into_property_for_collections();

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override async Task Query_with_loader_delegate_injected_into_property_for_reference_async()
        => Assert.Contains("configured as an owned entity type", (await Assert.ThrowsAsync<global::System.InvalidOperationException>(() => base.Query_with_loader_delegate_injected_into_property_for_reference_async())).Message);

    public override Task Query_with_loader_delegate_injected_into_property_for_collections_async()
        => base.Query_with_loader_delegate_injected_into_property_for_collections_async();

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override void Query_with_loader_injected_into_property_via_constructor_for_reference()
        => Assert.Contains("configured as an owned entity type", Assert.Throws<global::System.InvalidOperationException>(() => base.Query_with_loader_injected_into_property_via_constructor_for_reference()).Message);

    public override void Query_with_loader_injected_into_property_via_constructor_for_collections()
        => base.Query_with_loader_injected_into_property_via_constructor_for_collections();

    // Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213
    public override void Query_with_loader_delegate_injected_into_property_via_constructor_for_reference()
        => Assert.Contains("configured as an owned entity type", Assert.Throws<global::System.InvalidOperationException>(() => base.Query_with_loader_delegate_injected_into_property_via_constructor_for_reference()).Message);

    public override void Query_with_loader_delegate_injected_into_property_via_constructor_for_collections()
        => base.Query_with_loader_delegate_injected_into_property_via_constructor_for_collections();

    public override Task Add_immutable_record()
        => base.Add_immutable_record();

#if !EF8
    public override Task Query_and_update_using_constructors_with_property_parameters()
        => base.Query_and_update_using_constructors_with_property_parameters();
#endif

    public class WithConstructorsMongoFixture : WithConstructorsFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("WithConstructors");

        public TestServer TestServer { get; private set; }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await base.InitializeAsync();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.MapSinglePropertyKeysToElementId(generateIntKeys: true); // EF-X201, EF-X205
        }
    }
}
