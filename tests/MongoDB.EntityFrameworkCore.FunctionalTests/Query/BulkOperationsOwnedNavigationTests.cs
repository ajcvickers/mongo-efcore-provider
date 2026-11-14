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

#if !EF8

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

// #69: an entity type with an owned navigation is auto-included by convention, which leaves a Select(Include) wrapper in the captured
// source chain (always on EF9; for ExecuteDelete on EF10 and later). It must not stop the type being the source of a bulk operation, and
// a bulk operation must neither change which documents it affects nor touch the owned data of the documents it does not.
public class BulkOperationsOwnedNavigationTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Geo
    {
        public string Region { get; set; } = null!;
    }

    private class Address
    {
        public string City { get; set; } = null!;
        public Geo? Location { get; set; }
    }

    private class Note
    {
        public string Text { get; set; } = null!;
    }

    private class Customer
    {
        public ObjectId _id { get; set; }
        public string Name { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int Rank { get; set; }
        public Address? Home { get; set; }
        public List<Note> Notes { get; set; } = [];
    }

    private static void ConfigureOwned(ModelBuilder mb)
        => mb.Entity<Customer>(b =>
        {
            b.OwnsOne(c => c.Home, h => h.OwnsOne(a => a.Location));
            b.OwnsMany(c => c.Notes);
        });

    private static void ConfigureOwnedWithFilter(ModelBuilder mb)
    {
        ConfigureOwned(mb);
        mb.Entity<Customer>().HasQueryFilter(c => c.Status != "closed");
    }

    private IMongoCollection<Customer> SeedCustomers(bool async, string name, Action<ModelBuilder> configure)
    {
        var collection = database.CreateCollection<Customer>(name, async);
        using var seed = SingleEntityDbContext.Create(collection, configure);
        seed.AddRange(
            new Customer
            {
                _id = ObjectId.GenerateNewId(), Name = "alice", Status = "open", Rank = 1,
                Home = new Address { City = "London", Location = new Geo { Region = "EU" } },
                Notes = [new Note { Text = "a1" }, new Note { Text = "a2" }]
            },
            new Customer
            {
                _id = ObjectId.GenerateNewId(), Name = "bob", Status = "open", Rank = 2,
                Home = new Address { City = "Paris", Location = new Geo { Region = "EU" } },
                Notes = [new Note { Text = "b1" }]
            },
            new Customer { _id = ObjectId.GenerateNewId(), Name = "carol", Status = "closed", Rank = 3 });
        seed.SaveChanges();
        return collection;
    }

    // Reads the rows back through a new context, so what is asserted is what is stored, with every owned navigation loaded.
    private static List<Customer> ReadBack(IMongoCollection<Customer> collection, Action<ModelBuilder> configure, bool ignoreFilters = true)
    {
        using var db = SingleEntityDbContext.Create(collection, configure);
        var query = ignoreFilters ? db.Entities.IgnoreQueryFilters() : db.Entities;
        return query.AsNoTracking().OrderBy(c => c.Rank).ToList();
    }

    private static async Task<int> Delete<T>(IQueryable<T> source, bool async) where T : class
        => async ? await source.ExecuteDeleteAsync() : source.ExecuteDelete();

    private static async Task<int> SetStatus(IQueryable<Customer> source, string status, bool async)
        => async
            ? await source.ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, status))
            : source.ExecuteUpdate(s => s.SetProperty(c => c.Status, status));

    private static void AssertBobAndCarolUntouched(IReadOnlyList<Customer> rows)
    {
        var bob = Assert.Single(rows, c => c.Name == "bob");
        Assert.Equal("Paris", bob.Home!.City);
        Assert.Equal("EU", bob.Home.Location!.Region);
        Assert.Equal(["b1"], bob.Notes.Select(n => n.Text));

        var carol = Assert.Single(rows, c => c.Name == "carol");
        Assert.Null(carol.Home);
        Assert.Empty(carol.Notes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_with_Where_over_owned_navigations(bool async)
    {
        var collection = SeedCustomers(async, nameof(ExecuteDelete_with_Where_over_owned_navigations), ConfigureOwned);
        using var db = SingleEntityDbContext.Create(collection, ConfigureOwned);

        var deleted = await Delete(db.Entities.Where(c => c.Name == "alice"), async);

        Assert.Equal(1, deleted);
        var rows = ReadBack(collection, ConfigureOwned);
        Assert.Equal(["bob", "carol"], rows.Select(c => c.Name));
        AssertBobAndCarolUntouched(rows);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_without_Where_over_owned_navigations(bool async)
    {
        var collection = SeedCustomers(async, nameof(ExecuteDelete_without_Where_over_owned_navigations), ConfigureOwned);
        using var db = SingleEntityDbContext.Create(collection, ConfigureOwned);

        var deleted = await Delete(db.Entities, async);

        Assert.Equal(3, deleted);
        Assert.Empty(ReadBack(collection, ConfigureOwned));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_with_Where_on_owned_member(bool async)
    {
        var collection = SeedCustomers(async, nameof(ExecuteDelete_with_Where_on_owned_member), ConfigureOwned);
        using var db = SingleEntityDbContext.Create(collection, ConfigureOwned);

        var deleted = await Delete(db.Entities.Where(c => c.Home!.City == "London"), async);

        Assert.Equal(1, deleted);
        var rows = ReadBack(collection, ConfigureOwned);
        Assert.Equal(["bob", "carol"], rows.Select(c => c.Name));
        AssertBobAndCarolUntouched(rows);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_with_Skip_and_Take_over_owned_navigations(bool async)
    {
        var collection = SeedCustomers(async, nameof(ExecuteDelete_with_Skip_and_Take_over_owned_navigations), ConfigureOwned);
        using var db = SingleEntityDbContext.Create(collection, ConfigureOwned);

        // Two-phase: phase 1 reads the target ids through the read path, so the wrapper must not reach it either.
        var deleted = await Delete(db.Entities.OrderBy(c => c.Rank).Skip(1).Take(1), async);

        Assert.Equal(1, deleted);
        var rows = ReadBack(collection, ConfigureOwned);
        Assert.Equal(["alice", "carol"], rows.Select(c => c.Name));
        Assert.Equal(["a1", "a2"], rows[0].Notes.Select(n => n.Text));
        Assert.Equal("London", rows[0].Home!.City);
        Assert.Null(rows[1].Home);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_applies_the_query_filter_over_owned_navigations(bool async)
    {
        var collection = SeedCustomers(async, nameof(ExecuteDelete_applies_the_query_filter_over_owned_navigations), ConfigureOwnedWithFilter);
        using var db = SingleEntityDbContext.Create(collection, ConfigureOwnedWithFilter);

        var deleted = await Delete(db.Entities, async);

        // carol is "closed", which the filter hides.
        Assert.Equal(2, deleted);
        var rows = ReadBack(collection, ConfigureOwnedWithFilter);
        Assert.Equal(["carol"], rows.Select(c => c.Name));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteUpdate_scalar_setter_over_owned_navigations(bool async)
    {
        var collection = SeedCustomers(async, nameof(ExecuteUpdate_scalar_setter_over_owned_navigations), ConfigureOwned);
        using var db = SingleEntityDbContext.Create(collection, ConfigureOwned);

        var updated = await SetStatus(db.Entities.Where(c => c.Name == "bob"), "done", async);

        Assert.Equal(1, updated);
        var rows = ReadBack(collection, ConfigureOwned);
        Assert.Equal(["open", "done", "closed"], rows.Select(c => c.Status));
        AssertBobAndCarolUntouched(rows);
        Assert.Equal(["a1", "a2"], rows[0].Notes.Select(n => n.Text));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteUpdate_without_Where_over_owned_navigations(bool async)
    {
        var collection = SeedCustomers(async, nameof(ExecuteUpdate_without_Where_over_owned_navigations), ConfigureOwned);
        using var db = SingleEntityDbContext.Create(collection, ConfigureOwned);

        var updated = await SetStatus(db.Entities, "done", async);

        Assert.Equal(3, updated);
        var rows = ReadBack(collection, ConfigureOwned);
        Assert.All(rows, c => Assert.Equal("done", c.Status));
        AssertBobAndCarolUntouched(rows);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteUpdate_with_Where_on_owned_member(bool async)
    {
        var collection = SeedCustomers(async, nameof(ExecuteUpdate_with_Where_on_owned_member), ConfigureOwned);
        using var db = SingleEntityDbContext.Create(collection, ConfigureOwned);

        var updated = await SetStatus(db.Entities.Where(c => c.Home!.Location!.Region == "EU" && c.Rank > 1), "done", async);

        Assert.Equal(1, updated);
        Assert.Equal(["open", "done", "closed"], ReadBack(collection, ConfigureOwned).Select(c => c.Status));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteUpdate_with_Skip_and_Take_over_owned_navigations(bool async)
    {
        var collection = SeedCustomers(async, nameof(ExecuteUpdate_with_Skip_and_Take_over_owned_navigations), ConfigureOwned);
        using var db = SingleEntityDbContext.Create(collection, ConfigureOwned);

        var updated = await SetStatus(db.Entities.OrderBy(c => c.Rank).Skip(1).Take(1), "done", async);

        Assert.Equal(1, updated);
        Assert.Equal(["open", "done", "closed"], ReadBack(collection, ConfigureOwned).Select(c => c.Status));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteUpdate_applies_the_query_filter_over_owned_navigations(bool async)
    {
        var collection = SeedCustomers(async, nameof(ExecuteUpdate_applies_the_query_filter_over_owned_navigations), ConfigureOwnedWithFilter);
        using var db = SingleEntityDbContext.Create(collection, ConfigureOwnedWithFilter);

        var updated = await SetStatus(db.Entities, "done", async);

        Assert.Equal(2, updated);
        Assert.Equal(["done", "done", "closed"], ReadBack(collection, ConfigureOwnedWithFilter).Select(c => c.Status));
    }

    // A select that is NOT an include wrapper is still rejected: it is not a no-op for the affected set's shape.
    [Fact]
    public void A_projection_is_still_rejected_as_a_bulk_source()
    {
        var collection = SeedCustomers(false, nameof(A_projection_is_still_rejected_as_a_bulk_source), ConfigureOwned);
        using var db = SingleEntityDbContext.Create(collection, ConfigureOwned);

        Assert.Throws<InvalidOperationException>(
            () => db.Entities.Select(c => new Customer { _id = c._id, Name = c.Name, Home = c.Home }).ExecuteDelete());

        Assert.Equal(3, ReadBack(collection, ConfigureOwned).Count);
    }

    private abstract class Animal
    {
        public ObjectId _id { get; set; }
        public string Kind { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
    }

    private class Collar
    {
        public string Color { get; set; } = null!;
    }

    private class Dog : Animal
    {
        public Collar? Collar { get; set; }
    }

    private class Cat : Animal;

    private static void ConfigureHierarchy(ModelBuilder mb)
    {
        mb.Entity<Animal>().HasDiscriminator(a => a.Kind).HasValue<Cat>("cat").HasValue<Dog>("dog");
        mb.Entity<Cat>();
        mb.Entity<Dog>().OwnsOne(d => d.Collar);
    }

    private IMongoCollection<Animal> SeedAnimals(bool async, string name)
    {
        var collection = database.CreateCollection<Animal>(name, async);
        using var seed = SingleEntityDbContext.Create(collection, ConfigureHierarchy);
        seed.AddRange(
            new Dog { _id = ObjectId.GenerateNewId(), Name = "rex", Rank = 1, Collar = new Collar { Color = "red" } },
            new Cat { _id = ObjectId.GenerateNewId(), Name = "tom", Rank = 2 },
            new Dog { _id = ObjectId.GenerateNewId(), Name = "fido", Rank = 3, Collar = new Collar { Color = "blue" } });
        seed.SaveChanges();
        return collection;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_OfType_a_derived_type_that_owns_a_navigation(bool async)
    {
        var collection = SeedAnimals(async, nameof(ExecuteDelete_OfType_a_derived_type_that_owns_a_navigation));
        using var db = SingleEntityDbContext.Create(collection, ConfigureHierarchy);

        var deleted = await Delete(db.Entities.OfType<Dog>().Where(d => d.Collar!.Color == "red"), async);

        Assert.Equal(1, deleted);
        using var check = SingleEntityDbContext.Create(collection, ConfigureHierarchy);
        var rows = check.Entities.AsNoTracking().OrderBy(a => a.Rank).ToList();
        Assert.Equal(["tom", "fido"], rows.Select(a => a.Name));
        Assert.Equal("blue", Assert.IsType<Dog>(rows[1]).Collar!.Color);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteUpdate_OfType_a_derived_type_that_owns_a_navigation(bool async)
    {
        var collection = SeedAnimals(async, nameof(ExecuteUpdate_OfType_a_derived_type_that_owns_a_navigation));
        using var db = SingleEntityDbContext.Create(collection, ConfigureHierarchy);

        var source = db.Entities.OfType<Dog>();
        var updated = async
            ? await source.ExecuteUpdateAsync(s => s.SetProperty(d => d.Name, "renamed"))
            : source.ExecuteUpdate(s => s.SetProperty(d => d.Name, "renamed"));

        Assert.Equal(2, updated);
        using var check = SingleEntityDbContext.Create(collection, ConfigureHierarchy);
        var rows = check.Entities.AsNoTracking().OrderBy(a => a.Rank).ToList();
        Assert.Equal(["renamed", "tom", "renamed"], rows.Select(a => a.Name));
        Assert.Equal(["red", "blue"], rows.OfType<Dog>().Select(d => d.Collar!.Color));
    }

    // A navigation to ANOTHER collection configured with AutoInclude leaves the same Select(Include) wrapper. The foreign key is optional,
    // so the include is a left join: a store whose region is missing (an orphan) is still in the affected set and must be affected.
    private class Region
    {
        public ObjectId _id { get; set; }
        public string Name { get; set; } = null!;
    }

    private class Store
    {
        public ObjectId _id { get; set; }
        public string Name { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int Rank { get; set; }
        public ObjectId? RegionId { get; set; }
        public Region? Region { get; set; }
    }

    private SingleEntityDbContext<Store> CreateStoreContext(string name, bool async)
        => SingleEntityDbContext.Create<Store>(
            database,
            mb =>
            {
                mb.Entity<Region>().ToUniqueCollection(name + "Regions", async);
                mb.Entity<Store>(b =>
                {
                    b.HasOne(s => s.Region).WithMany().HasForeignKey(s => s.RegionId);
                    b.Navigation(s => s.Region).AutoInclude();
                });
            },
            null,
            name,
            async);

    private SingleEntityDbContext<Store> SeedStores(string name, bool async)
    {
        using (var seed = CreateStoreContext(name, async))
        {
            var eu = new Region { _id = ObjectId.GenerateNewId(), Name = "EU" };
            seed.AddRange(
                new Store { _id = ObjectId.GenerateNewId(), Name = "s1", Status = "open", Rank = 1, Region = eu },
                new Store { _id = ObjectId.GenerateNewId(), Name = "s2", Status = "open", Rank = 2, Region = eu },
                // An orphan: the foreign key points at no region document.
                new Store { _id = ObjectId.GenerateNewId(), Name = "s3", Status = "open", Rank = 3, RegionId = ObjectId.GenerateNewId() },
                new Store { _id = ObjectId.GenerateNewId(), Name = "s4", Status = "open", Rank = 4 });
            seed.SaveChanges();
        }

        return CreateStoreContext(name, async);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_over_a_cross_collection_auto_include(bool async)
    {
        using var db = SeedStores(nameof(ExecuteDelete_over_a_cross_collection_auto_include), async);

        var deleted = await Delete(db.Entities.Where(s => s.Rank >= 2), async);

        // s2 (has a region), s3 (orphan) and s4 (no region) are all affected; none is dropped by the include.
        Assert.Equal(3, deleted);
        using var check = CreateStoreContext(nameof(ExecuteDelete_over_a_cross_collection_auto_include), async);
        Assert.Equal(["s1"], check.Entities.AsNoTracking().OrderBy(s => s.Rank).Select(s => s.Name).ToList());
        // The included collection is read, never written: its documents are untouched.
        Assert.Equal(1, check.Set<Region>().Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_without_Where_over_a_cross_collection_auto_include(bool async)
    {
        using var db = SeedStores(nameof(ExecuteDelete_without_Where_over_a_cross_collection_auto_include), async);

        var deleted = await Delete(db.Entities, async);

        Assert.Equal(4, deleted);
        using var check = CreateStoreContext(nameof(ExecuteDelete_without_Where_over_a_cross_collection_auto_include), async);
        Assert.Empty(check.Entities.AsNoTracking().ToList());
        Assert.Equal(1, check.Set<Region>().Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteUpdate_over_a_cross_collection_auto_include(bool async)
    {
        using var db = SeedStores(nameof(ExecuteUpdate_over_a_cross_collection_auto_include), async);

        var source = db.Entities.Where(s => s.Rank >= 2);
        var updated = async
            ? await source.ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "done"))
            : source.ExecuteUpdate(s => s.SetProperty(x => x.Status, "done"));

        Assert.Equal(3, updated);
        using var check = CreateStoreContext(nameof(ExecuteUpdate_over_a_cross_collection_auto_include), async);
        Assert.Equal(
            ["open", "done", "done", "done"],
            check.Entities.AsNoTracking().OrderBy(s => s.Rank).Select(s => s.Status).ToList());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_with_Skip_and_Take_over_a_cross_collection_auto_include(bool async)
    {
        using var db = SeedStores(nameof(ExecuteDelete_with_Skip_and_Take_over_a_cross_collection_auto_include), async);

        // Two-phase, over the orphan: phase 1 must still find it.
        var deleted = await Delete(db.Entities.OrderBy(s => s.Rank).Skip(2).Take(1), async);

        Assert.Equal(1, deleted);
        using var check = CreateStoreContext(nameof(ExecuteDelete_with_Skip_and_Take_over_a_cross_collection_auto_include), async);
        Assert.Equal(["s1", "s2", "s4"], check.Entities.AsNoTracking().OrderBy(s => s.Rank).Select(s => s.Name).ToList());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_predicate_on_a_cross_collection_navigation_is_still_rejected(bool async)
    {
        using var db = SeedStores(nameof(A_predicate_on_a_cross_collection_navigation_is_still_rejected), async);

        // Cross-document navigation predicates are not supported in a bulk operation; the include wrapper must not hide that.
        await Assert.ThrowsAsync<InvalidOperationException>(() => Delete(db.Entities.Where(s => s.Region!.Name == "EU"), async));

        using var check = CreateStoreContext(nameof(A_predicate_on_a_cross_collection_navigation_is_still_rejected), async);
        Assert.Equal(4, check.Entities.AsNoTracking().Count());
    }

    // A REQUIRED reference navigation joins with an inner join, which can drop a document whose reference dangles. Stripping it would
    // change the affected set, so it stays rejected.
    private class RequiredStore
    {
        public ObjectId _id { get; set; }
        public string Name { get; set; } = null!;
        public ObjectId RegionId { get; set; }
        public Region Region { get; set; } = null!;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_required_cross_collection_auto_include_is_still_rejected(bool async)
    {
        var name = nameof(A_required_cross_collection_auto_include_is_still_rejected);
        SingleEntityDbContext<RequiredStore> Create() => SingleEntityDbContext.Create<RequiredStore>(
            database,
            mb =>
            {
                mb.Entity<Region>().ToUniqueCollection(name + "Regions", async);
                mb.Entity<RequiredStore>(b =>
                {
                    b.HasOne(s => s.Region).WithMany().HasForeignKey(s => s.RegionId).IsRequired();
                    b.Navigation(s => s.Region).AutoInclude();
                });
            },
            null,
            name,
            async);

        using (var seed = Create())
        {
            var eu = new Region { _id = ObjectId.GenerateNewId(), Name = "EU" };
            seed.AddRange(
                new RequiredStore { _id = ObjectId.GenerateNewId(), Name = "r1", Region = eu },
                // Dangling: the region does not exist.
                new RequiredStore { _id = ObjectId.GenerateNewId(), Name = "r2", RegionId = ObjectId.GenerateNewId() });
            seed.SaveChanges();
        }

        using var db = Create();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Delete(db.Entities, async));

        using var check = Create();
        Assert.Equal(2, check.Entities.IgnoreAutoIncludes().AsNoTracking().Count());
    }

    private class Owner
    {
        public ObjectId _id { get; set; }
        public string Name { get; set; } = null!;
    }

    private class TwoRefStore
    {
        public ObjectId _id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public ObjectId? RegionId { get; set; }
        public Region? Region { get; set; }
        public ObjectId? OwnerId { get; set; }
        public Owner? Owner { get; set; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteDelete_over_two_cross_collection_auto_includes(bool async)
    {
        var name = nameof(ExecuteDelete_over_two_cross_collection_auto_includes);
        SingleEntityDbContext<TwoRefStore> Create() => SingleEntityDbContext.Create<TwoRefStore>(
            database,
            mb =>
            {
                mb.Entity<Region>().ToUniqueCollection(name + "Regions", async);
                mb.Entity<Owner>().ToUniqueCollection(name + "Owners", async);
                mb.Entity<TwoRefStore>(b =>
                {
                    b.HasOne(s => s.Region).WithMany().HasForeignKey(s => s.RegionId);
                    b.HasOne(s => s.Owner).WithMany().HasForeignKey(s => s.OwnerId);
                    b.Navigation(s => s.Region).AutoInclude();
                    b.Navigation(s => s.Owner).AutoInclude();
                });
            },
            null,
            name,
            async);

        using (var seed = Create())
        {
            seed.AddRange(
                new TwoRefStore
                {
                    _id = ObjectId.GenerateNewId(), Name = "t1", Rank = 1,
                    Region = new Region { _id = ObjectId.GenerateNewId(), Name = "EU" },
                    Owner = new Owner { _id = ObjectId.GenerateNewId(), Name = "o" }
                },
                new TwoRefStore { _id = ObjectId.GenerateNewId(), Name = "t2", Rank = 2 },
                new TwoRefStore { _id = ObjectId.GenerateNewId(), Name = "t3", Rank = 3, OwnerId = ObjectId.GenerateNewId() });
            seed.SaveChanges();
        }

        using var db = Create();
        var deleted = await Delete(db.Entities.Where(s => s.Rank >= 2), async);

        Assert.Equal(2, deleted);
        using var check = Create();
        Assert.Equal(["t1"], check.Entities.AsNoTracking().Select(s => s.Name).ToList());
        Assert.Equal(1, check.Set<Region>().Count());
        Assert.Equal(1, check.Set<Owner>().Count());
    }
}

#endif
