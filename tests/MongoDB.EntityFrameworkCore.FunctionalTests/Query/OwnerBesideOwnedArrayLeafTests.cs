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
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// A projection that reads an owner beside its owned array (<c>new { r, Ws = r.Kids.Select(k =&gt; k.W).ToList() }</c>,
/// <c>new { r, r.Kids }</c>) reads the array from the same document the owner was read from. The document variables the
/// shaper declares per entity are lifted to the lambda, and a read outside the entity's own block must not see a second,
/// unassigned declaration of the same variable (#121).
/// </summary>
public class OwnerBesideOwnedArrayLeafTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Kid
    {
        public int W { get; set; }
        public string Label { get; set; } = "";
    }

    public class Spot
    {
        public string City { get; set; } = "";
    }

    public class Root
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<Kid> Kids { get; set; } = [];
        public Spot Address { get; set; } = new();
    }

    [Theory]
    [InlineData(MongoQueryMode.NativeOnly, QueryTrackingBehavior.NoTracking)]
    [InlineData(MongoQueryMode.NativeOnly, QueryTrackingBehavior.NoTrackingWithIdentityResolution)]
    [InlineData(MongoQueryMode.NativeOnly, QueryTrackingBehavior.TrackAll)]
    [InlineData(MongoQueryMode.Native, QueryTrackingBehavior.NoTracking)]
    [InlineData(MongoQueryMode.Native, QueryTrackingBehavior.NoTrackingWithIdentityResolution)]
    [InlineData(MongoQueryMode.Native, QueryTrackingBehavior.TrackAll)]
    [InlineData(MongoQueryMode.DriverLinq, QueryTrackingBehavior.NoTracking)]
    [InlineData(MongoQueryMode.DriverLinq, QueryTrackingBehavior.NoTrackingWithIdentityResolution)]
    [InlineData(MongoQueryMode.DriverLinq, QueryTrackingBehavior.TrackAll)]
    public void Owner_beside_owned_select_leaf_reads_both(MongoQueryMode mode, QueryTrackingBehavior tracking)
    {
        using var db = Create(Seed(), mode, tracking);

        var row = db.Entities.Where(r => r.Id == 1)
            .Select(r => new { r, Ws = r.Kids.Select(k => k.W).ToList() }).Single();

        Assert.Equal([1, 2], row.r.Kids.Select(k => k.W));
        Assert.Equal([1, 2], row.Ws);
    }

    // Native since #122 (a whole-root leaf beside an array leaf): NativeOnly proves the native read side serves the row.
    [Theory]
    [InlineData(MongoQueryMode.NativeOnly, QueryTrackingBehavior.NoTracking)]
    [InlineData(MongoQueryMode.NativeOnly, QueryTrackingBehavior.NoTrackingWithIdentityResolution)]
    [InlineData(MongoQueryMode.NativeOnly, QueryTrackingBehavior.TrackAll)]
    [InlineData(MongoQueryMode.Native, QueryTrackingBehavior.NoTracking)]
    [InlineData(MongoQueryMode.Native, QueryTrackingBehavior.NoTrackingWithIdentityResolution)]
    [InlineData(MongoQueryMode.Native, QueryTrackingBehavior.TrackAll)]
    [InlineData(MongoQueryMode.DriverLinq, QueryTrackingBehavior.NoTracking)]
    [InlineData(MongoQueryMode.DriverLinq, QueryTrackingBehavior.NoTrackingWithIdentityResolution)]
    [InlineData(MongoQueryMode.DriverLinq, QueryTrackingBehavior.TrackAll)]
    public void Owner_beside_owned_collection_reads_both(MongoQueryMode mode, QueryTrackingBehavior tracking)
    {
        using var db = Create(Seed(), mode, tracking);

        var row = db.Entities.Where(r => r.Id == 1).Select(r => new { r, r.Kids }).Single();

        Assert.Equal(1, row.r.Id);
        Assert.Equal(["a", "b"], row.Kids.Select(k => k.Label));
        Assert.Equal([1, 2], row.Kids.Select(k => k.W));
        Assert.Equal([1, 2], row.r.Kids.Select(k => k.W));
        if (tracking == QueryTrackingBehavior.TrackAll)
        {
            for (var i = 0; i < row.Kids.Count; i++)
            {
                Assert.Same(row.r.Kids[i], row.Kids[i]);
            }

            AssertOwnedHeldByTrackedOwner(db, expectedKids: 2);
        }
        else
        {
            Assert.Empty(db.ChangeTracker.Entries());
        }
    }

    // An owned REFERENCE beside the whole root (native since #122): the same instance as the root's, held by its tracked owner (R6).
    [Theory]
    [InlineData(QueryTrackingBehavior.NoTracking)]
    [InlineData(QueryTrackingBehavior.NoTrackingWithIdentityResolution)]
    [InlineData(QueryTrackingBehavior.TrackAll)]
    public void Owner_beside_owned_reference_reads_both(QueryTrackingBehavior tracking)
    {
        using var db = Create(Seed(), MongoQueryMode.NativeOnly, tracking);

        var row = db.Entities.Where(r => r.Id == 1).Select(r => new { r, r.Address }).Single();

        Assert.Equal(1, row.r.Id);
        Assert.Equal("Oslo", row.Address.City);
        Assert.Equal("Oslo", row.r.Address.City);
        if (tracking == QueryTrackingBehavior.TrackAll)
        {
            Assert.Same(row.r.Address, row.Address);
            var spot = Assert.Single(db.ChangeTracker.Entries<Spot>());
            Assert.Same(row.Address, spot.Entity);
            Assert.Same(spot.Entity, Assert.Single(db.ChangeTracker.Entries<Root>()).Entity.Address);
        }
        else
        {
            Assert.Empty(db.ChangeTracker.Entries());
        }
    }

    // R4: the owned reference is an entity, not a scalar read, so a tracking query returns the tracked instance with its unsaved change.
    [Fact]
    public void Owner_beside_owned_reference_is_the_tracked_instance()
    {
        using var db = Create(Seed(), MongoQueryMode.NativeOnly);
        var tracked = db.Entities.Single(r => r.Id == 1);
        tracked.Address.City = "Bergen";

        var row = db.Entities.Where(r => r.Id == 1).Select(r => new { r, r.Address }).Single();

        Assert.Same(tracked, row.r);
        Assert.Same(tracked.Address, row.Address);
        Assert.Equal("Bergen", row.Address.City);
        Assert.Single(db.ChangeTracker.Entries<Spot>());
    }

    [Theory]
    [InlineData(MongoQueryMode.NativeOnly)]
    [InlineData(MongoQueryMode.Native)]
    [InlineData(MongoQueryMode.DriverLinq)]
    public void Owner_beside_owned_select_leaf_tracks_owner_and_owned_graph(MongoQueryMode mode)
    {
        using var db = Create(Seed(), mode, QueryTrackingBehavior.TrackAll);

        var row = db.Entities.Where(r => r.Id == 1)
            .Select(r => new { r, Ws = r.Kids.Select(k => k.W).ToList() }).Single();

        Assert.Equal([1, 2], row.Ws);
        var owners = db.ChangeTracker.Entries<Root>().ToList();
        Assert.Single(owners);
        var kids = db.ChangeTracker.Entries<Kid>().ToList();
        Assert.Equal(2, kids.Count);
        foreach (var kid in kids)
        {
            Assert.Equal(EntityState.Unchanged, kid.State);
            Assert.Contains(kid.Entity, owners[0].Entity.Kids);
        }
    }

    // R6: every tracked owned entity is held by a tracked owner.
    private static void AssertOwnedHeldByTrackedOwner(SingleEntityDbContext<Root> db, int expectedKids)
    {
        var owners = db.ChangeTracker.Entries<Root>().Select(o => o.Entity).ToList();
        var kids = db.ChangeTracker.Entries<Kid>().ToList();
        Assert.Equal(expectedKids, kids.Count);
        Assert.All(kids, kid => Assert.Contains(owners, owner => owner.Kids.Contains(kid.Entity)));
        Assert.All(db.ChangeTracker.Entries<Spot>(), spot => Assert.Contains(owners, owner => ReferenceEquals(owner.Address, spot.Entity)));
    }

    private static SingleEntityDbContext<Root> Create(
        IMongoCollection<Root> collection, MongoQueryMode mode, QueryTrackingBehavior tracking = QueryTrackingBehavior.TrackAll)
        => SingleEntityDbContext.Create(
            collection,
            mb =>
            {
                mb.Entity<Root>().OwnsMany(e => e.Kids);
                mb.Entity<Root>().OwnsOne(e => e.Address);
            },
            optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                b.UseQueryTrackingBehavior(tracking);
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });

    private IMongoCollection<Root> Seed()
    {
        var collection = database.MongoDatabase.GetCollection<Root>(
            TemporaryDatabaseFixtureBase.CreateCollectionName("ownerBesideLeaf") + Guid.NewGuid().ToString("N")[..8]);
        using var db = Create(collection, MongoQueryMode.DriverLinq);
        db.Entities.Add(new Root { Id = 1, Name = "x", Kids = [new Kid { W = 1, Label = "a" }, new Kid { W = 2, Label = "b" }], Address = new Spot { City = "Oslo" } });
        db.SaveChanges();
        return collection;
    }
}
