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

// On EF8 the provider does not implement ExecuteDelete/ExecuteUpdate (the whole bulk path is #if !EF8),
// so EF Core reports the operation as untranslatable. This pins that clean-failure boundary. On EF9+ the
// feature is implemented and exercised by ExecuteDeleteTests / ExecuteUpdateTests instead.
#if EF8

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

[XUnitCollection("QueryTests")]
public class BulkOperationsUnsupportedOnEf8Tests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class SimpleEntity
    {
        public ObjectId _id { get; set; }
    }

    [Fact]
    public void ExecuteDelete_throws_translation_failure()
    {
        using var db = SingleEntityDbContext.Create(database.CreateCollection<SimpleEntity>());

        var ex = Assert.Throws<InvalidOperationException>(() => db.Entities.ExecuteDelete());
        Assert.Contains("could not be translated", ex.Message);
    }

    public class Place
    {
        public string City { get; set; } = null!;
        public double Lat { get; set; }
    }

    public class WithPlace
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Place Home { get; set; } = null!;
    }

    // Complex-property shapes (task 14): the same clean boundary, one row per shape class, and nothing is written.
    [Fact]
    public void Bulk_operations_over_complex_properties_throw_translation_failure_and_write_nothing()
    {
        var collection = database.CreateCollection<WithPlace>();
        var raw = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        var seed = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", "a" }, { "Home", new BsonDocument { { "City", "X" }, { "Lat", 1.0 } } }
        };
        raw.InsertOne(seed.DeepClone().AsBsonDocument);

        using (var db = SingleEntityDbContext.Create(collection, mb => mb.Entity<WithPlace>().ComplexProperty(w => w.Home)))
        {
            var replacement = new Place { City = "N", Lat = 2 };
            Assert.Contains("could not be translated",
                Assert.Throws<InvalidOperationException>(() => db.Entities.Where(w => w.Home.City == "X").ExecuteDelete()).Message);
            Assert.Contains("could not be translated",
                Assert.Throws<InvalidOperationException>(() => db.Entities.ExecuteUpdate(s => s.SetProperty(w => w.Home.City, "Y"))).Message);
            Assert.Contains("could not be translated",
                Assert.Throws<InvalidOperationException>(() => db.Entities.ExecuteUpdate(s => s.SetProperty(w => w.Home.Lat, w => w.Home.Lat + 1))).Message);
            Assert.Contains("could not be translated",
                Assert.Throws<InvalidOperationException>(() => db.Entities.ExecuteUpdate(s => s.SetProperty(w => w.Home, replacement))).Message);
            Assert.Contains("could not be translated",
                Assert.Throws<InvalidOperationException>(() => db.Entities.OrderBy(w => w.Home.City).Take(1).ExecuteDelete()).Message);
        }

        Assert.Equal(seed.ToJson(), raw.Find(FilterDefinition<BsonDocument>.Empty).Single().ToJson());
    }
}

#endif
