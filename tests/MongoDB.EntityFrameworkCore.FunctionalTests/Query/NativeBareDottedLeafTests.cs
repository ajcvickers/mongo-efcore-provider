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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// Bare projections of a stored field at a DOTTED path (owned-reference hops, a composite-key component under
/// <c>_id</c>), which go native under the synthetic <c>_v</c> alias (<c>NativeProjectionBinder.TryDeriveSyntheticAlias</c>
/// gate 1f; formerly the EF-362 decline). Every row is NativeOnly + Native + DriverLinq against a hand-written answer.
/// </summary>
[XUnitCollection("QueryTests")]
public class NativeBareDottedLeafTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Blog
    {
        public ObjectId Id { get; set; }
        public string Title { get; set; } = "";
        public Home Home { get; set; } = null!;
    }

    public class Home
    {
        public string City { get; set; } = "";
        public int Floor { get; set; }
        public Geo Geo { get; set; } = null!;
        public int Code { get; set; }
    }

    public class Geo
    {
        public string Country { get; set; } = "";
    }

    public class Line
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string Label { get; set; } = "";
    }

    private static void BlogModel(ModelBuilder mb)
        => mb.Entity<Blog>().OwnsOne(b => b.Home, h =>
        {
            h.OwnsOne(x => x.Geo);
            h.Property(x => x.Code).HasConversion<string>();
        });

    private IMongoCollection<Blog> SeedBlogs(string name)
    {
        var raw = database.MongoDatabase.GetCollection<BsonDocument>(Unique(name));
        raw.InsertMany(
        [
            Blog("a", "Bristol", 3, "UK", "30"),
            Blog("b", "Cardiff", 1, "Wales", "4"),
            Blog("c", "Bristol", 2, "UK", "100")
        ]);
        return database.MongoDatabase.GetCollection<Blog>(raw.CollectionNamespace.CollectionName);

        static BsonDocument Blog(string title, string city, int floor, string country, string code)
            => new()
            {
                { "_id", ObjectId.GenerateNewId() }, { "Title", title },
                { "Home", new BsonDocument { { "City", city }, { "Floor", floor }, { "Code", code }, { "Geo", new BsonDocument { { "Country", country } } } } }
            };
    }

    private static List<T> RunBlogs<T>(IMongoCollection<Blog> collection, MongoQueryMode mode, Func<IQueryable<Blog>, IEnumerable<T>> query)
    {
        using var db = SingleEntityDbContext.Create(collection, BlogModel, optionsBuilderAction: b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });
        return query(db.Entities.AsNoTracking()).ToList();
    }

    [Fact]
    public void Bare_owned_hop_scalar()
    {
        var collection = SeedBlogs(nameof(Bare_owned_hop_scalar));
        NativeModeAssert.NativeAndExpected(m => RunBlogs(collection, m, q => q.OrderBy(b => b.Title).Select(b => b.Home.City)),
            ["Bristol", "Cardiff", "Bristol"]);
        NativeModeAssert.NativeAndExpected(m => RunBlogs(collection, m, q => q.OrderBy(b => b.Title).Select(b => b.Home.Floor)),
            [3, 1, 2]);
    }

    [Fact]
    public void Bare_multi_hop_owned_scalar()
    {
        var collection = SeedBlogs(nameof(Bare_multi_hop_owned_scalar));
        NativeModeAssert.NativeAndExpected(m => RunBlogs(collection, m, q => q.OrderBy(b => b.Title).Select(b => b.Home.Geo.Country)),
            ["UK", "Wales", "UK"]);
    }

    [Fact]
    public void Bare_owned_hop_scalar_distinct()
    {
        var collection = SeedBlogs(nameof(Bare_owned_hop_scalar_distinct));
        NativeModeAssert.NativeAndExpected(
            m => RunBlogs(collection, m, q => q.Select(b => b.Home.City).Distinct().ToList().Order(StringComparer.Ordinal)),
            ["Bristol", "Cardiff"]);
        NativeModeAssert.NativeAndExpected(
            m => RunBlogs(collection, m, q => q.Select(b => b.Home.Geo.Country).Distinct().ToList().Order(StringComparer.Ordinal)),
            ["UK", "Wales"]);
    }

    [Fact]
    public void OrderBy_over_owned_hop_leaf_projected_bare()
    {
        var collection = SeedBlogs(nameof(OrderBy_over_owned_hop_leaf_projected_bare));
        NativeModeAssert.NativeAndExpected(
            m => RunBlogs(collection, m, q => q.OrderBy(b => b.Home.Floor).Select(b => b.Home.City)),
            ["Cardiff", "Bristol", "Bristol"]);
        NativeModeAssert.NativeAndExpected(
            m => RunBlogs(collection, m, q => q.OrderByDescending(b => b.Home.Geo.Country).ThenBy(b => b.Home.Floor).Select(b => b.Home.Floor)),
            [1, 2, 3]);
    }

    [Fact]
    public void Bare_value_converted_owned_hop_leaf_still_declines()
    {
        // Gate 1f admits only default-serialized fields; a converted dotted leaf keeps declining natively and the
        // fallback reads it through the converter ("30" -> 30).
        var collection = SeedBlogs(nameof(Bare_value_converted_owned_hop_leaf_still_declines));
        Assert.Equal([30, 4, 100],
            NativeModeAssert.DeclinesCleanly(m => RunBlogs(collection, m, q => q.OrderBy(b => b.Title).Select(b => b.Home.Code))));
    }

    [Fact]
    public void Bare_composite_key_component()
    {
        var raw = database.MongoDatabase.GetCollection<BsonDocument>(Unique(nameof(Bare_composite_key_component)));
        raw.InsertMany(
        [
            new BsonDocument { { "_id", new BsonDocument { { "OrderId", 1 }, { "CustomerId", 10 } } }, { "Label", "a" } },
            new BsonDocument { { "_id", new BsonDocument { { "OrderId", 2 }, { "CustomerId", 20 } } }, { "Label", "b" } },
            new BsonDocument { { "_id", new BsonDocument { { "OrderId", 3 }, { "CustomerId", 10 } } }, { "Label", "c" } }
        ]);
        var collection = database.MongoDatabase.GetCollection<Line>(raw.CollectionNamespace.CollectionName);

        List<T> Run<T>(MongoQueryMode mode, Func<IQueryable<Line>, IEnumerable<T>> query)
        {
            using var db = SingleEntityDbContext.Create(collection, mb => mb.Entity<Line>().HasKey(x => new { x.OrderId, x.CustomerId }),
                optionsBuilderAction: b =>
                {
                    b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                    new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
                });
            return query(db.Entities.AsNoTracking()).ToList();
        }

        NativeModeAssert.NativeAndExpected(m => Run(m, q => q.OrderBy(l => l.Label).Select(l => l.CustomerId)), [10, 20, 10]);
        NativeModeAssert.NativeAndExpected(m => Run(m, q => q.Select(l => l.CustomerId).Distinct().ToList().Order()), [10, 20]);
    }

    private static string Unique(string name)
        => TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
}
