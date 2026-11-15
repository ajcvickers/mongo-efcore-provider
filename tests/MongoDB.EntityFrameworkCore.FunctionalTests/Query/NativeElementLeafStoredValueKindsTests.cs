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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// An element leaf whose selector only reads scalar members returns the stored row for every kind of mapped scalar (#32): a string key stored as an
/// <c>ObjectId</c>, a renamed element, a converted enum, a nullable, a primitive collection. The element type also owns a reference and a collection,
/// which the elements are materialized with outside the change tracker.
/// </summary>
public class NativeElementLeafStoredValueKindsTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private enum Grade
    {
        A,
        B
    }

    private class Owner
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public List<Item> Items { get; set; } = null!;
    }

    private class Item
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("renamed_note")]
        public string Note { get; set; } = "";

        public Grade Grade { get; set; }
        public int? Count { get; set; }
        public int Rank { get; set; }
        public List<string> Labels { get; set; } = [];
        public ObjectId OwnerId { get; set; }
        public Owner Owner { get; set; } = null!;
        public Detail Detail { get; set; } = null!;
        public List<Piece> Pieces { get; set; } = null!;
    }

    private class Detail
    {
        public string Label { get; set; } = "";
    }

    private class Piece
    {
        public string Name { get; set; } = "";
    }

    private ItemDbContext CreateContext(string name, out ObjectId owner, MongoQueryMode mode = MongoQueryMode.NativeOnly)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var owners = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "O" + suffix;
        var items = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "I" + suffix;

        owner = ObjectId.GenerateNewId();
        using (var seed = new ItemDbContext(database, owners, items))
        {
            seed.Add(new Owner { Id = owner, Name = "Rex" });
            seed.Add(new Item
            {
                Id = ObjectId.GenerateNewId().ToString(), Note = "Microchip", Grade = Grade.A, Count = null, Rank = 1, Labels = ["a"], OwnerId = owner,
                Detail = new Detail { Label = "chip" }, Pieces = [new Piece { Name = "p1" }]
            });
            seed.Add(new Item
            {
                Id = ObjectId.GenerateNewId().ToString(), Note = "Tattoo", Grade = Grade.B, Count = 7, Rank = 2, Labels = ["b", "c"], OwnerId = owner,
                Detail = new Detail { Label = "ink" }, Pieces = [new Piece { Name = "p2" }, new Piece { Name = "p3" }]
            });
            seed.SaveChanges();
        }

        return new ItemDbContext(database, owners, items, mode);
    }

    /// <summary>Both rows are tracked and changed; the selector reads the stored one, and tracks nothing further.</summary>
    [Theory]
    [InlineData("Renamed")]
    [InlineData("Enum")]
    [InlineData("NullableValue")]
    [InlineData("NullableNull")]
    [InlineData("Key")]
    [InlineData("Labels")]
    [InlineData("ListOfRenamed")]
    [InlineData("ListOfEnum")]
    [InlineData("ListOfLabels")]
    public void A_scalar_selector_returns_the_stored_value_of_every_kind_of_mapped_scalar(string form)
    {
        using var db = CreateContext(nameof(A_scalar_selector_returns_the_stored_value_of_every_kind_of_mapped_scalar) + form, out var owner);

        var chip = db.Items.Single(i => i.OwnerId == owner && i.Rank == 1);
        var tattoo = db.Items.Single(i => i.OwnerId == owner && i.Rank == 2);
        var storedKey = tattoo.Id;
        chip.Count = 99;
        chip.Note = "Unsaved1";
        tattoo.Note = "Unsaved";
        tattoo.Grade = Grade.A;
        tattoo.Count = 99;
        tattoo.Labels.Add("zz");
        var trackedBefore = db.ChangeTracker.Entries().Count();

        var query = db.Owners.Where(o => o.Id == owner);
        switch (form)
        {
            case "Renamed":
                Assert.Equal("Tattoo", query.Select(o => o.Items.Where(i => i.Rank == 2).OrderBy(i => i.Rank).First().Note).Single());
                break;
            case "Enum":
                Assert.Equal(Grade.B, query.Select(o => o.Items.Where(i => i.Rank == 2).OrderBy(i => i.Rank).First().Grade).Single());
                break;
            case "NullableValue":
                Assert.Equal(7, query.Select(o => o.Items.Where(i => i.Rank == 2).OrderBy(i => i.Rank).First().Count).Single());
                break;
            case "NullableNull":
                Assert.Null(query.Select(o => o.Items.Where(i => i.Rank == 1).OrderBy(i => i.Rank).First().Count).Single());
                break;
            case "Key":
                Assert.Equal(storedKey, query.Select(o => o.Items.Where(i => i.Rank == 2).OrderBy(i => i.Rank).First().Id).Single());
                break;
            case "Labels":
                var labels = query.Select(o => o.Items.Where(i => i.Rank == 2).OrderBy(i => i.Rank).First().Labels).Single();
                Assert.Equal(["b", "c"], labels);
                Assert.NotSame(tattoo.Labels, labels);
                break;
            case "ListOfRenamed":
                Assert.Equal(["Microchip", "Tattoo"], query.Select(o => o.Items.OrderBy(i => i.Rank).Select(i => i.Note).ToList()).Single());
                break;
            case "ListOfEnum":
                Assert.Equal([Grade.A, Grade.B], query.Select(o => o.Items.OrderBy(i => i.Rank).Select(i => i.Grade).ToList()).Single());
                break;
            default:
                var lists = query.Select(o => o.Items.OrderBy(i => i.Rank).Select(i => i.Labels).ToList()).Single();
                Assert.Equal(["a"], lists[0]);
                Assert.Equal(["b", "c"], lists[1]);
                Assert.NotSame(tattoo.Labels, lists[1]);
                break;
        }

        Assert.Equal("Unsaved", tattoo.Note);
        Assert.Equal(99, tattoo.Count);
        Assert.Equal(["b", "c", "zz"], tattoo.Labels);
        Assert.Equal(trackedBefore, db.ChangeTracker.Entries().Count());
    }

    /// <summary>
    /// The element owns a reference and a collection: a scalar selector still reads the stored row and tracks neither the element nor what it owns,
    /// even though the tracked row's owned values have unsaved changes.
    /// </summary>
    [Theory]
    [InlineData("First")]
    [InlineData("List")]
    public void A_scalar_selector_over_an_element_with_owned_navigations_returns_the_stored_value_and_tracks_nothing(string form)
    {
        using var db = CreateContext(
            nameof(A_scalar_selector_over_an_element_with_owned_navigations_returns_the_stored_value_and_tracks_nothing) + form, out var owner);

        var tattoo = db.Items.Single(i => i.OwnerId == owner && i.Rank == 2);
        tattoo.Note = "Unsaved";
        tattoo.Detail.Label = "Unsaved label";
        tattoo.Pieces.Add(new Piece { Name = "Unsaved piece" });
        var trackedBefore = db.ChangeTracker.Entries().Count();

        var query = db.Owners.Where(o => o.Id == owner);
        if (form == "First")
        {
            Assert.Equal("Tattoo", query.Select(o => o.Items.Where(i => i.Rank == 2).OrderBy(i => i.Rank).First().Note).Single());
        }
        else
        {
            Assert.Equal(["Tattoo"], query.Select(o => o.Items.Where(i => i.Rank == 2).Select(i => i.Note).ToList()).Single());
        }

        Assert.Equal("Unsaved", tattoo.Note);
        Assert.Equal("Unsaved label", tattoo.Detail.Label);
        Assert.Equal(3, tattoo.Pieces.Count);
        Assert.Equal(trackedBefore, db.ChangeTracker.Entries().Count());
        AssertNoOwnedEntryWithoutItsOwner(db);
    }

    /// <summary>
    /// A scalar selector the streaming fusion declines (<c>Rank.ToString()</c>), over an element that owns a reference and a collection, in the same
    /// projection as the tracked root entity: the root keeps its identity and is tracked as usual, the elements are built outside the change tracker
    /// with their owned navigations read from the document, and nothing is tracked for the leaf.
    /// </summary>
    [Theory]
    [InlineData("First")]
    [InlineData("List")]
    public void A_declined_scalar_leaf_over_an_element_with_owned_navigations_beside_a_tracked_root_reads_the_stored_row(string form)
    {
        using var db = CreateContext(
            nameof(A_declined_scalar_leaf_over_an_element_with_owned_navigations_beside_a_tracked_root_reads_the_stored_row) + form, out var owner);

        var root = db.Owners.Single(o => o.Id == owner);
        var tattoo = db.Items.Single(i => i.OwnerId == owner && i.Rank == 2);
        tattoo.Rank = 99;
        tattoo.Detail.Label = "Unsaved label";
        var trackedBefore = db.ChangeTracker.Entries().Count();

        var query = db.Owners.Where(o => o.Id == owner);
        if (form == "First")
        {
            var row = query
                .Select(o => new { o, R = o.Items.Where(i => i.Note == "Tattoo").OrderBy(i => i.Note).Select(i => i.Rank.ToString()).First() })
                .Single();
            Assert.Equal("2", row.R);
            Assert.Same(root, row.o);
        }
        else
        {
            var row = query
                .Select(o => new { o, R = o.Items.OrderBy(i => i.Rank).Select(i => i.Rank.ToString()).ToList() })
                .Single();
            Assert.Equal(["1", "2"], row.R);
            Assert.Same(root, row.o);
        }

        Assert.Equal(99, tattoo.Rank);
        Assert.Equal("Unsaved label", tattoo.Detail.Label);
        Assert.Equal(trackedBefore, db.ChangeTracker.Entries().Count());
        AssertNoOwnedEntryWithoutItsOwner(db);
    }

    /// <summary>
    /// An owned reference member of the element read in the selector (#119): the selector uses the element other than through a scalar property
    /// (<see cref="MongoDB.EntityFrameworkCore.Query.NativeTranslation.ElementSelectorKind.Other"/>), so under <c>TrackAll</c> it runs over the
    /// tracked instance, unsaved change included, and without tracking over the stored element.
    /// </summary>
    [Theory]
    [InlineData("List", true)]
    [InlineData("List", false)]
    [InlineData("First", true)]
    [InlineData("First", false)]
    public void Owned_reference_member_in_selector(string form, bool tracked)
    {
        using var db = CreateContext(nameof(Owned_reference_member_in_selector) + form + tracked, out var owner);

        var tattoo = db.Items.Single(i => i.OwnerId == owner && i.Rank == 2);
        tattoo.Detail.Label = "Unsaved label";
        tattoo.Pieces.Add(new Piece { Name = "Unsaved piece" });
        var trackedBefore = db.ChangeTracker.Entries().Count();

        var query = tracked ? db.Owners.Where(o => o.Id == owner) : db.Owners.AsNoTracking().Where(o => o.Id == owner);
        var expectedTattoo = tracked ? "Unsaved label" : "ink";
        if (form == "List")
        {
            Assert.Equal(["chip", expectedTattoo], query.Select(o => o.Items.OrderBy(i => i.Rank).Select(i => i.Detail.Label).ToList()).Single());
        }
        else
        {
            Assert.Equal(expectedTattoo, query.Select(o => o.Items.Where(i => i.Rank == 2).OrderBy(i => i.Rank).First().Detail.Label).Single());
        }

        Assert.Equal("Unsaved label", tattoo.Detail.Label);
        AssertTrackingAfterOwnedMemberSelector(db, tracked, trackedBefore);
    }

    /// <summary>An owned collection of the element read in the selector (#119), as <see cref="Owned_reference_member_in_selector"/>.</summary>
    [Theory]
    [InlineData("List", true)]
    [InlineData("List", false)]
    [InlineData("First", true)]
    [InlineData("First", false)]
    public void Owned_collection_count_in_selector(string form, bool tracked)
    {
        using var db = CreateContext(nameof(Owned_collection_count_in_selector) + form + tracked, out var owner);

        var tattoo = db.Items.Single(i => i.OwnerId == owner && i.Rank == 2);
        tattoo.Detail.Label = "Unsaved label";
        tattoo.Pieces.Add(new Piece { Name = "Unsaved piece" });
        var trackedBefore = db.ChangeTracker.Entries().Count();

        var query = tracked ? db.Owners.Where(o => o.Id == owner) : db.Owners.AsNoTracking().Where(o => o.Id == owner);
        var expectedTattoo = tracked ? 3 : 2;
        if (form == "List")
        {
            Assert.Equal([1, expectedTattoo], query.Select(o => o.Items.OrderBy(i => i.Rank).Select(i => i.Pieces.Count).ToList()).Single());
        }
        else
        {
            Assert.Equal(expectedTattoo, query.Select(o => o.Items.Where(i => i.Rank == 2).OrderBy(i => i.Rank).First().Pieces.Count).Single());
        }

        Assert.Equal(3, tattoo.Pieces.Count);
        AssertTrackingAfterOwnedMemberSelector(db, tracked, trackedBefore);
    }

    /// <summary>
    /// The elements themselves, of a type that owns a reference and a collection: nav-expansion includes the owned navigations through a selector of
    /// their own, which is no selector. Under <c>TrackAll</c> each element is the tracked instance with its owned graph; without tracking, the stored
    /// element with its stored owned values.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Elements_with_owned_navigations_beside_a_scalar(bool tracked)
    {
        using var db = CreateContext(nameof(Elements_with_owned_navigations_beside_a_scalar) + tracked, out var owner);

        var tattoo = db.Items.Single(i => i.OwnerId == owner && i.Rank == 2);
        tattoo.Detail.Label = "Unsaved label";
        tattoo.Pieces.Add(new Piece { Name = "Unsaved piece" });

        var query = tracked ? db.Owners.Where(o => o.Id == owner) : db.Owners.AsNoTracking().Where(o => o.Id == owner);
        var row = query.Select(o => new { o.Name, Items = o.Items.OrderBy(i => i.Rank).ToList() }).Single();

        Assert.Equal("Rex", row.Name);
        Assert.Equal(["chip", tracked ? "Unsaved label" : "ink"], row.Items.Select(i => i.Detail.Label));
        Assert.Equal([1, tracked ? 3 : 2], row.Items.Select(i => i.Pieces.Count));
        if (tracked)
        {
            Assert.Same(tattoo, row.Items[1]);
        }
        else
        {
            Assert.NotSame(tattoo, row.Items[1]);
        }

        AssertNoOwnedEntryWithoutItsOwner(db);
    }

    /// <summary>
    /// The owned-member selectors and the elements with their owned graph are answered by the element leaf's own lookup, which has no driver-LINQ
    /// fallback, so they give the same rows in every query mode.
    /// </summary>
    [Theory]
    [InlineData(MongoQueryMode.Native, true)]
    [InlineData(MongoQueryMode.Native, false)]
    [InlineData(MongoQueryMode.DriverLinq, true)]
    [InlineData(MongoQueryMode.DriverLinq, false)]
    public void Owned_members_in_selector_in_every_query_mode(MongoQueryMode mode, bool tracked)
    {
        using var db = CreateContext(nameof(Owned_members_in_selector_in_every_query_mode) + mode + tracked, out var owner, mode);

        var tattoo = db.Items.Single(i => i.OwnerId == owner && i.Rank == 2);
        tattoo.Detail.Label = "Unsaved label";
        tattoo.Pieces.Add(new Piece { Name = "Unsaved piece" });

        var query = tracked ? db.Owners.Where(o => o.Id == owner) : db.Owners.AsNoTracking().Where(o => o.Id == owner);
        Assert.Equal(
            ["chip", tracked ? "Unsaved label" : "ink"],
            query.Select(o => o.Items.OrderBy(i => i.Rank).Select(i => i.Detail.Label).ToList()).Single());
        Assert.Equal([1, tracked ? 3 : 2], query.Select(o => o.Items.OrderBy(i => i.Rank).Select(i => i.Pieces.Count).ToList()).Single());
        var items = query.Select(o => new { o.Name, Items = o.Items.OrderBy(i => i.Rank).ToList() }).Single().Items;
        Assert.Equal(["chip", tracked ? "Unsaved label" : "ink"], items.Select(i => i.Detail.Label));
        AssertNoOwnedEntryWithoutItsOwner(db);
    }

    // Under TrackAll the selector ran over the tracked elements, so the untracked Microchip row (and its owned graph) is now tracked with its owner;
    // without tracking nothing new is tracked.
    private static void AssertTrackingAfterOwnedMemberSelector(DbContext db, bool tracked, int trackedBefore)
    {
        if (!tracked)
        {
            Assert.Equal(trackedBefore, db.ChangeTracker.Entries().Count());
        }

        AssertNoOwnedEntryWithoutItsOwner(db);
    }

    /// <summary>
    /// The invariant behind all of this: an owned entity is never tracked without its owner (its key cannot be known without an owner instance),
    /// so no query, however it reads the elements, may leave an owned entry in the change tracker whose owner is not tracked and does not hold it.
    /// </summary>
    private static void AssertNoOwnedEntryWithoutItsOwner(DbContext db)
    {
        var entries = db.ChangeTracker.Entries().ToList();
        var ownedEntries = entries.Where(e => e.Metadata.IsOwned()).ToList();
        Assert.NotEmpty(ownedEntries); // the tracked row owns entities, so the check is not vacuous
        foreach (var owned in ownedEntries)
        {
            var ownership = owned.Metadata.FindOwnership()!;
            var navigation = ownership.PrincipalToDependent!;
            var held = entries.Any(
                e => e.Metadata == ownership.PrincipalEntityType
                     && e.Navigation(navigation.Name).CurrentValue is { } value
                     && (ReferenceEquals(value, owned.Entity)
                         || value is System.Collections.IEnumerable items && items.Cast<object>().Any(i => ReferenceEquals(i, owned.Entity))));
            Assert.True(held, $"{owned.Metadata.DisplayName()} is tracked without a tracked owner that holds it.");
        }
    }

    private class ItemDbContext : DbContext
    {
        private readonly string _owners;
        private readonly string _items;

        public ItemDbContext(TemporaryDatabaseFixture database, string owners, string items, MongoQueryMode mode = MongoQueryMode.NativeOnly)
            : base(
                new DbContextOptionsBuilder<ItemDbContext>()
                    .UseMongoDB(
                        database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName,
                        b => b.UseQueryMode(mode))
                    .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                    .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                    .Options)
        {
            _owners = owners;
            _items = items;
        }

        public DbSet<Owner> Owners { get; set; } = null!;
        public DbSet<Item> Items { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Owner>().ToCollection(_owners);
            modelBuilder.Entity<Owner>().HasMany(o => o.Items).WithOne(i => i.Owner).HasForeignKey(i => i.OwnerId);
            modelBuilder.Entity<Item>(b =>
            {
                b.ToCollection(_items);
                b.Property(i => i.Grade).HasConversion<string>();
                b.OwnsOne(i => i.Detail);
                b.OwnsMany(i => i.Pieces);
            });
        }

        private sealed class IgnoreCacheKeyFactory : IModelCacheKeyFactory
        {
            private static int _count;
            public object Create(DbContext context, bool designTime) => Interlocked.Increment(ref _count);
        }
    }
}
