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

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Tracking;

public class CompositeKeyEndToEndMongoTest(CompositeKeyEndToEndMongoTest.CompositeKeyEndToEndMongoFixture fixture)
    : CompositeKeyEndToEndTestBase<CompositeKeyEndToEndMongoTest.CompositeKeyEndToEndMongoFixture>(fixture)
{
    public class CompositeKeyEndToEndMongoFixture : CompositeKeyEndToEndFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("CompositeKeyEndToEnd");

        public TestServer TestServer { get; private set; } = null!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await base.InitializeAsync();
        }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;
    }

    // Fails: REGRESSION vs main: after SaveChanges the composite-key row cannot be queried back ("Sequence contains no elements") EF-X703
    // The upstream test is not virtual, so it is hidden here and the failure asserted instead.
#pragma warning disable xUnit1024, xUnit1003
    [ConditionalFact]
    public new Task Can_use_two_non_generated_integers_as_composite_key_end_to_end()
        => SpecFails.ThrowsAsync<global::System.InvalidOperationException>(
            () => base.Can_use_two_non_generated_integers_as_composite_key_end_to_end(), "Sequence contains no elements");
#pragma warning restore xUnit1024, xUnit1003

#if EF10 || EF9
    // Fails: Store-generated/computed value or update semantics differ (no database defaults, computed columns or generated keys) EF-X108
    public override Task Can_use_generated_values_in_composite_key_end_to_end()
        => SpecFails.ThrowsAsync<global::Xunit.Sdk.XunitException>(() => base.Can_use_generated_values_in_composite_key_end_to_end(), "Assert.True() Failure");
#elif EF8
    // Fails: No value generator for int/long/enum/converted keys EF-X103
    public override Task Can_use_generated_values_in_composite_key_end_to_end()
        => SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Can_use_generated_values_in_composite_key_end_to_end(), "The property 'Unicorn.Id");
#endif
}
