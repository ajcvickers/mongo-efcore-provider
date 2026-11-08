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

using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

public class MongoTestStoreFactory : ITestStoreFactory
{
    /// <param name="testServer">The server the stores connect to.</param>
    /// <param name="pinDefaultQueryMode">
    /// True to run the stores' contexts in the default query mode (<c>Native</c>, with the driver fallback) whatever
    /// <c>MONGODB_EF_QUERY_MODE</c> says. The suites ported in #35 are baselined in the default mode only, so they pin it;
    /// the NativeOnly baselines are a follow-up. See <see cref="MongoTestStore"/>.
    /// </param>
    public MongoTestStoreFactory(TestServer testServer, bool pinDefaultQueryMode = false)
    {
        TestServer = testServer;
        PinDefaultQueryMode = pinDefaultQueryMode;
    }

    public TestServer TestServer { get; }

    public bool PinDefaultQueryMode { get; }

    public IServiceCollection AddProviderServices(IServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkMongoDB().AddSingleton<ILoggerFactory>(CreateListLoggerFactory(_ => true));

    // A non-shared test's store: initialized, cleaned and seeded for every test.
    public TestStore Create(string storeName)
        => MongoTestStore.Create(storeName, TestServer, shared: false, pinDefaultQueryMode: PinDefaultQueryMode);

    // A fixture's store: initialized once per name.
    public virtual TestStore GetOrCreate(string storeName)
        => MongoTestStore.Create(storeName, TestServer, shared: true, pinDefaultQueryMode: PinDefaultQueryMode);

    public virtual ListLoggerFactory CreateListLoggerFactory(Func<string, bool> shouldLogCategory)
        => new TestMqlLoggerFactory(shouldLogCategory);
}
