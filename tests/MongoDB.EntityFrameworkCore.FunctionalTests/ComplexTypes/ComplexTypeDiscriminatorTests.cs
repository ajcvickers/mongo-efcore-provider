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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

#if !EF8 && !EF9
/// <summary>
/// An optional complex property whose members are all nullable has a discriminator (EF10 <c>HasDiscriminator()</c>), the way
/// EF tells a null property from one whose members are all null. A stored subdocument whose discriminator is null or
/// missing reads as a NULL property whatever its other members hold (EF's rule; SaveChanges writes the discriminator as an
/// ordinary leaf, and setting it to null on a tracked entry stores <c>Discriminator: null</c> beside the members).
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeDiscriminatorTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class OptProp
    {
        public bool? OptionalValue { get; set; }
    }

    public class Ent
    {
        public Guid Id { get; set; }
        public OptProp? Prop { get; set; }
    }

    private static void Configure(ModelBuilder mb)
    {
        var e = mb.Entity<Ent>();
        e.HasKey(p => p.Id);
        e.Property(p => p.Id).ValueGeneratedNever();
        e.ComplexProperty(p => p.Prop).HasDiscriminator();
    }

    private static SingleEntityDbContext<Ent> Context(IMongoCollection<Ent> collection, MongoQueryMode mode, QueryTrackingBehavior tracking)
        => SingleEntityDbContext.Create(collection, Configure, null, b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            b.UseQueryTrackingBehavior(tracking);
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    [Fact]
    public void Loaded_entity_carries_its_discriminator_so_update_keeps_the_property_present()
    {
        var collection = database.CreateCollection<Ent>(
            TemporaryDatabaseFixtureBase.CreateCollectionName(nameof(Loaded_entity_carries_its_discriminator_so_update_keeps_the_property_present))
            + Guid.NewGuid().ToString("N")[..6]);
        var raw = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        using (var db = Context(collection, MongoQueryMode.Native, QueryTrackingBehavior.TrackAll))
        {
            db.Entities.Add(new Ent { Id = Guid.NewGuid(), Prop = new OptProp { OptionalValue = true } });
            db.SaveChanges();
        }

        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            using (var db = Context(collection, mode, QueryTrackingBehavior.TrackAll))
            {
                var ent = db.Entities.Single();
                var discriminator = db.Entry(ent).ComplexProperty(e => e.Prop!).Property("Discriminator");
                Assert.Equal("OptProp", discriminator.CurrentValue);
                Assert.False(discriminator.IsModified);

                // An unchanged load writes nothing; Update() marks every property modified and writes it as loaded.
                Assert.Equal(EntityState.Unchanged, db.Entry(ent).State);
                db.Update(ent);
                db.SaveChanges();
            }

            var stored = raw.Find(FilterDefinition<BsonDocument>.Empty).Single()["Prop"].AsBsonDocument;
            Assert.Equal("OptProp", stored["Discriminator"].AsString);
            Assert.True(stored["OptionalValue"].AsBoolean);

            using (var db = Context(collection, mode, QueryTrackingBehavior.NoTracking))
            {
                Assert.Equal(true, db.Entities.Single().Prop?.OptionalValue);
            }
        }
    }

    [Fact]
    public void Null_missing_or_value_discriminator_decides_whether_the_property_is_null()
    {
        var collection = database.CreateCollection<Ent>(
            TemporaryDatabaseFixtureBase.CreateCollectionName(nameof(Null_missing_or_value_discriminator_decides_whether_the_property_is_null))
            + Guid.NewGuid().ToString("N")[..6]);
        var raw = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        Guid present, discriminatorNulled, clrNull;
        using (var db = Context(collection, MongoQueryMode.Native, QueryTrackingBehavior.TrackAll))
        {
            var a = new Ent { Id = Guid.NewGuid(), Prop = new OptProp { OptionalValue = true } };
            var b = new Ent { Id = Guid.NewGuid(), Prop = new OptProp { OptionalValue = true } };
            var c = new Ent { Id = Guid.NewGuid(), Prop = null };
            db.Entities.AddRange(a, b, c);
            (present, discriminatorNulled, clrNull) = (a.Id, b.Id, c.Id);
            db.Entry(b).ComplexProperty(e => e.Prop!).Property("Discriminator").CurrentValue = null;
            db.SaveChanges();
        }

        // The stored forms: a present value carries its discriminator, the nulled one stores null beside its members.
        var docs = raw.Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["_id"].AsGuid);
        Assert.Equal("OptProp", docs[present]["Prop"]["Discriminator"].AsString);
        Assert.True(docs[discriminatorNulled]["Prop"]["Discriminator"].IsBsonNull);
        Assert.True(docs[discriminatorNulled]["Prop"]["OptionalValue"].AsBoolean);
        Assert.True(docs[clrNull]["Prop"].IsBsonNull);

        // A subdocument with members but no discriminator at all is also a null property.
        var missing = Guid.NewGuid();
        raw.InsertOne(new BsonDocument { { "_id", new BsonBinaryData(missing, GuidRepresentation.Standard) }, { "Prop", new BsonDocument("OptionalValue", true) } });

        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            foreach (var tracking in new[] { QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking })
            {
                using var db = Context(collection, mode, tracking);
                var read = db.Entities.ToList().ToDictionary(e => e.Id);
                Assert.Equal(true, read[present].Prop?.OptionalValue);
                Assert.Null(read[discriminatorNulled].Prop);
                Assert.Null(read[clrNull].Prop);
                Assert.Null(read[missing].Prop);

                var projected = db.Entities.Select(e => e.Prop).ToList();
                Assert.Equal(1, projected.Count(p => p is not null));
            }
        }
    }
}
#endif
