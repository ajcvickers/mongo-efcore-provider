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
using System.Linq.Expressions;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// Differential-correctness functional test (real DB) for the native-join-scope-nested-projection design
/// (<c>docs/superpowers/specs/2026-09-08-native-join-scope-nested-projection-design.md</c>): a nested
/// anonymous-projection member sourced from a reference-Include join scope —
/// <c>new { CustomerInfo = new { Name = o.Customer!.Name } }</c> over <c>Orders.Include(o => o.Customer)</c>
/// — matches an in-memory LINQ oracle, including the UNMATCHED-FK (dangling reference, no matching Customer
/// document) case. Since a reference Include over an OPTIONAL navigation lowers to a LEFT-OUTER
/// <c>$lookup</c>/<c>$unwind</c>, the unmatched row is exactly where a native-vs-fallback bug in the nested
/// leaf's dotted-path read (<c>BsonBinding.CreateGetPropertyValueAtPath</c>'s absent-intermediate-segment
/// handling) would most likely hide — a REQUIRED navigation would just drop that row via an inner unwind,
/// never exercising the null-nested-leaf path at all.
/// </summary>
/// <remarks>
/// The oracle is NOT built by literally executing <c>selector</c> against a plain in-memory object graph
/// with a null <c>Customer</c> navigation: <c>o.Customer!.Name</c>'s null-forgiving <c>!</c> operator emits
/// no runtime null-check at all (it is a compile-time-only warning suppression), so compiling and running
/// that exact expression tree via <c>IQueryable</c>-over-<c>List&lt;T&gt;</c> (which just runs the compiled
/// delegate — no EF null-propagating SQL/MQL translation is involved) would throw
/// <see cref="NullReferenceException"/> for the unmatched row, not produce <see langword="null"/>. EF's own
/// translation of this exact shape against a LEFT-OUTER join is what makes an absent join match propagate as
/// null instead of throwing — that translated behavior is precisely the thing under test, so the oracle
/// instead performs the left-outer join explicitly with <c>GroupJoin</c>/<c>DefaultIfEmpty</c> (ordinary,
/// well-understood LINQ-to-Objects semantics, sharing no code with the provider) and applies the SAME nested
/// anonymous shape to the joined pair.
/// </remarks>
[XUnitCollection("QueryTests")]
public class NativeJoinScopeNestedProjectionTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Order
    {
        public ObjectId Id { get; set; }
        public int OrderNo { get; set; }
        public ObjectId? CustomerId { get; set; }
        public Customer? Customer { get; set; }
    }

    private class Customer
    {
        public ObjectId Id { get; set; }
        public string? Name { get; set; }
    }

    // The exact motivating shape from the design doc, adapted to this project's model: a top-level scalar
    // sibling (OrderNo, off the query root) alongside a NESTED anonymous member (CustomerInfo) whose own
    // member (Name) is sourced from the join's INNER side (Customer, an optional/nullable reference
    // navigation reached via Include). Depth-1 join scope, single level of nesting — squarely Design §"in
    // scope (v1)".
    private static readonly Expression<Func<Order, object>> Selector =
        o => new { o.OrderNo, CustomerInfo = new { o.Customer!.Name } };

    [Theory]
    [InlineData(MongoQueryMode.Native)]
    [InlineData(MongoQueryMode.NativeOnly)]
    public void Nested_projection_over_reference_include_matches_oracle(MongoQueryMode mode)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var ordersName = TemporaryDatabaseFixtureBase.CreateCollectionName(
            nameof(Nested_projection_over_reference_include_matches_oracle)) + "O" + suffix;
        var customersName = TemporaryDatabaseFixtureBase.CreateCollectionName(
            nameof(Nested_projection_over_reference_include_matches_oracle)) + "C" + suffix;

        var matchedCustomerId = ObjectId.GenerateNewId();
        var matchedOrderId = ObjectId.GenerateNewId();
        var unmatchedOrderId = ObjectId.GenerateNewId();
        // A dangling FK: generated but NEVER inserted into the Customers collection, so the $lookup this
        // reference Include emits genuinely finds no match for it (not merely null-CustomerId, which would
        // be a different, less interesting case — the $lookup itself still runs and fails to match).
        var danglingCustomerId = ObjectId.GenerateNewId();

        using (var seed = new JoinScopeDbContext(database, ordersName, customersName, MongoQueryMode.DriverLinq))
        {
            seed.Set<Customer>().Add(new Customer { Id = matchedCustomerId, Name = "Alfreds" });
            seed.Set<Order>().AddRange(
                new Order { Id = matchedOrderId, OrderNo = 1, CustomerId = matchedCustomerId },   // matched
                new Order { Id = unmatchedOrderId, OrderNo = 2, CustomerId = danglingCustomerId }); // unmatched FK
            seed.SaveChanges();
        }

        using var db = new JoinScopeDbContext(database, ordersName, customersName, mode);

#if EF8 || EF9
        // The feature under test (native translation of a nested anonymous-projection member sourced from a
        // reference-Include join scope) is EF10-scoped only — see the design doc referenced in the class
        // remarks. On EF8/EF9, MongoQueryMode.NativeOnly correctly forbids the driver-LINQ fallback this shape
        // still needs, so it must throw rather than execute; MongoQueryMode.Native (which allows the fallback)
        // is unaffected and is exercised below like on EF10.
        if (mode == MongoQueryMode.NativeOnly)
        {
            Assert.Throws<NativeTranslationNotSupportedException>(() =>
                db.Set<Order>().Include(o => o.Customer)
                    .OrderBy(o => o.OrderNo)
                    .Select(Selector)
                    .ToList());
            return;
        }
#endif

        var actual = db.Set<Order>().Include(o => o.Customer)
            .OrderBy(o => o.OrderNo)
            .Select(Selector)
            .ToList();

        // The independent oracle: an ordinary LINQ-to-Objects left-outer join (see the class remarks for why
        // this — not a literal re-execution of Selector against a null Customer — is the correct oracle
        // construction here), applying the SAME nested anonymous shape to the (Order, Customer?) pair.
        var orderSeeds = new[]
        {
            new { Id = matchedOrderId, OrderNo = 1, CustomerId = (ObjectId?)matchedCustomerId },
            new { Id = unmatchedOrderId, OrderNo = 2, CustomerId = (ObjectId?)danglingCustomerId }
        };
        var customerSeeds = new[] { new { Id = matchedCustomerId, Name = "Alfreds" } };

        var oracle = orderSeeds
            .GroupJoin(customerSeeds, o => o.CustomerId, c => (ObjectId?)c.Id, (o, cs) => new { o, cs })
            .SelectMany(x => x.cs.DefaultIfEmpty(), (x, c) => new { x.o.OrderNo, CustomerInfo = new { Name = c?.Name } })
            .OrderBy(x => x.OrderNo)
            .Cast<object>()
            .ToList();

        Assert.Equal(2, actual.Count);
        // Structural equality of the (compiler-unified, since the shapes match exactly) anonymous type —
        // proves the native result agrees with the oracle on BOTH rows, not merely on row count.
        Assert.Equal(oracle, actual);

        // Named-value assertions too, so a bug that happened to preserve anonymous-type Equals (e.g. via a
        // coincidentally-matching hash/serialization round trip) can't hide: read the actual matched/dangling
        // values back explicitly.
        dynamic matchedRow = actual[0];
        dynamic unmatchedRow = actual[1];
        Assert.Equal(1, (int)matchedRow.OrderNo);
        Assert.Equal("Alfreds", (string)matchedRow.CustomerInfo.Name);
        Assert.Equal(2, (int)unmatchedRow.OrderNo);
        Assert.Null((string?)unmatchedRow.CustomerInfo.Name);
    }

    private class JoinScopeDbContext : DbContext
    {
        private readonly string _ordersCollection;
        private readonly string _customersCollection;

        public JoinScopeDbContext(
            TemporaryDatabaseFixture database, string ordersCollection, string customersCollection,
            MongoQueryMode mode)
            : base(new DbContextOptionsBuilder<JoinScopeDbContext>()
                .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName,
                    b => b.UseQueryMode(mode))
                .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                .Options)
        {
            _ordersCollection = ordersCollection;
            _customersCollection = customersCollection;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>().ToCollection(_customersCollection);
            modelBuilder.Entity<Order>(b =>
            {
                b.ToCollection(_ordersCollection);
                b.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId).IsRequired(false);
            });
        }

        private sealed class IgnoreCacheKeyFactory : IModelCacheKeyFactory
        {
            private static int _count;
            public object Create(DbContext context, bool designTime) => Interlocked.Increment(ref _count);
        }
    }
}
