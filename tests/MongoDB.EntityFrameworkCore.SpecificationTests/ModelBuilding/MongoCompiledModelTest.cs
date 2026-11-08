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

// EF-X710: excluded on EF11. The generated compiled model throws InvalidCastException (ValueComparer<object> to ValueComparer<int>) from its
// static constructor on a background thread, which crashes the whole test host.
#if !EF8 && !EF11
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.EntityFrameworkCore.Design;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.ModelBuilding;

public class MongoCompiledModelTest
#if EF10 || EF11
    (NonSharedFixture fixture) : CompiledModelTestBase(fixture)
#else
    : CompiledModelTestBase
#endif
{
    private ITestStoreFactory? _testStoreFactory;

    protected override TestHelpers TestHelpers
        => MongoTestHelpers.Instance;

#if EF11
    protected override ITestStoreFactory NonSharedTestStoreFactory
#else
    protected override ITestStoreFactory TestStoreFactory
#endif
        => _testStoreFactory!;

#if EF11
    protected override string NonSharedStoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("CompiledModel");
#else
    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("CompiledModel");
#endif

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server);
        await base.InitializeAsync();
    }

    // Fails: Properties of types without a Mongo mapping (IPAddress, ...) are discovered as owned entity types EF-X412
    public override async Task No_NativeAOT()
        => Assert.Contains(
            "No suitable constructor was found for",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.No_NativeAOT())).Message);

    // Fails: Properties of types without a Mongo mapping (IPAddress, ...) are discovered as owned entity types EF-X412
#pragma warning disable xUnit1024 // BigModel is also the name of a protected overload in the base class
    public override async Task BigModel()
        => Assert.Contains(
            "No suitable constructor was found for",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.BigModel())).Message);
#pragma warning restore xUnit1024

    // Fails: Properties of types without a Mongo mapping (IPAddress, ...) are discovered as owned entity types EF-X412
    public override async Task ComplexTypes()
        => Assert.Contains(
            "No suitable constructor was found for",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => base.ComplexTypes())).Message);

    protected override BuildSource AddReferences(BuildSource build, string filePath = "")
    {
        build = base.AddReferences(build, filePath);
        build.References.Add(BuildReference.ByName("MongoDB.EntityFrameworkCore", copyLocal: true));
        build.References.Add(BuildReference.ByName("MongoDB.Driver", copyLocal: true));
        build.References.Add(BuildReference.ByName("MongoDB.Bson", copyLocal: true));
        return build;
    }

    protected override void AddDesignTimeServices(IServiceCollection services)
        => new MongoDesignTimeServices().ConfigureDesignTimeServices(services);
}
#endif
