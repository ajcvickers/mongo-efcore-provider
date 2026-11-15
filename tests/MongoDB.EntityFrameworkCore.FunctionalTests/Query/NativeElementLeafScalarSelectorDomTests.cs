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
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// An element leaf whose selector only reads scalar members of the element returns the stored row on the DOM shaper too (#32). The element type
/// is the root of a hierarchy, which the streaming reader does not read, so these queries are shaped by EF's injected materializers, where the
/// elements would otherwise be resolved against the tracked instances (the fusion of #116 is streaming only). A projection the planner cannot
/// stream, one past 32 slots and the fallback routes are shaped the same way. <see cref="NativeCorrelatedReducerProjectionTests"/> covers the same
/// selectors on the streaming shaper.
/// </summary>
public class NativeElementLeafScalarSelectorDomTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Owner
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public List<Tag> Tags { get; set; } = null!;
    }

    private class Tag
    {
        public ObjectId Id { get; set; }
        public string Note { get; set; } = "";
        public int Rank { get; set; }
        public ObjectId OwnerId { get; set; }
        public Owner Owner { get; set; } = null!;
    }

    private class LabelTag : Tag
    {
        public string Extra { get; set; } = "";
    }

    private TagDbContext CreateContext(string name, out ObjectId owner)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var owners = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "O" + suffix;
        var tags = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "T" + suffix;

        owner = ObjectId.GenerateNewId();
        using (var seed = new TagDbContext(database, owners, tags))
        {
            seed.Add(new Owner { Id = owner, Name = "Rex" });
            seed.Add(new Tag { Id = ObjectId.GenerateNewId(), Note = "Microchip", Rank = 1, OwnerId = owner });
            seed.Add(new LabelTag { Id = ObjectId.GenerateNewId(), Note = "Tattoo", Rank = 2, OwnerId = owner, Extra = "Left ear" });
            seed.SaveChanges();
        }

        return new TagDbContext(database, owners, tags);
    }

    /// <summary>
    /// The row is tracked and has an unsaved change; the selector reads the stored row, and tracks nothing further. The tracked row is a derived
    /// type, so the elements are read polymorphically.
    /// </summary>
    [Theory]
    [InlineData("First")]
    [InlineData("Single")]
    [InlineData("List")]
    [InlineData("ListOfComputed")]
    [InlineData("Anonymous")]
    [InlineData("ToString")]
    public void A_selector_over_scalar_members_returns_the_stored_row_on_the_DOM_shaper(string form)
    {
        using var db = CreateContext(nameof(A_selector_over_scalar_members_returns_the_stored_row_on_the_DOM_shaper) + form, out var owner);

        var tracked = db.Tags.Single(t => t.OwnerId == owner && t.Rank == 2);
        tracked.Note = "Unsaved";
        tracked.Rank = 99;
        var trackedBefore = db.ChangeTracker.Entries<Tag>().Count();

        var query = db.Owners.Where(o => o.Id == owner);
        switch (form)
        {
            case "First":
                Assert.Equal(
                    "Tattoo",
                    query.Select(o => o.Tags.Where(t => t.Rank == 2).OrderBy(t => t.Rank).First().Note).Single());
                break;
            case "Single":
                Assert.Equal(
                    "Tattoo",
                    query.Select(o => o.Tags.Where(t => t.Rank == 2).Single().Note).Single());
                break;
            case "List":
                Assert.Equal(
                    new List<string> { "Tattoo" },
                    query.Select(o => o.Tags.Where(t => t.Rank == 2).Select(t => t.Note).ToList()).Single());
                break;
            case "ListOfComputed":
                Assert.Equal(
                    new List<int> { 3 },
                    query.Select(o => o.Tags.Where(t => t.Rank == 2).Select(t => t.Rank + 1).ToList()).Single());
                break;
            case "ToString":
                Assert.Equal(
                    new List<string> { "2" },
                    query.Select(o => o.Tags.Where(t => t.Rank == 2).Select(t => t.Rank.ToString()).ToList()).Single());
                break;
            default:
                Assert.Equal(
                    "Tattoo",
                    query.Select(o => new { N = o.Tags.Where(t => t.Rank == 2).OrderBy(t => t.Rank).First().Note }).Single().N);
                break;
        }

        Assert.Equal("Unsaved", tracked.Note);
        Assert.Equal(99, tracked.Rank);
        Assert.Equal(trackedBefore, db.ChangeTracker.Entries<Tag>().Count());
    }

    /// <summary>
    /// A selector that uses the element itself sees the tracked instance on a tracking query, on this shaper as on the streaming one, and a query that
    /// does not track reads the stored row.
    /// </summary>
    [Fact]
    public void A_selector_that_keeps_the_element_sees_the_tracked_instance_when_tracking_and_the_stored_row_when_not()
    {
        using var db = CreateContext(
            nameof(A_selector_that_keeps_the_element_sees_the_tracked_instance_when_tracking_and_the_stored_row_when_not), out var owner);

        var tracked = db.Tags.Single(t => t.OwnerId == owner && t.Rank == 2);
        tracked.Note = "Unsaved";

        var tracking = db.Owners.Where(o => o.Id == owner)
            .Select(o => o.Tags.Where(t => t.Rank == 2).Select(t => new { t }).ToList())
            .Single().Single();
        Assert.Same(tracked, tracking.t);

        var untracked = db.Owners.AsNoTracking().Where(o => o.Id == owner)
            .Select(o => o.Tags.Where(t => t.Rank == 2).Select(t => new { t }).ToList())
            .Single().Single();
        Assert.NotSame(tracked, untracked.t);
        Assert.Equal("Tattoo", untracked.t.Note);
        Assert.Equal("Unsaved", tracked.Note);
    }

    private class TagDbContext : DbContext
    {
        private readonly string _owners;
        private readonly string _tags;

        public TagDbContext(TemporaryDatabaseFixture database, string owners, string tags)
            : base(
                new DbContextOptionsBuilder<TagDbContext>()
                    .UseMongoDB(
                        database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName,
                        b => b.UseQueryMode(MongoQueryMode.NativeOnly))
                    .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                    .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                    .Options)
        {
            _owners = owners;
            _tags = tags;
        }

        public DbSet<Owner> Owners { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Owner>().ToCollection(_owners);
            modelBuilder.Entity<Owner>().HasMany(o => o.Tags).WithOne(t => t.Owner).HasForeignKey(t => t.OwnerId);
            modelBuilder.Entity<Tag>().ToCollection(_tags);
            modelBuilder.Entity<LabelTag>();
        }

        private sealed class IgnoreCacheKeyFactory : IModelCacheKeyFactory
        {
            private static int _count;
            public object Create(DbContext context, bool designTime) => Interlocked.Increment(ref _count);
        }
    }
}
