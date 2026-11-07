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
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class ConferencePlannerMongoTest(ConferencePlannerMongoTest.ConferencePlannerMongoFixture fixture)
    : ConferencePlannerTestBase<ConferencePlannerMongoTest.ConferencePlannerMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
    // Fails: Cross-document navigation access issue EF-216
    public override Task AttendeesController_AddSession()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.AttendeesController_AddSession(), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.T");

    public override Task AttendeesController_AddSession_bad_attendee()
        => base.AttendeesController_AddSession_bad_attendee();

    public override Task AttendeesController_AddSession_bad_session()
        => base.AttendeesController_AddSession_bad_session();

    public override Task AttendeesController_Get()
        => base.AttendeesController_Get();

#if EF10
    // Fails: Cross-document navigation access issue EF-216
    public override Task AttendeesController_GetSessions()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.AttendeesController_GetSessions(), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.T");
#elif EF9 || EF8
    // Fails: Cross-document navigation access issue EF-216
    public override Task AttendeesController_GetSessions()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.AttendeesController_GetSessions(), "The LINQ expression 'DbSet<Session>()");
#endif

    public override Task AttendeesController_Post_with_existing_attendee()
        => base.AttendeesController_Post_with_existing_attendee();

    public override Task AttendeesController_Post_with_new_attendee()
        => base.AttendeesController_Post_with_new_attendee();

    // Fails: Cross-document navigation access issue EF-216
    public override Task AttendeesController_RemoveSession()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.AttendeesController_RemoveSession(), "Unsupported cross-DbSet query between 'Microsoft.EntityFrameworkCore.T");

    public override Task AttendeesController_RemoveSession_bad_attendee()
        => base.AttendeesController_RemoveSession_bad_attendee();

    public override Task AttendeesController_RemoveSession_bad_session()
        => base.AttendeesController_RemoveSession_bad_session();

#if EF10
    // Fails (some data rows): Relationship/navigation discovery differs (navigations become owned by convention) EF-X308
    public override Task SearchController_Search(string searchTerm, int totalCount, int sessionCount)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.SearchController_Search(searchTerm, totalCount, sessionCount), "Object reference not set to an instance of an object.");
#elif EF9 || EF8
    // Fails: Cross-document navigation access issue EF-216
    public override Task SearchController_Search(string searchTerm, int totalCount, int sessionCount)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.SearchController_Search(searchTerm, totalCount, sessionCount), "The LINQ expression 'DbSet<Session>()");
#endif

    // Fails: Single/First over empty result in test body (not triaged) EF-X310
    public override Task SessionsController_Delete()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.SessionsController_Delete(), "Sequence contains no elements");

    public override Task SessionsController_Delete_with_bad_ID()
        => base.SessionsController_Delete_with_bad_ID();

#if EF10
    public override Task SessionsController_Get()
        => base.SessionsController_Get();
#elif EF9 || EF8
    // Fails: Cross-document navigation access issue EF-216
    public override Task SessionsController_Get()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.SessionsController_Get(), "The LINQ expression 'DbSet<Session>()");
#endif

    // Fails: Single/First over empty result in test body (not triaged) EF-X310
    public override Task SessionsController_Get_with_ID()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.SessionsController_Get_with_ID(), "Sequence contains no elements");

#if EF10
    public override Task SessionsController_Get_with_bad_ID()
        => base.SessionsController_Get_with_bad_ID();
#elif EF9 || EF8
    // Fails: Cross-document navigation access issue EF-216
    public override Task SessionsController_Get_with_bad_ID()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.SessionsController_Get_with_bad_ID(), "The LINQ expression 'DbSet<Session>()");
#endif

    public override Task SessionsController_Post()
        => base.SessionsController_Post();

    public override Task SessionsController_Put()
        => base.SessionsController_Put();

    public override Task SessionsController_Put_with_bad_ID()
        => base.SessionsController_Put_with_bad_ID();

    public override Task SpeakersController_GetSpeaker_with_ID()
        => base.SpeakersController_GetSpeaker_with_ID();

    public override Task SpeakersController_GetSpeaker_with_bad_ID()
        => base.SpeakersController_GetSpeaker_with_bad_ID();

    public override Task SpeakersController_GetSpeakers()
        => base.SpeakersController_GetSpeakers();
    // </generated>

    public class ConferencePlannerMongoFixture : ConferencePlannerFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("ConferencePlanner");

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

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;
    }
}
