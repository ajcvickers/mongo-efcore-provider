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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// Pins the join-scope complex-projection contract: a complex property projected from a JOIN's inner side takes
/// the driver-LINQ fallback under Native mode (NativeJoinScopeProjectionBinder does not yet admit complex
/// leaves) and that fallback reads CORRECT values — driver LINQ, via the entity serializer's complex member
/// bindings, is a working oracle for whole complex reads. Under NativeOnly the fallback is forbidden, so the
/// query declines at compile time. When a native join-scope complex leaf lands, the Native leg's assertions
/// stay valid — they are mode-agnostic on purpose.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeJoinProjectionTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Address
    {
        public string City { get; set; } = "";
        public string Zip { get; set; } = "";
    }

    private class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public Address Address { get; set; } = null!;
    }

    private class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Label { get; set; } = "";
    }

    private sealed record CollectionNames(string Customers, string Orders);

    private class JoinDbContext : DbContext
    {
        private readonly CollectionNames _names;
        private readonly MongoQueryMode? _mode;

        public JoinDbContext(
            TemporaryDatabaseFixture database, CollectionNames names, MongoQueryMode? mode = null)
            : base(
                new DbContextOptionsBuilder<JoinDbContext>()
                    .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
                    .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                    .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                    .Options)
        {
            _names = names;
            _mode = mode;
        }

        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Customer>(b =>
            {
                b.ToCollection(_names.Customers);
                b.HasKey(c => c.Id);
                b.Property(c => c.Id).HasElementName("_id");
                b.ComplexProperty(c => c.Address);
            });
            modelBuilder.Entity<Order>(b =>
            {
                b.ToCollection(_names.Orders);
                b.HasKey(o => o.Id);
                b.Property(o => o.Id).HasElementName("_id");
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_mode is { } mode)
            {
                new MongoDbContextOptionsBuilder(optionsBuilder).UseQueryMode(mode);
            }
        }
    }

    private CollectionNames SeedJoinData(string name)
    {
        var customerNames = TemporaryDatabaseFixtureBase.CreateCollectionName(name + "-c")
            + Guid.NewGuid().ToString("N")[..8];
        var rawCustomers = database.MongoDatabase.GetCollection<BsonDocument>(customerNames);
        rawCustomers.InsertMany(
        [
            new BsonDocument { { "_id", 1 }, { "Name", "Alpha" },
                { "Address", new BsonDocument { { "City", "NYC" }, { "Zip", "10001" } } } },
            new BsonDocument { { "_id", 2 }, { "Name", "Beta" },
                { "Address", new BsonDocument { { "City", "LA" }, { "Zip", "90001" } } } },
        ]);

        var orderNames = TemporaryDatabaseFixtureBase.CreateCollectionName(name + "-o")
            + Guid.NewGuid().ToString("N")[..8];
        var rawOrders = database.MongoDatabase.GetCollection<BsonDocument>(orderNames);
        rawOrders.InsertMany(
        [
            new BsonDocument { { "_id", 10 }, { "CustomerId", 1 }, { "Label", "first" } },
            new BsonDocument { { "_id", 11 }, { "CustomerId", 2 }, { "Label", "second" } },
            new BsonDocument { { "_id", 13 }, { "CustomerId", 99 }, { "Label", "unmatched" } },
        ]);

        return new CollectionNames(customerNames, orderNames);
    }

    [Fact]
    public void Join_projecting_complex_property_reads_correct_values_under_native_fallback()
    {
        var names = SeedJoinData(nameof(Join_projecting_complex_property_reads_correct_values_under_native_fallback));

        static List<(int OrderId, string Name, string? City, string? Zip)> Run(JoinDbContext context)
            => (from o in context.Orders
                join c in context.Customers on o.CustomerId equals c.Id
                select new { o.Id, c.Name, c.Address })
                .ToList()
                .Select(r => (r.Id, r.Name, (string?)r.Address?.City, (string?)r.Address?.Zip))
                .OrderBy(r => r.Item1)
                .ToList();

        List<(int, string, string?, string?)> driver;
        using (var context = new JoinDbContext(database, names, MongoQueryMode.DriverLinq))
        {
            driver = Run(context);
        }

        // The fallback must be an oracle here, never silent wrong data: same values as driver LINQ.
        using (var context = new JoinDbContext(database, names, MongoQueryMode.Native))
        {
            var native = Run(context);
            Assert.Equal(driver, native);
        }

        Assert.Equal(2, driver.Count); // the unmatched order's left side drops under an inner join
        Assert.Contains(((10, "Alpha", "NYC", "10001")), driver);
        Assert.Contains(((11, "Beta", "LA", "90001")), driver);
    }

    [Fact]
    public void NativeOnly_declines_join_projecting_complex_property()
    {
        // No native join-scope complex leaf yet: under NativeOnly the fallback is forbidden, so the query
        // declines at compile time rather than silently changing path.
        var names = SeedJoinData(nameof(NativeOnly_declines_join_projecting_complex_property));

        using var context = new JoinDbContext(database, names, MongoQueryMode.NativeOnly);

        Assert.Throws<NativeTranslationNotSupportedException>(
            () => (from o in context.Orders
                   join c in context.Customers on o.CustomerId equals c.Id
                   select new { o.Id, c.Address })
                .ToList());
    }

    private sealed class IgnoreCacheKeyFactory : IModelCacheKeyFactory
    {
        private static int _count;

        public object Create(DbContext context, bool designTime)
            => Interlocked.Increment(ref _count); // one model (and compiled-query set) per context instance
    }
}
