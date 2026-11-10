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
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// An owned type nested in another owned type, with a back navigation to its (owned) owner, must not send the schema
/// generator or the native materializer into infinite recursion: the inverse navigation of an ownership is never followed.
/// </summary>
public class OwnedInverseNavigationTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    public class Shop
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public Branch Branch { get; set; } = null!;
    }

    public class Branch
    {
        public string City { get; set; } = "";
        public Address Address { get; set; } = null!;
    }

    public class Address
    {
        public string Street { get; set; } = "";
        public Branch Branch { get; set; } = null!;
    }

    [Theory]
    [InlineData(MongoQueryMode.Native)]
    [InlineData(MongoQueryMode.DriverLinq)]
    [InlineData(MongoQueryMode.NativeOnly)]
    public void Insert_and_query_with_inverse_navigation_between_owned_types(MongoQueryMode mode)
    {
        var collection = database.CreateCollection<Shop>(values: mode);

        using (var db = CreateContext(collection, mode))
        {
            db.Entities.Add(new Shop
            {
                Name = "s1",
                Branch = new Branch { City = "NYC", Address = new Address { Street = "Main" } }
            });
            db.SaveChanges();
        }

        using (var db = CreateContext(collection, mode))
        {
            var shop = db.Entities.Single();
            Assert.Equal("s1", shop.Name);
            Assert.Equal("NYC", shop.Branch.City);
            Assert.Equal("Main", shop.Branch.Address.Street);
            Assert.Same(shop.Branch, shop.Branch.Address.Branch);
        }
    }

    public class Depot
    {
        public ObjectId Id { get; set; }
        public Section Section { get; set; } = null!;
    }

    public class Section
    {
        public string Label { get; set; } = "";
        public System.Collections.Generic.List<Bin> Bins { get; set; } = null!;
    }

    public class Bin
    {
        public int Number { get; set; }
        public Section Section { get; set; } = null!;
    }

    [Theory]
    [InlineData(MongoQueryMode.Native)]
    [InlineData(MongoQueryMode.DriverLinq)]
    [InlineData(MongoQueryMode.NativeOnly)]
    public void Insert_and_query_with_inverse_navigation_from_owned_collection(MongoQueryMode mode)
    {
        var collection = database.CreateCollection<Depot>(values: mode);
        Action<DbContextOptionsBuilder> options = b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        };
        Action<ModelBuilder> model = mb => mb.Entity<Depot>().OwnsOne(
            d => d.Section, s => s.OwnsMany(x => x.Bins, bin => bin.WithOwner(x => x.Section)));

        using (var db = SingleEntityDbContext.Create(collection, modelBuilderAction: model, optionsBuilderAction: options))
        {
            db.Entities.Add(new Depot { Section = new Section { Label = "A", Bins = [new Bin { Number = 1 }, new Bin { Number = 2 }] } });
            db.SaveChanges();
        }

        using (var db = SingleEntityDbContext.Create(collection, modelBuilderAction: model, optionsBuilderAction: options))
        {
            var depot = db.Entities.Single();
            Assert.Equal("A", depot.Section.Label);
            Assert.Equal([1, 2], depot.Section.Bins.Select(x => x.Number).OrderBy(x => x));
            Assert.All(depot.Section.Bins, x => Assert.Same(depot.Section, x.Section));
        }
    }

    private static SingleEntityDbContext<Shop> CreateContext(MongoDB.Driver.IMongoCollection<Shop> collection, MongoQueryMode mode)
        => SingleEntityDbContext.Create(
            collection,
            modelBuilderAction: mb => mb.Entity<Shop>().OwnsOne(
                s => s.Branch, b => b.OwnsOne(x => x.Address, a => a.WithOwner(x => x.Branch))),
            optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });
}
