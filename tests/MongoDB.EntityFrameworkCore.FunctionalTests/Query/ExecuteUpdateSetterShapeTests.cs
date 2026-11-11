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

// SerializeConstant claims to serialize setter constants with the same property serializer the write pipeline uses, so enum / Guid /
// DateTime handling matches inserts and SaveChanges. Round-tripping through EF only proves self-consistency, so the representation test
// writes one document through each path and compares the RAW stored BSON.

#if !EF8

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

public class ExecuteUpdateSetterShapeTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private enum Priority { Low, High }

    private class RichOrder
    {
        public ObjectId _id { get; set; }
        public string? Status { get; set; }
        public Priority Priority { get; set; }
        public Guid Correlation { get; set; }
        public DateTime OccurredAt { get; set; }
        public int Quantity { get; set; }
    }

    private IMongoCollection<BsonDocument> Raw(IMongoCollection<RichOrder> collection)
        => collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

    [Fact]
    public void Setting_null_writes_an_explicit_null()
    {
        var collection = database.CreateCollection<RichOrder>();
        using (var seed = SingleEntityDbContext.Create(collection))
        {
            seed.Add(new RichOrder { _id = ObjectId.GenerateNewId(), Status = "open" });
            seed.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection);

        var updated = db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Status, (string?)null));

        Assert.Equal(1, updated);
        var raw = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).Single();
        Assert.True(raw.Contains("Status"));
        Assert.Equal(BsonType.Null, raw["Status"].BsonType);
        Assert.Null(db.Entities.Single().Status);
    }

    [Fact]
    public void Setting_enum_Guid_and_DateTime_stores_the_same_BSON_as_SaveChanges()
    {
        var collection = database.CreateCollection<RichOrder>();
        var correlation = Guid.NewGuid();
        var occurredAt = new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        var viaSaveChanges = ObjectId.GenerateNewId();
        var viaBulk = ObjectId.GenerateNewId();

        using (var seed = SingleEntityDbContext.Create(collection))
        {
            seed.Add(new RichOrder
            {
                _id = viaSaveChanges, Priority = Priority.High, Correlation = correlation, OccurredAt = occurredAt
            });
            seed.Add(new RichOrder { _id = viaBulk });
            seed.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection);

        var updated = db.Entities.Where(o => o._id == viaBulk).ExecuteUpdate(
            s => s.SetProperty(o => o.Priority, Priority.High)
                .SetProperty(o => o.Correlation, correlation)
                .SetProperty(o => o.OccurredAt, occurredAt));

        Assert.Equal(1, updated);

        var raw = Raw(collection);
        var expected = raw.Find(Builders<BsonDocument>.Filter.Eq("_id", viaSaveChanges)).Single();
        var actual = raw.Find(Builders<BsonDocument>.Filter.Eq("_id", viaBulk)).Single();
        foreach (var field in new[] { "Priority", "Correlation", "OccurredAt" })
        {
            // Type first: a Guid stored as a string, or an enum as a string, has the right value and the wrong representation.
            Assert.Equal(expected[field].BsonType, actual[field].BsonType);
            Assert.Equal(expected[field], actual[field]);
        }
    }

    [Fact]
    public void Self_referencing_setter_over_a_field_absent_from_the_document_writes_an_explicit_null()
    {
        // A pipeline-form $set evaluates $add over a missing field as null, so the property becomes null rather than staying absent. This
        // is document-model behaviour that callers should know about; the test pins it rather than blessing it.
        var collection = database.CreateCollection<RichOrder>();
        Raw(collection).InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Status", "open" } });

        using var db = SingleEntityDbContext.Create(collection);

        var updated = db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Quantity, o => o.Quantity + 1));

        Assert.Equal(1, updated);
        var raw = Raw(collection).Find(FilterDefinition<BsonDocument>.Empty).Single();
        Assert.True(raw.Contains("Quantity"), "the field is written, not left absent");
        Assert.Equal(BsonType.Null, raw["Quantity"].BsonType);
    }
}

#endif
