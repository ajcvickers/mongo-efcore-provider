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

// Bulk operations against a TPH hierarchy. This is the case where a classification mistake destroys the wrong documents rather than
// returning the wrong rows, so it is covered explicitly, including what phase 1 actually sends to the server.

#if !EF8

using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

public class BulkOperationsInheritanceTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private abstract class Animal
    {
        public ObjectId _id { get; set; }
        public string Kind { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }

    private class Cat : Animal
    {
        public int Lives { get; set; }
    }

    private class Dog : Animal;

    private static void Configure(ModelBuilder mb)
    {
        mb.Entity<Animal>().HasDiscriminator(a => a.Kind).HasValue<Cat>("cat").HasValue<Dog>("dog");
        mb.Entity<Cat>();
        mb.Entity<Dog>();
    }

    private IMongoCollection<Animal> Seed(string name)
    {
        var collection = database.CreateCollection<Animal>(name);
        using var seed = SingleEntityDbContext.Create(collection, Configure);
        seed.AddRange(
            new Cat { _id = ObjectId.GenerateNewId(), Name = "Tom", Age = 3 },
            new Cat { _id = ObjectId.GenerateNewId(), Name = "Felix", Age = 5 },
            new Dog { _id = ObjectId.GenerateNewId(), Name = "Rex", Age = 7 });
        seed.SaveChanges();
        return collection;
    }

    private static long Count(IMongoCollection<Animal> collection, string discriminator)
        => collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName)
            .CountDocuments(new BsonDocument("_t", discriminator));

    [Fact]
    public void ExecuteDelete_with_OfType_deletes_only_that_type_via_the_two_phase_path()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = Seed(nameof(ExecuteDelete_with_OfType_deletes_only_that_type_via_the_two_phase_path));
        using var db = SingleEntityDbContext.Create(collection, loggerFactory, Configure);

        var deleted = db.Entities.OfType<Cat>().ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal(0, Count(collection, "cat"));
        Assert.Equal(1, Count(collection, "dog"));
        Assert.Contains(
            "Strategy=TwoPhase", spyLogger.GetBulkLogMessageByEventId(MongoEventId.ExecutingBulkDelete, "WriteTargets"));
    }

    [Fact]
    public void ExecuteUpdate_with_OfType_updates_only_that_type()
    {
        var collection = Seed(nameof(ExecuteUpdate_with_OfType_updates_only_that_type));
        using var db = SingleEntityDbContext.Create(collection, Configure);

        var updated = db.Entities.OfType<Cat>().ExecuteUpdate(s => s.SetProperty(a => a.Name, "renamed"));

        Assert.Equal(2, updated);
        Assert.Equal("Rex", db.Entities.OfType<Dog>().Single().Name);
        Assert.All(db.Entities.OfType<Cat>().ToList(), c => Assert.Equal("renamed", c.Name));
    }

    // The safety property: phase 1 is the read path's query, so it must carry the discriminator filter on the wire. If it did not, the
    // by-_id delete would be handed every document in the collection.
    [Fact]
    public void OfType_phase_1_sends_the_discriminator_filter_and_phase_2_acts_only_on_those_ids()
    {
        var collection = Seed(nameof(OfType_phase_1_sends_the_discriminator_filter_and_phase_2_acts_only_on_those_ids));
        using var capture = new CommandCapture(database);
        using var db = SingleEntityDbContext.Create(capture.Collection(collection), Configure);

        db.Entities.OfType<Dog>().ExecuteDelete();

        var phase1 = Assert.Single(capture.Named("aggregate"));
        Assert.Contains("_t", phase1["pipeline"].ToJson());
        Assert.Contains("dog", phase1["pipeline"].ToJson());
        var delete = Assert.Single(capture.Named("delete"));
        Assert.Single(delete["deletes"].AsBsonArray.Single()["q"]["_id"]["$in"].AsBsonArray);
        Assert.Equal(0, Count(collection, "dog"));
        Assert.Equal(2, Count(collection, "cat"));
    }

    [Fact]
    public void ExecuteDelete_over_a_derived_DbSet_deletes_only_that_type()
    {
        var collection = Seed(nameof(ExecuteDelete_over_a_derived_DbSet_deletes_only_that_type));
        using var db = SingleEntityDbContext.Create(collection, Configure);

        var deleted = db.Set<Dog>().ExecuteDelete();

        Assert.Equal(1, deleted);
        Assert.Equal(2, Count(collection, "cat"));
    }

    [Fact]
    public void ExecuteDelete_with_an_is_predicate_stays_on_the_single_command_path()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = Seed(nameof(ExecuteDelete_with_an_is_predicate_stays_on_the_single_command_path));
        using var db = SingleEntityDbContext.Create(collection, loggerFactory, Configure);

        var deleted = db.Entities.Where(a => a is Cat).ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal(1, Count(collection, "dog"));
        Assert.Contains("Strategy=SingleCommand", spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete));
    }

    [Fact]
    public void ExecuteDelete_with_OfType_of_the_root_type_stays_on_the_single_command_path()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = Seed(nameof(ExecuteDelete_with_OfType_of_the_root_type_stays_on_the_single_command_path));
        using var db = SingleEntityDbContext.Create(collection, loggerFactory, Configure);

        // OfType<Root> narrows nothing, so nothing needs the read path's discriminator filter.
        var deleted = db.Entities.OfType<Animal>().Where(a => a.Age > 4).ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Contains("Strategy=SingleCommand", spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete));
    }

    // Cast only reshapes the shaper; it is not a type filter, and the driver cannot translate it. It stays rejected, by the classifier
    // (the message names the operator), not by a driver exception from deep inside phase 1.
    [Fact]
    public void ExecuteDelete_with_Cast_is_rejected_by_the_classifier_and_deletes_nothing()
    {
        var collection = Seed(nameof(ExecuteDelete_with_Cast_is_rejected_by_the_classifier_and_deletes_nothing));
        using var db = SingleEntityDbContext.Create(collection, Configure);

        var ex = Assert.Throws<InvalidOperationException>(() => db.Entities.Cast<Cat>().ExecuteDelete());

        Assert.Contains("'Cast' operator is not supported", WholeChain(ex));
        Assert.Equal(3, db.Entities.Count());
    }

    [Fact]
    public void ExecuteUpdate_with_OfType_and_a_self_referencing_setter_is_rejected_with_a_clear_message()
    {
        var collection = Seed(nameof(ExecuteUpdate_with_OfType_and_a_self_referencing_setter_is_rejected_with_a_clear_message));
        using var db = SingleEntityDbContext.Create(collection, Configure);

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.OfType<Cat>().ExecuteUpdate(s => s.SetProperty(a => a.Age, a => a.Age + 1)));

        Assert.Contains("OfType", WholeChain(ex));
        Assert.Equal(3, Count(collection, "cat") + Count(collection, "dog"));
        Assert.Equal(3, db.Entities.Single(a => a.Name == "Tom").Age);
    }

    // EF11 dropped the "with details" translation-failure message: the specific reason travels as the inner exception.
    private static string WholeChain(Exception exception)
    {
        var messages = new List<string>();
        for (var e = exception; e != null; e = e.InnerException)
        {
            messages.Add(e.Message);
        }

        return string.Join(" | ", messages);
    }

    // A DbSet of a DERIVED type maps to its root's collection. A bulk operation on it must only touch that type: if the discriminator were
    // dropped, deleting all Cats would delete the Dog too. The preprocessor narrows the derived set to OfType<Cat>() (so it goes
    // two-phase with the discriminator filter); this pins that the narrowing reaches the bulk path.
    [Fact]
    public void ExecuteDelete_on_a_derived_DbSet_does_not_destroy_the_rest_of_the_hierarchy()
    {
        var collection = Seed(nameof(ExecuteDelete_on_a_derived_DbSet_does_not_destroy_the_rest_of_the_hierarchy));
        using var db = CreateRootAndDerivedContext(collection);

        var deleted = db.Cats.ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal(0, Count(collection, "cat"));
        Assert.Equal(1, Count(collection, "dog"));
    }

    [Fact]
    public void ExecuteUpdate_on_a_derived_DbSet_does_not_modify_the_rest_of_the_hierarchy()
    {
        var collection = Seed(nameof(ExecuteUpdate_on_a_derived_DbSet_does_not_modify_the_rest_of_the_hierarchy));
        using var db = CreateRootAndDerivedContext(collection);

        var updated = db.Cats.ExecuteUpdate(s => s.SetProperty(a => a.Name, "renamed"));

        Assert.Equal(2, updated);
        Assert.Equal("Rex", db.Animals.OfType<Dog>().Single().Name);
        Assert.All(db.Animals.OfType<Cat>().ToList(), c => Assert.Equal("renamed", c.Name));
    }

    // A property declared only on the derived type is a valid setter target once the source is narrowed to that type; the write is scoped
    // to the type, so the other types in the collection are untouched.
    [Fact]
    public void ExecuteUpdate_with_OfType_can_set_a_property_declared_only_on_the_derived_type()
    {
        var collection = Seed(nameof(ExecuteUpdate_with_OfType_can_set_a_property_declared_only_on_the_derived_type));
        using var db = SingleEntityDbContext.Create(collection, Configure);

        var updated = db.Entities.OfType<Cat>().ExecuteUpdate(s => s.SetProperty(c => c.Lives, 9));

        Assert.Equal(2, updated);
        Assert.All(db.Entities.OfType<Cat>().ToList(), c => Assert.Equal(9, c.Lives));
        Assert.Single(db.Entities.OfType<Dog>().ToList());
    }

    [Fact]
    public void ExecuteUpdate_on_a_derived_DbSet_can_set_a_property_declared_only_on_the_derived_type()
    {
        var collection = Seed(nameof(ExecuteUpdate_on_a_derived_DbSet_can_set_a_property_declared_only_on_the_derived_type));
        using var db = CreateRootAndDerivedContext(collection);

        var updated = db.Cats.ExecuteUpdate(s => s.SetProperty(c => c.Lives, 7));

        Assert.Equal(2, updated);
        Assert.All(db.Cats.ToList(), c => Assert.Equal(7, c.Lives));
        Assert.Equal(1, Count(collection, "dog"));
    }

    // Both the hierarchy root and a derived type as DbSets, over the root's collection.
    private sealed class RootAndDerivedContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Animal> Animals { get; set; } = null!;
        public DbSet<Cat> Cats { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder mb)
        {
            Configure(mb);
            mb.Entity<Animal>().ToCollection(Collection);
        }

        public static string Collection = null!;
    }

    private static RootAndDerivedContext CreateRootAndDerivedContext(IMongoCollection<Animal> collection)
    {
        RootAndDerivedContext.Collection = collection.CollectionNamespace.CollectionName;
        var options = new DbContextOptionsBuilder<RootAndDerivedContext>()
            .UseMongoDB(collection.Database.Client, collection.Database.DatabaseNamespace.DatabaseName)
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;
        return new RootAndDerivedContext(options);
    }
}

#endif
