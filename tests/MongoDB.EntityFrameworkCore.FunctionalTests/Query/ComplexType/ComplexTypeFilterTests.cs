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

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// Filtering on a scalar member of a complex property resolves to a dotted find filter on the native path, with
/// the same missing/null read semantics as any stored field. Driver LINQ has no complex-type oracle: under an
/// explicit <c>DriverLinq</c> mode the driver throws rather than answering.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeFilterTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Address
    {
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
    }

    public class Customer
    {
        public int Id { get; set; }
        public Address Address { get; set; } = null!;
    }

    private static Action<ModelBuilder> Model()
        => mb => mb.Entity<Customer>().ComplexProperty(c => c.Address);

    // Raw-BsonDocument seeding (ragged-seed rule): the stored shape must not depend on what materialization
    // would normalize on the way in.
    private static void Seed(IMongoCollection<BsonDocument> rawCollection, params BsonDocument[] documents)
        => rawCollection.InsertMany(documents);

    private SingleEntityDbContext<Customer> CreateContext(
        IMongoCollection<Customer> collection, MongoQueryMode mode, out SpyLoggerProvider spy)
    {
        var (loggerFactory, provider) = SpyLoggerProvider.Create();
        spy = provider;

        return SingleEntityDbContext.Create(
            collection,
            Model(),
            optionsBuilderAction: optionsBuilder =>
            {
                new MongoDbContextOptionsBuilder(optionsBuilder).UseQueryMode(mode);
                optionsBuilder.UseLoggerFactory(loggerFactory).EnableSensitiveDataLogging();
            });
    }

    [Fact]
    public void Filter_on_complex_leaf_returns_matching_documents_only()
    {
        var collection = database.CreateCollection<Customer>();
        Seed(collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName),
            BsonDocument.Parse("{ _id: 1, Address: { City: 'Seattle', Street: '1 Main' } }"),
            BsonDocument.Parse("{ _id: 2, Address: { City: 'Portland', Street: '9 SE' } }"));

        // NativeOnly: success proves the dotted-path filter went native.
        using var db = CreateContext(collection, MongoQueryMode.NativeOnly, out var spyLogger);
        var results = db.Entities.AsNoTracking().Where(c => c.Address.City == "Seattle").ToList();

        var customer = Assert.Single(results);
        Assert.Equal(1, customer.Id);
        Assert.Equal("1 Main", customer.Address.Street);

        // A plain find filter on the dotted path — not $expr, not an aggregation fallback.
        spyLogger.AssertExecutedMqlContains("Address.City");
        var mql = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutedMqlQuery);
        Assert.DoesNotContain("$expr", mql);
    }

    [Fact]
    public void Filter_on_complex_leaf_answers_the_same_under_native_mode()
    {
        var collection = database.CreateCollection<Customer>();
        Seed(collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName),
            BsonDocument.Parse("{ _id: 1, Address: { City: 'Seattle', Street: '1 Main' } }"),
            BsonDocument.Parse("{ _id: 2, Address: { City: 'Portland', Street: '9 SE' } }"),
            BsonDocument.Parse("{ _id: 3 }")); // ragged row: complex element missing entirely

        using var db = CreateContext(collection, MongoQueryMode.Native, out _);
        var results = db.Entities.AsNoTracking().Where(c => c.Address.City == "Seattle").ToList();

        var customer = Assert.Single(results);
        Assert.Equal(1, customer.Id);
    }

    [Fact]
    public void Filter_on_complex_leaf_answers_the_same_under_DriverLinq()
    {
        // Three-mode rule, DriverLinq leg. Since the provider's complex-type serializer exists (the driver's
        // class map reads complex members by element name), driver LINQ v3 can evaluate this filter too — it is
        // a working oracle for scalar-leaf complex filters, so parity is assertable here.
        var collection = database.CreateCollection<Customer>();
        Seed(collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName),
            BsonDocument.Parse("{ _id: 1, Address: { City: 'Seattle', Street: '1 Main' } }"),
            BsonDocument.Parse("{ _id: 2, Address: { City: 'Portland', Street: '9 SE' } }"));

        using var db = CreateContext(collection, MongoQueryMode.DriverLinq, out _);
        var results = db.Entities.AsNoTracking().Where(c => c.Address.City == "Seattle").ToList();

        var customer = Assert.Single(results);
        Assert.Equal(1, customer.Id);
    }
}
