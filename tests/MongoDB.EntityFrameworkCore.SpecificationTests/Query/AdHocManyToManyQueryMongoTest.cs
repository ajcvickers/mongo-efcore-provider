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

#if !EF8 && !EF11

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class AdHocManyToManyQueryMongoTest : AdHocManyToManyQueryTestBase
{
#if EF10 || EF11
    public AdHocManyToManyQueryMongoTest(NonSharedFixture fixture)
        : base(fixture)
    {
    }

#endif
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task SelectMany_with_collection_selector_having_subquery()
    {
        // Fails: Generated int key not assigned (no value generation), so tracking conflicts EF-X522
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_collection_selector_having_subquery(), "cannot be tracked because another instance");
    }

    public override async Task Many_to_many_load_works_when_join_entity_has_custom_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Many_to_many_load_works_when_join_entity_has_custom_key(async), "could not be translated");
    }

    private ITestStoreFactory? _testStoreFactory;

    protected override ITestStoreFactory TestStoreFactory
        => _testStoreFactory!;

    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("AdHocM2M");

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

        await base.InitializeAsync();
    }
}

#endif
