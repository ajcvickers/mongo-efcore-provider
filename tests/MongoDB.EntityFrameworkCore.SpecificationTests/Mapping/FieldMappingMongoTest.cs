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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.Storage;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Mapping;

public class FieldMappingMongoTest(FieldMappingMongoTest.FieldMappingMongoFixture fixture)
    : FieldMappingTestBase<FieldMappingMongoTest.FieldMappingMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Enlists the context in the rollback transaction opened by the base class (as NorthwindBulkUpdatesMongoFixture does).
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => ((MongoTransactionManager)facade.GetService<IDbContextTransactionManager>()).CurrentTransaction = transaction;

    public override void Field_mapping_with_conversion_does_not_throw()
        => base.Field_mapping_with_conversion_does_not_throw();

    public override void Simple_query_auto_props(bool tracking)
        => base.Simple_query_auto_props(tracking);

    public override void Include_collection_auto_props(bool tracking)
        => base.Include_collection_auto_props(tracking);

    public override void Include_reference_auto_props(bool tracking)
        => base.Include_reference_auto_props(tracking);

    public override void Load_collection_auto_props()
        => base.Load_collection_auto_props();

    public override void Load_reference_auto_props()
        => base.Load_reference_auto_props();

    public override void Query_with_conditional_constant_auto_props(bool tracking)
        => base.Query_with_conditional_constant_auto_props(tracking);

    public override void Query_with_conditional_param_auto_props(bool tracking)
        => base.Query_with_conditional_param_auto_props(tracking);

    public override void Projection_auto_props(bool tracking)
        => base.Projection_auto_props(tracking);

#if EF8
    public override void Update_auto_props()
        => base.Update_auto_props();
#endif

    // Fails: Property hiding (new Id on derived type) conflicts in BSON class map EF-X203
    public override void Simple_query_hiding_props(bool tracking)
        => Assert.Contains("cannot use element name '_id' because it is already being used", Assert.Throws<global::MongoDB.Bson.BsonSerializationException>(() => base.Simple_query_hiding_props(tracking)).Message);

    // Fails: Property hiding (new Id on derived type) conflicts in BSON class map EF-X203
    public override void Include_collection_hiding_props(bool tracking)
        => Assert.Contains("cannot use element name '_id' because it is already being used", Assert.Throws<global::MongoDB.Bson.BsonSerializationException>(() => base.Include_collection_hiding_props(tracking)).Message);

    // Fails: int keys are not store-generated, so a second Id=0 insert hits a duplicate key EF-X205
    public override void Can_define_a_backing_field_for_a_navigation_and_query_and_update_it()
        => Assert.Contains("DuplicateKey", Assert.ThrowsAny<global::System.Exception>(() => base.Can_define_a_backing_field_for_a_navigation_and_query_and_update_it()).Message);

    // Fails: Property hiding (new Id on derived type) conflicts in BSON class map EF-X203
    public override void Include_reference_hiding_props(bool tracking)
        => Assert.Contains("cannot use element name '_id' because it is already being used", Assert.Throws<global::MongoDB.Bson.BsonSerializationException>(() => base.Include_reference_hiding_props(tracking)).Message);

    // Fails: Property hiding (new Id on derived type) conflicts in BSON class map EF-X203
    public override void Load_collection_hiding_props()
        => Assert.Contains("cannot use element name '_id' because it is already being used", Assert.Throws<global::MongoDB.Bson.BsonSerializationException>(() => base.Load_collection_hiding_props()).Message);

    // Fails: Property hiding (new Id on derived type) conflicts in BSON class map EF-X203
    public override void Load_reference_hiding_props()
        => Assert.Contains("cannot use element name '_id' because it is already being used", Assert.Throws<global::MongoDB.Bson.BsonSerializationException>(() => base.Load_reference_hiding_props()).Message);

    // Fails: Property hiding (new Id on derived type) conflicts in BSON class map EF-X203
    public override void Query_with_conditional_constant_hiding_props(bool tracking)
        => Assert.Contains("cannot use element name '_id' because it is already being used", Assert.Throws<global::MongoDB.Bson.BsonSerializationException>(() => base.Query_with_conditional_constant_hiding_props(tracking)).Message);

    // Fails: Property hiding (new Id on derived type) conflicts in BSON class map EF-X203
    public override void Query_with_conditional_param_hiding_props(bool tracking)
        => Assert.Contains("cannot use element name '_id' because it is already being used", Assert.Throws<global::MongoDB.Bson.BsonSerializationException>(() => base.Query_with_conditional_param_hiding_props(tracking)).Message);

    // Fails: Property hiding (new Id on derived type) conflicts in BSON class map EF-X203
    public override void Projection_hiding_props(bool tracking)
        => Assert.Contains("cannot use element name '_id' because it is already being used", Assert.Throws<global::MongoDB.Bson.BsonSerializationException>(() => base.Projection_hiding_props(tracking)).Message);

#if EF8
    // Fails: Property hiding (new Id on derived type) conflicts in BSON class map EF-X203
    public override void Update_hiding_props()
        => Assert.Contains("cannot use element name '_id' because it is already being used", Assert.Throws<global::MongoDB.Bson.BsonSerializationException>(() => base.Update_hiding_props()).Message);
#endif

    public override void Simple_query_full_props(bool tracking)
        => base.Simple_query_full_props(tracking);

    public override void Include_collection_full_props(bool tracking)
        => base.Include_collection_full_props(tracking);

    public override void Include_reference_full_props(bool tracking)
        => base.Include_reference_full_props(tracking);

    public override void Load_collection_full_props()
        => base.Load_collection_full_props();

    public override void Load_reference_full_props()
        => base.Load_reference_full_props();

    public override void Query_with_conditional_constant_full_props(bool tracking)
        => base.Query_with_conditional_constant_full_props(tracking);

    public override void Query_with_conditional_param_full_props(bool tracking)
        => base.Query_with_conditional_param_full_props(tracking);

    public override void Projection_full_props(bool tracking)
        => base.Projection_full_props(tracking);

#if EF8
    public override void Update_full_props()
        => base.Update_full_props();
#endif

    public override void Simple_query_full_props_with_named_fields(bool tracking)
        => base.Simple_query_full_props_with_named_fields(tracking);

    public override void Include_collection_full_props_with_named_fields(bool tracking)
        => base.Include_collection_full_props_with_named_fields(tracking);

    public override void Include_reference_full_props_with_named_fields(bool tracking)
        => base.Include_reference_full_props_with_named_fields(tracking);

    public override void Load_collection_full_props_with_named_fields()
        => base.Load_collection_full_props_with_named_fields();

    public override void Load_reference_full_props_with_named_fields()
        => base.Load_reference_full_props_with_named_fields();

    public override void Query_with_conditional_constant_full_props_with_named_fields(bool tracking)
        => base.Query_with_conditional_constant_full_props_with_named_fields(tracking);

    public override void Query_with_conditional_param_full_props_with_named_fields(bool tracking)
        => base.Query_with_conditional_param_full_props_with_named_fields(tracking);

    public override void Projection_full_props_with_named_fields(bool tracking)
        => base.Projection_full_props_with_named_fields(tracking);

#if EF8
    public override void Update_full_props_with_named_fields()
        => base.Update_full_props_with_named_fields();
#endif

    public override void Simple_query_read_only_props(bool tracking)
        => base.Simple_query_read_only_props(tracking);

    public override void Include_collection_read_only_props(bool tracking)
        => base.Include_collection_read_only_props(tracking);

    public override void Include_reference_read_only_props(bool tracking)
        => base.Include_reference_read_only_props(tracking);

    public override void Load_collection_read_only_props()
        => base.Load_collection_read_only_props();

    public override void Load_reference_read_only_props()
        => base.Load_reference_read_only_props();

    public override void Query_with_conditional_constant_read_only_props(bool tracking)
        => base.Query_with_conditional_constant_read_only_props(tracking);

    public override void Query_with_conditional_param_read_only_props(bool tracking)
        => base.Query_with_conditional_param_read_only_props(tracking);

    public override void Projection_read_only_props(bool tracking)
        => base.Projection_read_only_props(tracking);

#if EF8
    public override void Update_read_only_props()
        => base.Update_read_only_props();
#endif

    public override void Simple_query_props_with_IReadOnlyCollection(bool tracking)
        => base.Simple_query_props_with_IReadOnlyCollection(tracking);

    public override void Include_collection_props_with_IReadOnlyCollection(bool tracking)
        => base.Include_collection_props_with_IReadOnlyCollection(tracking);

    public override void Include_reference_props_with_IReadOnlyCollection(bool tracking)
        => base.Include_reference_props_with_IReadOnlyCollection(tracking);

    public override void Load_collection_props_with_IReadOnlyCollection()
        => base.Load_collection_props_with_IReadOnlyCollection();

    public override void Load_reference_props_with_IReadOnlyCollection()
        => base.Load_reference_props_with_IReadOnlyCollection();

    public override void Query_with_conditional_constant_props_with_IReadOnlyCollection(bool tracking)
        => base.Query_with_conditional_constant_props_with_IReadOnlyCollection(tracking);

    public override void Query_with_conditional_param_props_with_IReadOnlyCollection(bool tracking)
        => base.Query_with_conditional_param_props_with_IReadOnlyCollection(tracking);

    public override void Projection_props_with_IReadOnlyCollection(bool tracking)
        => base.Projection_props_with_IReadOnlyCollection(tracking);

#if EF8
    public override void Update_props_with_IReadOnlyCollection()
        => base.Update_props_with_IReadOnlyCollection();
#endif

    public override void Simple_query_read_only_props_with_named_fields(bool tracking)
        => base.Simple_query_read_only_props_with_named_fields(tracking);

    public override void Include_collection_read_only_props_with_named_fields(bool tracking)
        => base.Include_collection_read_only_props_with_named_fields(tracking);

    public override void Include_reference_read_only_props_with_named_fields(bool tracking)
        => base.Include_reference_read_only_props_with_named_fields(tracking);

    public override void Load_collection_read_only_props_with_named_fields()
        => base.Load_collection_read_only_props_with_named_fields();

    public override void Load_reference_read_only_props_with_named_fields()
        => base.Load_reference_read_only_props_with_named_fields();

    public override void Query_with_conditional_constant_read_only_props_with_named_fields(bool tracking)
        => base.Query_with_conditional_constant_read_only_props_with_named_fields(tracking);

    public override void Query_with_conditional_param_read_only_props_with_named_fields(bool tracking)
        => base.Query_with_conditional_param_read_only_props_with_named_fields(tracking);

    public override void Projection_read_only_props_with_named_fields(bool tracking)
        => base.Projection_read_only_props_with_named_fields(tracking);

#if EF8
    public override void Update_read_only_props_with_named_fields()
        => base.Update_read_only_props_with_named_fields();
#endif

    public override void Simple_query_write_only_props(bool tracking)
        => base.Simple_query_write_only_props(tracking);

    public override void Include_collection_write_only_props(bool tracking)
        => base.Include_collection_write_only_props(tracking);

    public override void Include_reference_write_only_props(bool tracking)
        => base.Include_reference_write_only_props(tracking);

    public override void Load_collection_write_only_props()
        => base.Load_collection_write_only_props();

    public override void Load_reference_write_only_props()
        => base.Load_reference_write_only_props();

    public override void Query_with_conditional_constant_write_only_props(bool tracking)
        => base.Query_with_conditional_constant_write_only_props(tracking);

    public override void Query_with_conditional_param_write_only_props(bool tracking)
        => base.Query_with_conditional_param_write_only_props(tracking);

    public override void Projection_write_only_props(bool tracking)
        => base.Projection_write_only_props(tracking);

#if EF8
    public override void Update_write_only_props()
        => base.Update_write_only_props();
#endif

    public override void Simple_query_write_only_props_with_named_fields(bool tracking)
        => base.Simple_query_write_only_props_with_named_fields(tracking);

    public override void Include_collection_write_only_props_with_named_fields(bool tracking)
        => base.Include_collection_write_only_props_with_named_fields(tracking);

    public override void Include_reference_write_only_props_with_named_fields(bool tracking)
        => base.Include_reference_write_only_props_with_named_fields(tracking);

    public override void Load_collection_write_only_props_with_named_fields()
        => base.Load_collection_write_only_props_with_named_fields();

    public override void Load_reference_write_only_props_with_named_fields()
        => base.Load_reference_write_only_props_with_named_fields();

    public override void Query_with_conditional_constant_write_only_props_with_named_fields(bool tracking)
        => base.Query_with_conditional_constant_write_only_props_with_named_fields(tracking);

    public override void Query_with_conditional_param_write_only_props_with_named_fields(bool tracking)
        => base.Query_with_conditional_param_write_only_props_with_named_fields(tracking);

    public override void Projection_write_only_props_with_named_fields(bool tracking)
        => base.Projection_write_only_props_with_named_fields(tracking);

#if EF8
    public override void Update_write_only_props_with_named_fields()
        => base.Update_write_only_props_with_named_fields();
#endif

    public override void Simple_query_fields_only(bool tracking)
        => base.Simple_query_fields_only(tracking);

    public override void Include_collection_fields_only(bool tracking)
        => base.Include_collection_fields_only(tracking);

    public override void Include_reference_fields_only(bool tracking)
        => base.Include_reference_fields_only(tracking);

    public override void Load_collection_fields_only()
        => base.Load_collection_fields_only();

    public override void Load_reference_fields_only()
        => base.Load_reference_fields_only();

    public override void Query_with_conditional_constant_fields_only(bool tracking)
        => base.Query_with_conditional_constant_fields_only(tracking);

    public override void Query_with_conditional_param_fields_only(bool tracking)
        => base.Query_with_conditional_param_fields_only(tracking);

    public override void Projection_fields_only(bool tracking)
        => base.Projection_fields_only(tracking);

#if EF8
    public override void Update_fields_only()
        => base.Update_fields_only();
#endif

    public override void Simple_query_fields_only_for_navs_too(bool tracking)
        => base.Simple_query_fields_only_for_navs_too(tracking);

    public override void Include_collection_fields_only_for_navs_too(bool tracking)
        => base.Include_collection_fields_only_for_navs_too(tracking);

    public override void Include_reference_fields_only_only_for_navs_too(bool tracking)
        => base.Include_reference_fields_only_only_for_navs_too(tracking);

    public override void Load_collection_fields_only_only_for_navs_too()
        => base.Load_collection_fields_only_only_for_navs_too();

    public override void Load_reference_fields_only_only_for_navs_too()
        => base.Load_reference_fields_only_only_for_navs_too();

    public override void Query_with_conditional_constant_fields_only_only_for_navs_too(bool tracking)
        => base.Query_with_conditional_constant_fields_only_only_for_navs_too(tracking);

    public override void Query_with_conditional_param_fields_only_only_for_navs_too(bool tracking)
        => base.Query_with_conditional_param_fields_only_only_for_navs_too(tracking);

    public override void Projection_fields_only_only_for_navs_too(bool tracking)
        => base.Projection_fields_only_only_for_navs_too(tracking);

#if EF8
    public override void Update_fields_only_only_for_navs_too()
        => base.Update_fields_only_only_for_navs_too();
#endif

#if !EF8
    // Fails: Explicit transaction cannot be shared by a second context: session disposed with the first context EF-X202
    public override async Task Update_auto_props()
        => Assert.Contains("CoreSessionHandle", (await Assert.ThrowsAsync<global::System.ObjectDisposedException>(() => base.Update_auto_props())).Message);
#endif

#if !EF8
    // Fails: Property hiding (new Id on derived type) conflicts in BSON class map EF-X203
    public override async Task Update_hiding_props()
        => Assert.Contains("cannot use element name '_id' because it is already being used", (await Assert.ThrowsAsync<global::MongoDB.Bson.BsonSerializationException>(() => base.Update_hiding_props())).Message);
#endif

#if !EF8
    // Fails: Explicit transaction cannot be shared by a second context: session disposed with the first context EF-X202
    public override async Task Update_full_props()
        => Assert.Contains("CoreSessionHandle", (await Assert.ThrowsAsync<global::System.ObjectDisposedException>(() => base.Update_full_props())).Message);
#endif

#if !EF8
    // Fails: Explicit transaction cannot be shared by a second context: session disposed with the first context EF-X202
    public override async Task Update_full_props_with_named_fields()
        => Assert.Contains("CoreSessionHandle", (await Assert.ThrowsAsync<global::System.ObjectDisposedException>(() => base.Update_full_props_with_named_fields())).Message);
#endif

#if !EF8
    // Fails: Explicit transaction cannot be shared by a second context: session disposed with the first context EF-X202
    public override async Task Update_read_only_props()
        => Assert.Contains("CoreSessionHandle", (await Assert.ThrowsAsync<global::System.ObjectDisposedException>(() => base.Update_read_only_props())).Message);
#endif

#if !EF8
    // Fails: Explicit transaction cannot be shared by a second context: session disposed with the first context EF-X202
    public override async Task Update_props_with_IReadOnlyCollection()
        => Assert.Contains("CoreSessionHandle", (await Assert.ThrowsAsync<global::System.ObjectDisposedException>(() => base.Update_props_with_IReadOnlyCollection())).Message);
#endif

#if !EF8
    // Fails: Explicit transaction cannot be shared by a second context: session disposed with the first context EF-X202
    public override async Task Update_read_only_props_with_named_fields()
        => Assert.Contains("CoreSessionHandle", (await Assert.ThrowsAsync<global::System.ObjectDisposedException>(() => base.Update_read_only_props_with_named_fields())).Message);
#endif

#if !EF8
    // Fails: Explicit transaction cannot be shared by a second context: session disposed with the first context EF-X202
    public override async Task Update_write_only_props()
        => Assert.Contains("CoreSessionHandle", (await Assert.ThrowsAsync<global::System.ObjectDisposedException>(() => base.Update_write_only_props())).Message);
#endif

#if !EF8
    // Fails: Explicit transaction cannot be shared by a second context: session disposed with the first context EF-X202
    public override async Task Update_write_only_props_with_named_fields()
        => Assert.Contains("CoreSessionHandle", (await Assert.ThrowsAsync<global::System.ObjectDisposedException>(() => base.Update_write_only_props_with_named_fields())).Message);
#endif

#if !EF8
    // Fails: Explicit transaction cannot be shared by a second context: session disposed with the first context EF-X202
    public override async Task Update_fields_only()
        => Assert.Contains("CoreSessionHandle", (await Assert.ThrowsAsync<global::System.ObjectDisposedException>(() => base.Update_fields_only())).Message);
#endif

#if !EF8
    // Fails: Explicit transaction cannot be shared by a second context: session disposed with the first context EF-X202
    public override async Task Update_fields_only_only_for_navs_too()
        => Assert.Contains("CoreSessionHandle", (await Assert.ThrowsAsync<global::System.ObjectDisposedException>(() => base.Update_fields_only_only_for_navs_too())).Message);
#endif

    public class FieldMappingMongoFixture : FieldMappingFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("FieldMapping");

        public TestServer TestServer { get; private set; }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.MapSinglePropertyKeysToElementId(); // EF-X201
        }
    }
}
