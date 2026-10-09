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

// The Associations/ComplexProperties test bases only exist in EF10.
#if EF10

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.Associations.ComplexProperties;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query.Associations.ComplexProperties;

public class ComplexPropertiesMongoFixture : ComplexPropertiesFixtureBase
{
    private readonly string _storeName = TestDatabaseNamer.GetUniqueDatabaseName("ComplexProperties");

    protected override string StoreName
        => _storeName;

    private ITestStoreFactory? _testStoreFactory;

    protected override ITestStoreFactory TestStoreFactory
        => _testStoreFactory!;

    public TestServer TestServer { get; private set; }

    public override async Task InitializeAsync()
    {
        TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(TestServer);

        await base.InitializeAsync();
    }

    protected override bool UsePooling
        => false;

    public TestMqlLoggerFactory TestMqlLoggerFactory
        => (TestMqlLoggerFactory)ServiceProvider.GetRequiredService<ILoggerFactory>();

    protected override bool ShouldLogCategory(string logCategory)
        => logCategory == DbLoggerCategory.Query.Name;

    // EF's BulkUpdatesAsserter wraps every AssertDelete/AssertUpdate in a transaction that is rolled
    // back, so the shared seed is left unchanged across the repeated (sync + async) runs.
    // The transaction is begun on one context and the bulk operation is executed on a *second*
    // context; this hook enlists that second context in the same MongoDB session so DeleteMany /
    // UpdateMany participate in — and are rolled back with — the outer transaction. Both contexts
    // are built against the shared TestServer.Client, so the session handle is valid across them
    // (CurrentTransaction's internal setter is reachable here via InternalsVisibleTo). The bulk
    // executor reads the session from Database.CurrentTransaction (see PrepareBulk in
    // MongoShapedQueryCompilingExpressionVisitor).
    public override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => ((MongoTransactionManager)facade.GetService<IDbContextTransactionManager>()).CurrentTransaction = transaction;
}

#endif
