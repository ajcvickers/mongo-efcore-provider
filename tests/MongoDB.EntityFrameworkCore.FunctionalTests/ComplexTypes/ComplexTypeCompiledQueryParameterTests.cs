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
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// A compiled query whose argument is an ENTITY (<c>EF.CompileQuery((db, C o) =&gt; ... c.Address == o.Address)</c>): on
/// EF8/EF9 EF passes <c>o</c> as a plain <see cref="System.Linq.Expressions.ParameterExpression"/> named
/// <c>__o</c> (EF10: a <c>QueryParameterExpression</c>). The native translator took it for a ROW root (its CLR type is the
/// entity type), so <c>c.Address == o.Address</c> compared the field with itself and returned every row. A query-parameter
/// root is now never a row root: the shape declines (NativeOnly throws, Native falls back to the driver's correct rows).
/// Every answer is hand-written: ann (Paris, 1), bob (Rome, 2); probe o = {Name "x", Address (Paris, 1)}.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeCompiledQueryParameterTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Addr
    {
        public string City { get; set; } = null!;
        public int Zip { get; set; }
    }

    public class Card
    {
        public string Tag { get; set; } = null!;
    }

    public class Customer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Addr Address { get; set; } = null!;
        public Card Card { get; set; } = null!;
#if !EF8 && !EF9
        public List<Addr> Others { get; set; } = [];
#endif
    }

    private sealed class ShopContext(DbContextOptions options, string collection) : DbContext(options)
    {
        public DbSet<Customer> Customers => Set<Customer>();

        protected override void OnModelCreating(ModelBuilder mb)
            => mb.Entity<Customer>(b =>
            {
                b.ToCollection(collection);
                b.ComplexProperty(c => c.Address);
                b.OwnsOne(c => c.Card);
#if !EF8 && !EF9
                b.ComplexCollection(c => c.Others);
#endif
            });
    }

    private static readonly Customer Probe = new() { Name = "x", Address = new Addr { City = "Paris", Zip = 1 }, Card = new Card { Tag = "t-ann" } };

    private string Seed(string name)
    {
        var collection = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
        static BsonDocument A(string city, int zip) => new() { { "City", city }, { "Zip", zip } };
        database.MongoDatabase.GetCollection<BsonDocument>(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "ann" }, { "Address", A("Paris", 1) }, { "Card", new BsonDocument("Tag", "t-ann") },
                { "Others", new BsonArray { A("Paris", 1) } }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "bob" }, { "Address", A("Rome", 2) }, { "Card", new BsonDocument("Tag", "t-bob") },
                { "Others", new BsonArray { A("Rome", 2) } }
            }
        ]);
        return collection;
    }

    private ShopContext Context(string collection, MongoQueryMode mode)
    {
        var builder = new DbContextOptionsBuilder<ShopContext>()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
        return new ShopContext(builder.Options, collection);
    }

    // NativeOnly declines; Native falls back and, like DriverLinq, serves the hand-written rows.
    // A compiled query is bound to one model, and every context here builds its own: compile once per context.
    private void Declines(System.Linq.Expressions.Expression<Func<ShopContext, Customer, IQueryable<string>>> query, string[] expected,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = Seed(name);
        using (var db = Context(collection, MongoQueryMode.NativeOnly))
        {
            var compiled = EF.CompileQuery(query);
            Assert.IsType<NativeTranslationNotSupportedException>(Record.Exception(() => compiled(db, Probe).ToList()));
        }

        foreach (var mode in new[] { MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            using var db = Context(collection, mode);
            var compiled = EF.CompileQuery(query);
            var rows = compiled(db, Probe).Order(StringComparer.Ordinal).ToList();
            Assert.True(expected.SequenceEqual(rows), $"{mode}: expected [{string.Join(", ", expected)}], got [{string.Join(", ", rows)}]");
        }
    }

    private static readonly System.Linq.Expressions.Expression<Func<ShopContext, Customer, IQueryable<string>>> ComplexEquality
        = ((ShopContext db, Customer o) => db.Customers.Where(c => c.Address == o.Address).Select(c => c.Name));

    private static readonly System.Linq.Expressions.Expression<Func<ShopContext, Customer, IQueryable<string>>> ComplexNotEqual
        = ((ShopContext db, Customer o) => db.Customers.Where(c => c.Address != o.Address).Select(c => c.Name));

    private static readonly System.Linq.Expressions.Expression<Func<ShopContext, Customer, IQueryable<string>>> ComplexLeaf
        = ((ShopContext db, Customer o) => db.Customers.Where(c => c.Address.City == o.Address.City).Select(c => c.Name));

    private static readonly System.Linq.Expressions.Expression<Func<ShopContext, Customer, IQueryable<string>>> Scalar
        = ((ShopContext db, Customer o) => db.Customers.Where(c => c.Name == o.Name).Select(c => c.Name));

    private static readonly System.Linq.Expressions.Expression<Func<ShopContext, Customer, IQueryable<string>>> OwnedHop
        = ((ShopContext db, Customer o) => db.Customers.Where(c => c.Card.Tag == o.Card.Tag).Select(c => c.Name));

    private static readonly System.Linq.Expressions.Expression<Func<ShopContext, Customer, IQueryable<string>>> ReversedLeaf
        = ((ShopContext db, Customer o) => db.Customers.Where(c => o.Address.Zip < c.Address.Zip).Select(c => c.Name));

    [Fact]
    public void Whole_complex_value_equal_to_a_compiled_entity_parameters_value()
    {
        // RED (5547cfa2, EF8/EF9): NativeOnly [ann, bob] for `==` (the field compared with itself) and [] for `!=`. Now as on
        // EF10: native declines a whole value read off a query parameter, and the driver-LINQ fallback refuses a whole complex
        // value comparison loudly (ruling R1), never wrong rows. Compare the members instead (the leaf tests below).
        foreach (var query in new[] { ComplexEquality, ComplexNotEqual })
        {
            var collection = Seed(nameof(Whole_complex_value_equal_to_a_compiled_entity_parameters_value) + (query == ComplexEquality ? "eq" : "ne"));
            using (var db = Context(collection, MongoQueryMode.NativeOnly))
            {
                var compiled = EF.CompileQuery(query);
                Assert.IsType<NativeTranslationNotSupportedException>(Record.Exception(() => compiled(db, Probe).ToList()));
            }

            foreach (var mode in new[] { MongoQueryMode.Native, MongoQueryMode.DriverLinq })
            {
                using var db = Context(collection, mode);
                var compiled = EF.CompileQuery(query);
                var ex = Assert.Throws<NotSupportedException>(() => compiled(db, Probe).ToList());
                Assert.Contains("as a whole value is not supported", ex.Message);
            }
        }
    }

    [Fact]
    public void Complex_leaf_equal_to_a_compiled_entity_parameters_leaf()
    {
        // RED (5547cfa2, EF8/EF9): [ann, bob].
        Declines(ComplexLeaf, ["ann"]);
        Declines(ReversedLeaf, ["bob"]);
    }

    [Fact]
    public void Root_scalar_equal_to_a_compiled_entity_parameters_member()
        // NON-complex (pre-existing at origin/EF-322c on EF8/EF9, exception (i)): [ann, bob] natively, now [] (no Name "x").
        => Declines(Scalar, []);

    [Fact]
    public void Owned_leaf_equal_to_a_compiled_entity_parameters_owned_leaf()
        => Declines(OwnedHop, ["ann"]);

#if !EF8 && !EF9
    private static readonly System.Linq.Expressions.Expression<Func<ShopContext, Customer, IQueryable<string>>> ElementScope
        = ((ShopContext db, Customer o) => db.Customers.Where(c => c.Others.Any(a => a.City == o.Address.City)).Select(c => c.Name));

    [Fact]
    public void Complex_element_leaf_equal_to_a_compiled_entity_parameters_leaf()
        => Declines(ElementScope, ["ann"]);
#endif
}
