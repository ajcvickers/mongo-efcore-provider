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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// <c>MinBy</c>/<c>MaxBy</c> over a collection reached through a collection navigation, read as an ordering key
/// (<c>OrderBy(c =&gt; c.Orders.MinBy(o =&gt; o.Lines.Sum(l =&gt; l.Qty)).Id)</c>), written in the form EF Core lowers it to
/// (<c>OrderBy(..).Select(o =&gt; (int?)o.Id).FirstOrDefault()</c>) so that every EF version translates it: the key of each order may itself read the lines of that
/// order, or be another <c>MinBy</c>/<c>MaxBy</c>, so the orders array carries a lines array per order. Compared with LINQ to objects
/// over the same seed, including a tie between orders (the first wins), an order without lines and a customer with no orders (left out
/// of every query: LINQ would throw for it).
/// </summary>
public class NativeReferencedOrderedFirstTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Cust
    {
        public string Id { get; set; } = "";
        public List<Ord> Orders { get; set; } = [];
    }

    public class Ord
    {
        public int Id { get; set; }
        public string? CustId { get; set; }
        public Cust? Cust { get; set; }
        public List<Line> Lines { get; set; } = [];
    }

    public class Line
    {
        public int Id { get; set; }
        public int OrdId { get; set; }
        public Ord? Ord { get; set; }
        public int Qty { get; set; }
    }

    // customer, order, line quantities
    //   A1: 1 -> 5, 5 (sum 10, max 5)   2 -> 3 (sum 3, max 3)   3 -> 1, 2 (sum 3, max 2): a tie with order 2 on the sum
    //   A2: 4 -> 9                       5 -> none (sum 0)
    //   B1: 6 -> 4, 4, 4 (sum 12, max 4)
    //   D1: 20 -> 1  30 -> 1: a tie, the first (20) wins     E1: 25 -> 7     C0: no orders
    private static readonly (string Cust, int Order, int[] Qtys)[] Data =
    [
        ("A1", 1, [5, 5]), ("A1", 2, [3]), ("A1", 3, [1, 2]),
        ("A2", 4, [9]), ("A2", 5, []),
        ("B1", 6, [4, 4, 4]),
        ("D1", 20, [1]), ("D1", 30, [1]),
        ("E1", 25, [7])
    ];

    private static readonly string[] Customers = ["A1", "A2", "B1", "C0", "D1", "E1"];

    private string[] Seed()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var names = new[]
        {
            TemporaryDatabaseFixtureBase.CreateCollectionName("rofCusts") + suffix,
            TemporaryDatabaseFixtureBase.CreateCollectionName("rofOrds") + suffix,
            TemporaryDatabaseFixtureBase.CreateCollectionName("rofLines") + suffix
        };

        database.MongoDatabase.GetCollection<BsonDocument>(names[0]).InsertMany(
            Customers.Select(c => new BsonDocument { { "_id", c } }));
        database.MongoDatabase.GetCollection<BsonDocument>(names[1]).InsertMany(
            Data.Select(d => new BsonDocument { { "_id", d.Order }, { "CustId", d.Cust } }));

        var lineId = 0;
        var lines = Data.SelectMany(d => d.Qtys.Select(q => new BsonDocument { { "_id", ++lineId }, { "OrdId", d.Order }, { "Qty", q } })).ToList();
        database.MongoDatabase.GetCollection<BsonDocument>(names[2]).InsertMany(lines);
        return names;
    }

    // The same data as objects, for the LINQ to objects oracle.
    private static List<Cust> Oracle()
    {
        var lineId = 0;
        var customers = Customers.Select(c => new Cust { Id = c }).ToList();
        foreach (var (custId, orderId, qtys) in Data)
        {
            var order = new Ord { Id = orderId, CustId = custId, Lines = qtys.Select(q => new Line { Id = ++lineId, OrdId = orderId, Qty = q }).ToList() };
            customers.Single(c => c.Id == custId).Orders.Add(order);
        }

        return customers;
    }

    private void Assert2<T>(Func<Ctx, T> query, Func<IEnumerable<Cust>, T> oracle)
    {
        var names = Seed();
        var expected = oracle(Oracle().Where(c => c.Id != "C0"));

        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native })
        {
            using var db = Create(names, mode);
            Assert.Equal(expected, query(db));
        }
    }

    private static IQueryable<Cust> WithOrders(Ctx db) => db.Set<Cust>().Where(c => c.Id != "C0");

    [Fact]
    public void MinBy_over_an_aggregate_of_the_nested_collection()
        => Assert2(
            db => WithOrders(db).OrderBy(c => c.Orders.OrderBy(o => o.Lines.Sum(l => l.Qty)).Select(o => (int?)o.Id).FirstOrDefault()).ThenBy(c => c.Id).Select(c => c.Id).ToList(),
            customers => customers.OrderBy(c => c.Orders.MinBy(o => o.Lines.Sum(l => l.Qty))!.Id).ThenBy(c => c.Id).Select(c => c.Id).ToList());

    [Fact]
    public void MaxBy_over_an_aggregate_of_the_nested_collection()
        => Assert2(
            db => WithOrders(db).OrderByDescending(c => c.Orders.OrderByDescending(o => 5 + o.Lines.Sum(l => l.Qty)).Select(o => (int?)o.Id).FirstOrDefault()).ThenBy(c => c.Id).Select(c => c.Id).ToList(),
            customers => customers.OrderByDescending(c => c.Orders.MaxBy(o => 5 + o.Lines.Sum(l => l.Qty))!.Id).ThenBy(c => c.Id).Select(c => c.Id).ToList());

    // Order 5 of A2 has no lines, so its MinBy would be null: only the orders with lines are asked.
    [Fact]
    public void MinBy_over_a_MinBy_of_the_nested_collection()
        => Assert2(
            db => WithOrders(db).Where(c => c.Id != "A2").OrderBy(c => c.Orders.OrderBy(o => 5 + o.Lines.OrderBy(l => l.Qty).Select(l => (int?)l.Qty).FirstOrDefault()).Select(o => (int?)o.Id).FirstOrDefault()).ThenBy(c => c.Id).Select(c => c.Id).ToList(),
            customers => customers.Where(c => c.Id != "A2").OrderBy(c => c.Orders.MinBy(o => 5 + o.Lines.MinBy(l => l.Qty)!.Qty)!.Id).ThenBy(c => c.Id).Select(c => c.Id).ToList());

    // As above, for MaxBy.
    [Fact]
    public void MaxBy_over_a_MaxBy_of_the_nested_collection()
        => Assert2(
            db => WithOrders(db).Where(c => c.Id != "A2").OrderByDescending(c => c.Orders.OrderByDescending(o => 5 + o.Lines.OrderByDescending(l => l.Qty).Select(l => (int?)l.Qty).FirstOrDefault()).Select(o => (int?)o.Id).FirstOrDefault())
                .ThenBy(c => c.Id).Select(c => c.Id).ToList(),
            customers => customers.Where(c => c.Id != "A2")
                .OrderByDescending(c => c.Orders.MaxBy(o => 5 + o.Lines.MaxBy(l => l.Qty)!.Qty)!.Id).ThenBy(c => c.Id).Select(c => c.Id).ToList());

    // A group with no orders has no first element: the value is null, and `== null` is true for it (not MISSING, which `$eq` would not match).
    [Fact]
    public void The_value_of_an_empty_group_is_null()
    {
        var names = Seed();
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native })
        {
            using var db = Create(names, mode);
            Assert.Equal(
                ["C0"],
                db.Set<Cust>().Where(c => c.Orders.OrderByDescending(o => o.Id).Select(o => o.CustId).FirstOrDefault() == null).Select(c => c.Id).ToList());
            Assert.Equal(
                5,
                db.Set<Cust>().Count(c => c.Orders.OrderByDescending(o => o.Id).Select(o => o.CustId).FirstOrDefault() != null));
        }
    }

    [Fact]
    public void A_tie_between_orders_selects_the_first()
    {
        // D1's orders 20 and 30 tie on the key: the key of D1 is 20, so D1 sorts before E1 (25). Were 30 chosen, E1 would come first.
        var names = Seed();
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native })
        {
            using var db = Create(names, mode);
            var ids = db.Set<Cust>().Where(c => c.Id == "D1" || c.Id == "E1")
                .OrderBy(c => c.Orders.OrderBy(o => o.Lines.Sum(l => l.Qty) * 0 + 1).Select(o => (int?)o.Id).FirstOrDefault()).Select(c => c.Id).ToList();
            Assert.Equal(["D1", "E1"], ids);
        }
    }

    [Fact]
    public void MinBy_with_a_filter_on_the_outer_entity_and_a_sum_selector_is_native()
    {
        // The same ordering key as a root MinBy/MaxBy, which is what the conformance suite asks.
        var names = Seed();
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native })
        {
            using var db = Create(names, mode);
            var min = WithOrders(db).OrderBy(c => c.Id).Take(3).OrderBy(c => c.Orders.OrderBy(o => 5 + o.Lines.Sum(l => l.Qty)).Select(o => (int?)o.Id).FirstOrDefault()).First();
            var max = WithOrders(db).OrderBy(c => c.Id).Take(3).OrderByDescending(c => c.Orders.OrderByDescending(o => 5 + o.Lines.Sum(l => l.Qty)).Select(o => (int?)o.Id).FirstOrDefault()).First();
            var oracle = Oracle().Where(c => c.Id != "C0").OrderBy(c => c.Id).Take(3).ToList();
            Assert.Equal(oracle.MinBy(c => c.Orders.MinBy(o => 5 + o.Lines.Sum(l => l.Qty))!.Id)!.Id, min!.Id);
            Assert.Equal(oracle.MaxBy(c => c.Orders.MaxBy(o => 5 + o.Lines.Sum(l => l.Qty))!.Id)!.Id, max!.Id);
        }
    }

    private static List<string> OrderedBySign(Ctx db, int sign)
        => WithOrders(db).OrderBy(c => c.Orders.OrderBy(o => o.Lines.Sum(l => l.Qty) * sign).Select(o => (int?)o.Id).FirstOrDefault()).ThenBy(c => c.Id).Select(c => c.Id).ToList();

    [Theory]
    [InlineData(MongoQueryMode.NativeOnly)]
    [InlineData(MongoQueryMode.Native)]
    public void A_parameter_in_the_key_is_read_per_execution(MongoQueryMode mode)
    {
        var names = Seed();
        using var db = Create(names, mode);

        // Ascending by sum picks the smallest sums; negating the sums picks the largest (a baked sign would repeat the first rows).
        var customers = Oracle().Where(c => c.Id != "C0").ToList();
        List<string> Expected(int sign)
            => customers.OrderBy(c => c.Orders.MinBy(o => o.Lines.Sum(l => l.Qty) * sign)!.Id).ThenBy(c => c.Id).Select(c => c.Id).ToList();

        NativeModeAssert.TwiceWithDifferentValues(v => OrderedBySign(db, (int)v!), 1, Expected(1), -1, Expected(-1));
    }

    private Ctx Create(string[] names, MongoQueryMode mode)
    {
        var options = new DbContextOptionsBuilder<Ctx>()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName, o => o.UseQueryMode(mode))
            .ReplaceService<IModelCacheKeyFactory, UniqueModelCacheKeyFactory>()
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;
        return new Ctx(options, names);
    }

    private sealed class UniqueModelCacheKeyFactory : IModelCacheKeyFactory
    {
        private static int _count;
        public object Create(DbContext context, bool designTime) => Interlocked.Increment(ref _count);
    }

    private sealed class Ctx(DbContextOptions<Ctx> options, string[] names) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Cust>().ToCollection(names[0]);
            mb.Entity<Ord>(b =>
            {
                b.ToCollection(names[1]);
                b.HasOne(o => o.Cust).WithMany(c => c.Orders).HasForeignKey(o => o.CustId);
            });
            mb.Entity<Line>(b =>
            {
                b.ToCollection(names[2]);
                b.HasOne(l => l.Ord).WithMany(o => o.Lines).HasForeignKey(l => l.OrdId);
            });
        }
    }
}
