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
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Metadata.Conventions;

/// <summary>
/// The documents that are stored for types that used to be embedded by convention even though other entity types referred to
/// them (#65): each is now a document of its own.
/// </summary>
public class OwnershipByConventionTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    // ---- A type that an entity type outside its owner's document navigates to ----

    class Holder
    {
        public ObjectId _id { get; set; }
        public Game Game { get; set; }
    }

    class Game
    {
        public ObjectId _id { get; set; }
        public string Name { get; set; }
        public List<Level> Levels { get; set; }
    }

    class Level
    {
        public ObjectId _id { get; set; }
        public string Title { get; set; }
        public Game Game { get; set; }
    }

    [Fact]
    public void Type_that_another_entity_navigates_to_and_back_is_stored_in_its_own_collection()
    {
        var names = Names("referenced");
        var game = new Game { Name = "Chess" };
        var holder = new Holder { Game = game };
        var level = new Level { Title = "Opening", Game = game };

        using (var db = new ReferencedContext(database, names))
        {
            Assert.Null(db.Model.FindEntityType(typeof(Game))!.FindOwnership());
            db.Holders.Add(holder);
            db.Levels.Add(level);
            db.SaveChanges();
        }

        var holderDocument = database.GetCollection<BsonDocument>(names[0]).Find(FilterDefinition<BsonDocument>.Empty).Single();
        var gameDocument = database.GetCollection<BsonDocument>(names[2]).Find(FilterDefinition<BsonDocument>.Empty).Single();
        Assert.Equal(game._id, gameDocument["_id"].AsObjectId);
        Assert.Equal("Chess", gameDocument["Name"].AsString);

        // The holder refers to the game by key instead of containing it.
        Assert.False(holderDocument.Contains("Game"));
        Assert.Contains(holderDocument.Elements, e => e.Value == game._id);

        using var reader = new ReferencedContext(database, names);
        var read = reader.Holders.Include(h => h.Game).Single(h => h._id == holder._id);
        Assert.Equal("Chess", read.Game.Name);
    }

    class ReferencedContext(TemporaryDatabaseFixture database, string[] names) : DbContext(Options(database))
    {
        public DbSet<Holder> Holders { get; set; }
        public DbSet<Level> Levels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Holder>().ToCollection(names[0]);
            modelBuilder.Entity<Level>().ToCollection(names[1]);
            modelBuilder.Entity<Game>().ToCollection(names[2]);
        }
    }

    // ---- A type that derives from a root is stored in the root's collection, whichever was registered first ----

    class Blog
    {
        public ObjectId _id { get; set; }
        public string Title { get; set; }
    }

    class FeaturedBlog : Blog
    {
        public string Banner { get; set; }
    }

    class Magazine
    {
        public ObjectId _id { get; set; }
        public FeaturedBlog Featured { get; set; }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Derived_type_reached_through_a_navigation_is_stored_in_the_base_collection(bool blogFirst)
    {
        var names = Names("hierarchy" + blogFirst);
        var magazine = new Magazine { Featured = new FeaturedBlog { Title = "Hello", Banner = "Welcome" } };

        using (var db = new HierarchyContext(database, names, blogFirst))
        {
            db.Magazines.Add(magazine);
            db.SaveChanges();
        }

        var blogDocument = database.GetCollection<BsonDocument>(names[1]).Find(FilterDefinition<BsonDocument>.Empty).Single();
        Assert.Equal("Welcome", blogDocument["Banner"].AsString);
        Assert.Equal("FeaturedBlog", blogDocument["_t"].AsString);

        // The magazine refers to the blog by key instead of containing it.
        var magazineDocument = database.GetCollection<BsonDocument>(names[0]).Find(FilterDefinition<BsonDocument>.Empty).Single();
        Assert.False(magazineDocument.Contains("Featured"));
        Assert.Contains(magazineDocument.Elements, e => e.Value == blogDocument["_id"]);

        using var reader = new HierarchyContext(database, names, blogFirst);
        var blog = Assert.IsType<FeaturedBlog>(reader.Blogs.Single());
        Assert.Equal("Hello", blog.Title);
        Assert.Equal("Welcome", blog.Banner);
    }

    class HierarchyContext(TemporaryDatabaseFixture database, string[] names, bool blogFirst) : DbContext(Options(database))
    {
        public DbSet<Magazine> Magazines => Set<Magazine>();
        public DbSet<Blog> Blogs => Set<Blog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Registered with Entity<T>() rather than DbSet properties so that the order is the one under test.
            if (blogFirst)
            {
                modelBuilder.Entity<Blog>().ToCollection(names[1]);
                modelBuilder.Entity<Magazine>().ToCollection(names[0]);
            }
            else
            {
                modelBuilder.Entity<Magazine>().ToCollection(names[0]);
                modelBuilder.Entity<Blog>().ToCollection(names[1]);
            }
        }
    }

    // ---- A type that holds a collection of an independent entity type ----

    class Shelf
    {
        public ObjectId _id { get; set; }
        public Catalog Catalog { get; set; }
    }

    class Catalog
    {
        public ObjectId _id { get; set; }
        public ICollection<Book> Books { get; set; }
    }

    class Book
    {
        public ObjectId _id { get; set; }
        public string Title { get; set; }
    }

    [Fact]
    public void Type_that_holds_a_collection_of_a_root_is_stored_in_its_own_collection()
    {
        var names = Names("catalog");
        var catalog = new Catalog { Books = [new Book { Title = "Dune" }] };

        using (var db = new CatalogContext(database, names))
        {
            db.Add(new Shelf { Catalog = catalog });
            db.SaveChanges();
        }

        var catalogDocument = database.GetCollection<BsonDocument>(names[1]).Find(FilterDefinition<BsonDocument>.Empty).Single();
        Assert.Equal(catalog._id, catalogDocument["_id"].AsObjectId);
        Assert.False(database.GetCollection<BsonDocument>(names[0]).Find(FilterDefinition<BsonDocument>.Empty).Single().Contains("Catalog"));

        using var reader = new CatalogContext(database, names);
        Assert.Equal("Dune", reader.Set<Catalog>().Include(c => c.Books).Single().Books.Single().Title);
    }

    class CatalogContext(TemporaryDatabaseFixture database, string[] names) : DbContext(Options(database))
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>().ToCollection(names[2]);
            modelBuilder.Entity<Shelf>().ToCollection(names[0]);
            modelBuilder.Entity<Catalog>().ToCollection(names[1]);
        }
    }

    // ---- A type that nothing outside its owner refers to stays embedded ----

    class Customer
    {
        public ObjectId _id { get; set; }
        public Address Address { get; set; }
    }

    class Address
    {
        public string City { get; set; }
        public Customer Owner { get; set; }
    }

    [Fact]
    public void Type_with_a_reference_back_to_its_owner_stays_embedded()
    {
        var collection = database.CreateCollection<Customer>();

        using (var db = SingleEntityDbContext.Create(collection))
        {
            db.Entities.Add(new Customer { Address = new Address { City = "Oslo" } });
            db.SaveChanges();
        }

        var document = database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName)
            .Find(FilterDefinition<BsonDocument>.Empty).Single();
        Assert.Equal("Oslo", document["Address"]["City"].AsString);
    }

    private static string[] Names(string test)
        => new[] { "a", "b", "c" }.Select(suffix => TemporaryDatabaseFixtureBase.CreateCollectionName(test) + Guid.NewGuid().ToString("N")[..6] + suffix).ToArray();

    private static DbContextOptions Options(TemporaryDatabaseFixture database)
        => new DbContextOptionsBuilder()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;
}
