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

    private static readonly MongoQueryMode[] AllModes =
        [MongoQueryMode.Native, MongoQueryMode.DriverLinq, MongoQueryMode.NativeOnly];

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
