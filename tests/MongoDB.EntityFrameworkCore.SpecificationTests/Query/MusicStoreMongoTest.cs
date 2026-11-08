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
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class MusicStoreMongoTest(MusicStoreMongoTest.MusicStoreMongoFixture fixture)
    : MusicStoreTestBase<MusicStoreMongoTest.MusicStoreMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
    public override Task AddressAndPayment_RedirectToCompleteWhenSuccessful()
        => base.AddressAndPayment_RedirectToCompleteWhenSuccessful();

    public override Task AddressAndPayment_ReturnsOrderIfInvalidPromoCode()
        => base.AddressAndPayment_ReturnsOrderIfInvalidPromoCode();

    public override Task Browse_ReturnsViewWithGenre()
        => base.Browse_ReturnsViewWithGenre();

    public override Task Can_add_items_to_cart()
        => base.Can_add_items_to_cart();

    public override Task CartSummaryComponent_returns_items()
        => base.CartSummaryComponent_returns_items();

    public override Task Cart_has_items_once_they_have_been_added()
        => base.Cart_has_items_once_they_have_been_added();

    public override Task Cart_is_empty_when_no_items_have_been_added(string cartId)
        => base.Cart_is_empty_when_no_items_have_been_added(cartId);

    public override Task Complete_ReturnsErrorIfInvalidOrder()
        => base.Complete_ReturnsErrorIfInvalidOrder();

    public override Task Complete_ReturnsOrderIdIfValid()
        => base.Complete_ReturnsOrderIdIfValid();

    // Fails (some data rows): Query fails with InvalidOperationException (not triaged) EF-X311
    public override Task Custom_projection_FirstOrDefault_works(bool async)
        => SpecFails.AsyncSomeRows<global::System.InvalidOperationException>(() => base.Custom_projection_FirstOrDefault_works(async), "Operation is not valid due to the current state of the object.");

    public override Task Details_ReturnsAlbumDetail()
        => base.Details_ReturnsAlbumDetail();

    public override Task GenreMenuComponent_Returns_NineGenres()
        => base.GenreMenuComponent_Returns_NineGenres();

    public override Task Index_CreatesViewWithGenres()
        => base.Index_CreatesViewWithGenres();

#if EF8 || EF9 || EF10
    public override Task Index_GetsSixTopAlbums()
        => base.Index_GetsSixTopAlbums();
#else
    // Fails: Cross-document navigation access issue EF-216
    public override Task Index_GetsSixTopAlbums()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Index_GetsSixTopAlbums(), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.T");
#endif

#if EF10 || EF9
    // Fails: InvalidOperationException from query execution or materialization EF-X604
    public override Task Music_store_project_to_mapped_entity()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Music_store_project_to_mapped_entity());
#elif EF8
    // Fails: Query fails with InvalidOperationException (not triaged) EF-X311
    [ConditionalFact(Skip = "On EF8 the base test fails inside an async void body, so the exception cannot be caught and asserted. EF-X311")]
    public override void Music_store_project_to_mapped_entity()
        => base.Music_store_project_to_mapped_entity();
#endif

    public override Task RemoveFromCart_removes_items_from_cart()
        => base.RemoveFromCart_removes_items_from_cart();
    // </generated>

    public class MusicStoreMongoFixture : MusicStoreFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("MusicStore");

        public TestServer TestServer { get; private set; }

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await base.InitializeAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
        }

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder.ConfigureWarnings(w => w.Ignore(MongoEventId.ColumnAttributeWithTypeUsed)));

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;
    }
}
