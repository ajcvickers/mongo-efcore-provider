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
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// An element <c>Select</c> over an owned collection (<c>Weights = e.Kids.Select(k =&gt; k.Weight).ToList()</c>) stages
/// the whole owned array (<c>NativeProjectionBinder.TryMatchOwnedArraySelectLeaf</c>). A selector that only reads mapped
/// scalar members of the element (<c>ElementSelectorKind.ScalarMembersOnly</c>) is evaluated over the stored element
/// documents, through each property's serializer: no owned entity is constructed or tracked, whatever the query's tracking
/// behavior, so it reads the stored values and not an unsaved change to an owner the context already tracks (#121).
/// </summary>
public class NativeOwnedArraySelectLeafTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public enum Size
    {
        Small,
        Big
    }

    public class Kid
    {
        public string Label { get; set; } = "";

        [BsonRepresentation(BsonType.String)]
        public decimal Weight { get; set; }

        public int W { get; set; }

        public Size Size { get; set; }
    }

    public class Owner
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<Kid> Kids { get; set; } = [];
    }

    public class Animal
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<Kid> Kids { get; set; } = [];
    }

    public class Dog : Animal
    {
        public string Breed { get; set; } = "";
    }

    private static readonly MongoQueryMode[] AllModes = [MongoQueryMode.NativeOnly, MongoQueryMode.Native, MongoQueryMode.DriverLinq];

    [Fact]
    public void Select_leaf_beside_the_whole_array_reads_through_the_owned_serializer()
        => NativeModeAssert.NativeAndExpected(
            mode =>
            {
                using var db = Create(Seed(), mode);
                var row = db.Entities.AsNoTracking().OrderBy(e => e.Id)
                    .Select(e => new { e.Kids, Weights = e.Kids.Select(k => k.Weight).ToList() }).First();
                return row.Weights.Select(w => $"{w}").Concat(row.Kids.Select(k => k.Label)).ToList();
            },
            ["10.5", "20.25", "a", "b"]);

    [Fact]
    public void Select_leaf_before_the_whole_array_shares_it()
        => NativeModeAssert.NativeAndExpected(
            mode =>
            {
                using var db = Create(Seed(), mode);
                var row = db.Entities.AsNoTracking().OrderBy(e => e.Id)
                    .Select(e => new { Labels = e.Kids.Select(k => k.Label).ToList(), e.Kids }).First();
                return row.Labels.Concat(row.Kids.Select(k => $"{k.Weight}")).ToList();
            },
            ["a", "b", "10.5", "20.25"]);

    [Fact]
    public void Select_leaf_alone_goes_native_and_reads_every_row()
        => NativeModeAssert.NativeAndExpected(
            mode =>
            {
                using var db = Create(Seed(), mode);
                return db.Entities.AsNoTracking().OrderBy(e => e.Id)
                    .Select(e => new { e.Name, Pairs = e.Kids.Select(k => new { k.Label, k.Weight }).ToList() })
                    .AsEnumerable().Select(r => $"{r.Name}:{string.Join(",", r.Pairs.Select(p => $"{p.Label}={p.Weight}"))}").ToList();
            },
            ["x:a=10.5,b=20.25", "y:"]);

    [Fact]
    public void Tracked_scalar_select_reads_stored_values_and_tracks_nothing_new()
    {
        var collection = Seed();
        NativeModeAssert.NativeAndExpected(
            mode =>
            {
                using var db = CreateWithUnsavedChange(collection, mode);
                var before = db.ChangeTracker.Entries().Count();
                var row = db.Entities.Where(e => e.Id == 1)
                    .Select(e => new { e.Name, Ws = e.Kids.Select(k => k.W).ToList() }).Single();
                var after = db.ChangeTracker.Entries().Count();
                return [..row.Ws.Select(w => $"{w}"), $"tracked:{before}->{after}"];
            },
            ["1", "2", "tracked:3->3"]);
    }

    [Fact]
    public void Tracked_scalar_select_is_read_in_place_by_the_streaming_shaper()
    {
        var collection = Seed();
        var commands = new List<string>();
        var settings = database.Client.Settings.Clone();
        settings.ClusterConfigurator = b => b.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(e =>
        {
            if (e.CommandName == "aggregate" && e.Command["aggregate"].AsString == collection.CollectionNamespace.CollectionName)
            {
                commands.Add(e.Command.ToJson());
            }
        });

        using var db = CreateWithUnsavedChange(
            new MongoClient(settings).GetDatabase(collection.Database.DatabaseNamespace.DatabaseName)
                .GetCollection<Owner>(collection.CollectionNamespace.CollectionName),
            MongoQueryMode.NativeOnly);
        commands.Clear();

        var row = db.Entities.Where(e => e.Id == 1)
            .Select(e => new { e.Name, Ws = e.Kids.Select(k => k.W).ToList() }).Single();

        Assert.Equal([1, 2], row.Ws);

        // The streaming shaper fused the Select, so the pipeline was narrowed to the one member it reads.
        var command = Assert.Single(commands);
        Assert.True(command.Contains("\"$$e.W\""), command);
        Assert.DoesNotContain("Label", command);
        Assert.DoesNotContain("Weight", command);
    }

    // The cursor accepts a captured value in the lambda (read from the query context), so the Select is still fused and the pipeline narrowed.
    [Fact]
    public void Captured_variable_in_selector_is_read_in_place_by_the_streaming_shaper()
    {
        var collection = Seed();
        var commands = new List<string>();
        var settings = database.Client.Settings.Clone();
        settings.ClusterConfigurator = b => b.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(e =>
        {
            if (e.CommandName == "aggregate" && e.Command["aggregate"].AsString == collection.CollectionNamespace.CollectionName)
            {
                commands.Add(e.Command.ToJson());
            }
        });

        using var db = CreateWithUnsavedChange(
            new MongoClient(settings).GetDatabase(collection.Database.DatabaseNamespace.DatabaseName)
                .GetCollection<Owner>(collection.CollectionNamespace.CollectionName),
            MongoQueryMode.NativeOnly);

        foreach (var offset in new[] { 10, 20 })
        {
            commands.Clear();
            var row = db.Entities.Where(e => e.Id == 1)
                .Select(e => new { e.Name, Ws = e.Kids.Select(k => k.W + offset).ToList() }).Single();

            Assert.Equal([1 + offset, 2 + offset], row.Ws);
            var command = Assert.Single(commands);
            Assert.True(command.Contains("\"$$e.W\""), command);
            Assert.DoesNotContain("Label", command);
        }
    }

    [Fact]
    public void Untracked_scalar_select_reads_stored_values()
    {
        var collection = Seed();
        NativeModeAssert.NativeAndExpected(
            mode =>
            {
                using var db = CreateWithUnsavedChange(collection, mode);
                var row = db.Entities.AsNoTracking().Where(e => e.Id == 1)
                    .Select(e => new { e.Name, Ws = e.Kids.Select(k => k.W).ToList() }).Single();
                return row.Ws.Select(w => $"{w}").ToList();
            },
            ["1", "2"]);
    }

    [Fact]
    public void Tracked_owner_beside_scalar_select_reads_stored_values()
    {
        var collection = Seed();
        foreach (var mode in AllModes)
        {
            using var db = CreateWithUnsavedChange(collection, mode);
            var tracked = db.Entities.Local.Single(e => e.Id == 1);

            var row = db.Entities.Where(e => e.Id == 1)
                .Select(e => new { r = e, Ws = e.Kids.Select(k => k.W).ToList() }).Single();

            Assert.Equal([1, 2], row.Ws);
            Assert.Same(tracked, row.r);
            Assert.Equal(99, row.r.Kids[1].W);

            // R6: an owned entity is tracked only with its tracked owner, which holds it.
            var owners = db.ChangeTracker.Entries<Owner>().Select(o => o.Entity).ToList();
            var kids = db.ChangeTracker.Entries<Kid>().ToList();
            Assert.Equal(2, kids.Count);
            Assert.All(kids, kid => Assert.Contains(owners, owner => owner.Kids.Contains(kid.Entity)));
        }
    }

    [Fact]
    public void Owner_after_scalar_select_reads_stored_values()
    {
        var collection = Seed();
        foreach (var mode in AllModes)
        {
            using var db = CreateWithUnsavedChange(collection, mode);

            var row = db.Entities.Where(e => e.Id == 1)
                .Select(e => new { Ws = e.Kids.Select(k => k.W).ToList(), r = e }).Single();

            Assert.Equal([1, 2], row.Ws);
            Assert.Equal(99, row.r.Kids[1].W);
        }
    }

    [Fact]
    public void Declined_scalar_selector_reads_stored_values()
    {
        var collection = Seed();
        NativeModeAssert.NativeAndExpected(
            mode =>
            {
                using var db = CreateWithUnsavedChange(collection, mode);
                var row = db.Entities.Where(e => e.Id == 1)
                    .Select(e => new { e.Name, Ws = e.Kids.Select(k => k.W.ToString()).ToList() }).Single();
                return row.Ws;
            },
            ["1", "2"]);
    }

    [Fact]
    public void Other_selector_beside_owner_sees_tracked_instance()
    {
        var collection = Seed();
        // Not native: EF drops the identity Select, leaving `ToList(AsQueryable(EF.Property(e, "Kids")))`, which no projection leaf arm
        // matches (neither the bare owned collection nor the element Select), so the provider's mixed shaper serves it.
        foreach (var mode in new[] { MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            using var db = CreateWithUnsavedChange(collection, mode);

            var row = db.Entities.Where(e => e.Id == 1)
                .Select(e => new { r = e, Ks = e.Kids.Select(k => k).ToList() }).Single();

            Assert.Equal(99, row.Ks[1].W);
            Assert.Same(row.r.Kids[1], row.Ks[1]);
        }
    }

    public static TheoryData<QueryTrackingBehavior> TrackingBehaviors
        => new(QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking, QueryTrackingBehavior.NoTrackingWithIdentityResolution);

    // R1, R3: a bare body is native, reads the stored values (not the tracked owner's unsaved change) and tracks nothing, for every tracking behavior.
    [Theory]
    [MemberData(nameof(TrackingBehaviors))]
    public void Bare_scalar_select_is_native(QueryTrackingBehavior tracking)
    {
        var collection = Seed();
        using var db = CreateWithUnsavedChange(collection, MongoQueryMode.NativeOnly);
        var before = db.ChangeTracker.Entries().Count();

        var rows = db.Entities.AsTracking(tracking).OrderBy(e => e.Id)
            .Select(r => r.Kids.Select(k => k.W).ToList()).ToList();

        Assert.Equal(StoredRoots(collection).Select(r => r.Kids.Select(k => k.W).ToList()).ToList(), rows);
        Assert.Equal([[1, 2], []], rows);
        Assert.Equal(before, db.ChangeTracker.Entries().Count());
    }

    // R5, R6: beside the whole root the leaf reads the stored values; under TrackAll the root is the tracked instance (with its unsaved change)
    // and its owned graph is tracked only with it, otherwise it is a fresh instance read from the store and nothing new is tracked.
    [Theory]
    [MemberData(nameof(TrackingBehaviors))]
    public void Whole_root_beside_scalar_select_is_native(QueryTrackingBehavior tracking)
    {
        var collection = Seed();
        using var db = CreateWithUnsavedChange(collection, MongoQueryMode.NativeOnly);
        var tracked = db.Entities.Local.Single(e => e.Id == 1);
        var before = db.ChangeTracker.Entries().Count();

        var row = db.Entities.AsTracking(tracking).Where(e => e.Id == 1)
            .Select(e => new { r = e, Ws = e.Kids.Select(k => k.W).ToList() }).Single();

        var stored = StoredRoots(collection).Where(e => e.Id == 1).Select(e => new { r = e, Ws = e.Kids.Select(k => k.W).ToList() }).Single();
        Assert.Equal(stored.Ws, row.Ws);
        Assert.Equal([1, 2], row.Ws);
        Assert.Equal(before, db.ChangeTracker.Entries().Count());

        if (tracking == QueryTrackingBehavior.TrackAll)
        {
            Assert.Same(tracked, row.r);
            Assert.Equal(99, row.r.Kids[1].W);
        }
        else
        {
            Assert.NotSame(tracked, row.r);
            Assert.Equal(stored.r.Kids.Select(k => k.W), row.r.Kids.Select(k => k.W));
        }

        var owners = db.ChangeTracker.Entries<Owner>().Select(o => o.Entity).ToList();
        var kids = db.ChangeTracker.Entries<Kid>().ToList();
        Assert.Equal(2, kids.Count);
        Assert.All(kids, kid => Assert.Contains(owners, owner => owner.Kids.Contains(kid.Entity)));
    }

    // An Other selector (it uses the element itself) as a bare body is native too (#122): untracked it matches the in-memory oracle; tracked,
    // EF's refusal of an owned entity without its owner stays (R1).
    [Fact]
    public void Bare_other_selector_is_native()
    {
        var collection = Seed();
        using var db = Create(collection, MongoQueryMode.NativeOnly);

        var rows = db.Entities.AsNoTracking().OrderBy(e => e.Id)
            .Select(r => r.Kids.Select(k => new { k, k.W }).ToList()).ToList();

        var expected = StoredRoots(collection).Select(r => r.Kids.Select(k => new { k, k.W }).ToList()).ToList();
        Assert.Equal(
            expected.Select(r => r.Select(p => $"{p.k.Label}:{p.k.W}:{p.k.Weight}:{p.k.Size}:{p.W}").ToList()).ToList(),
            rows.Select(r => r.Select(p => $"{p.k.Label}:{p.k.W}:{p.k.Weight}:{p.k.Size}:{p.W}").ToList()).ToList());
        Assert.Equal(["a:1:10.5:Small:1", "b:2:20.25:Big:2"], rows[0].Select(p => $"{p.k.Label}:{p.k.W}:{p.k.Weight}:{p.k.Size}:{p.W}"));
        Assert.Empty(db.ChangeTracker.Entries());

        var exception = Assert.Throws<InvalidOperationException>(
            () => db.Entities.OrderBy(e => e.Id).Select(r => r.Kids.Select(k => new { k, k.W }).ToList()).ToList());
        Assert.Contains("owned entity without a corresponding owner", exception.Message);
    }

    // R1: tracking decides what a result is, never whether a query translates; EF Core's own refusal of an owned entity without its owner stays.
    [Fact]
    public void Bare_owned_collection_tracked_keeps_efs_refusal()
    {
        var collection = Seed();
        using var db = Create(collection, MongoQueryMode.NativeOnly);

        var exception = Assert.Throws<InvalidOperationException>(
            () => db.Entities.OrderBy(e => e.Id).Select(r => r.Kids).ToList());
        Assert.Contains("owned entity without a corresponding owner", exception.Message);

        var kids = db.Entities.AsNoTracking().OrderBy(e => e.Id).Select(r => r.Kids).ToList();
        Assert.Equal([["a", "b"], []], kids.Select(k => k.Select(kid => kid.Label).ToList()).ToList());
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public void Missing_read_scalar_throws()
    {
        var collection = SeedWithElementMissingW();
        foreach (var mode in AllModes)
        {
            using var db = Create(collection, mode);
            var exception = Assert.ThrowsAny<Exception>(
                () => db.Entities.Where(e => e.Id == 3)
                    .Select(e => new { e.Name, Ws = e.Kids.Select(k => k.W).ToList() }).Single());
            Assert.Contains("'W'", exception.Message);
        }
    }

    [Fact]
    public void Unread_missing_scalar_does_not_throw()
    {
        var collection = SeedWithElementMissingW();
        NativeModeAssert.NativeAndExpected(
            mode =>
            {
                using var db = Create(collection, mode);
                return db.Entities.Where(e => e.Id == 3)
                    .Select(e => new { e.Name, Labels = e.Kids.Select(k => k.Label).ToList() }).Single().Labels;
            },
            ["c"]);
    }

    [Fact]
    public void Converted_member_reads_clr_value()
    {
        var collection = Seed();
        NativeModeAssert.NativeAndExpected(
            mode =>
            {
                using var db = CreateWithUnsavedChange(collection, mode);
                var row = db.Entities.Where(e => e.Id == 1)
                    .Select(e => new { e.Name, Ps = e.Kids.Select(k => new { k.Weight, k.Size }).ToList() }).Single();
                return row.Ps.Select(p => $"{p.Weight}:{p.Size}").ToList();
            },
            ["10.5:Small", "20.25:Big"]);
    }

    [Fact]
    public async Task Async_reads_stored_values()
    {
        var collection = Seed();
        foreach (var mode in AllModes)
        {
            using var db = CreateWithUnsavedChange(collection, mode);
            var before = db.ChangeTracker.Entries().Count();

            var row = await db.Entities.Where(e => e.Id == 1)
                .Select(e => new { e.Name, Ws = e.Kids.Select(k => k.W).ToList() }).SingleAsync(TestContext.Current.CancellationToken);

            Assert.Equal([1, 2], row.Ws);
            Assert.Equal(before, db.ChangeTracker.Entries().Count());
        }
    }

    [Fact]
    public void Hierarchy_owner_reads_stored_values()
    {
        var collection = SeedAnimals();
        foreach (var mode in AllModes)
        {
            using var db = CreateAnimals(collection, mode);
            var dog = db.Entities.Single(a => a.Id == 1);
            dog.Kids[1].W = 99;
            var before = db.ChangeTracker.Entries().Count();

            var row = db.Entities.Where(a => a.Id == 1)
                .Select(a => new { a.Name, Ws = a.Kids.Select(k => k.W).ToList() }).Single();

            Assert.Equal([1, 2], row.Ws);
            Assert.Equal(before, db.ChangeTracker.Entries().Count());
        }
    }

    /// <summary>
    /// A captured variable in the element selector (#119) is read from the query context on every execution, never baked into the cached shaper:
    /// the same query runs twice in one context with different values. The forms take different read paths: fused into the streaming cursor
    /// (<c>Fused</c>), the stored-value DOM shaper because the array is also read whole (<c>BesideArray</c>), beside the whole owner
    /// (<c>BesideRoot</c>), the stored-value DOM shaper because the cursor declines the lambda (<c>Declined</c>), a bare body (<c>Bare</c>), and an
    /// <c>Other</c> selector over the materialized
    /// owned entities (untracked: EF refuses an owned entity without its owner under tracking).
    /// </summary>
    [Theory]
    [InlineData(MongoQueryMode.NativeOnly, QueryTrackingBehavior.TrackAll)]
    [InlineData(MongoQueryMode.NativeOnly, QueryTrackingBehavior.NoTracking)]
    [InlineData(MongoQueryMode.Native, QueryTrackingBehavior.TrackAll)]
    [InlineData(MongoQueryMode.DriverLinq, QueryTrackingBehavior.TrackAll)]
    [InlineData(MongoQueryMode.DriverLinq, QueryTrackingBehavior.NoTracking)]
    public void Captured_variable_in_selector_reads_per_execution(MongoQueryMode mode, QueryTrackingBehavior tracking)
    {
        var collection = Seed();
        using var db = CreateWithUnsavedChange(collection, mode);
        var query = db.Entities.AsTracking(tracking).Where(e => e.Id == 1);

        List<int> Fused(int offset)
            => query.Select(r => new { r.Name, Ws = r.Kids.Select(k => k.W + offset).ToList() }).Single().Ws;

        // Untracked: EF refuses the owned collection without its owner under tracking.
        List<int> BesideArray(int offset)
            => db.Entities.AsNoTracking().Where(e => e.Id == 1)
                .Select(r => new { r.Kids, Ws = r.Kids.Select(k => k.W + offset).ToList() }).Single().Ws;

        List<int> BesideRoot(int offset)
            => query.Select(r => new { r, Ws = r.Kids.Select(k => k.W + offset).ToList() }).Single().Ws;

        List<string> Declined(int offset)
            => query.Select(r => new { r.Name, Ws = r.Kids.Select(k => (k.W + offset).ToString()).ToList() }).Single().Ws;

        List<int> Bare(int offset)
            => query.Select(r => r.Kids.Select(k => k.W + offset).ToList()).Single();

        List<string> Other(int offset)
            => db.Entities.AsNoTracking().Where(e => e.Id == 1)
                .Select(r => r.Kids.Select(k => new { k, V = k.W + offset }).ToList()).Single()
                .Select(p => $"{p.k.Label}:{p.V}").ToList();

        Assert.Equal([11, 12], Fused(10));
        Assert.Equal([21, 22], Fused(20));
        Assert.Equal([11, 12], BesideArray(10));
        Assert.Equal([21, 22], BesideArray(20));
        Assert.Equal([11, 12], BesideRoot(10));
        Assert.Equal([21, 22], BesideRoot(20));
        Assert.Equal(["11", "12"], Declined(10));
        Assert.Equal(["21", "22"], Declined(20));
        Assert.Equal([11, 12], Bare(10));
        Assert.Equal([21, 22], Bare(20));
        Assert.Equal(["a:11", "b:12"], Other(10));
        Assert.Equal(["a:21", "b:22"], Other(20));
    }

    private static SingleEntityDbContext<Owner> Create(IMongoCollection<Owner> collection, MongoQueryMode mode)
        => SingleEntityDbContext.Create(
            collection,
            mb => mb.Entity<Owner>().OwnsMany(e => e.Kids, o =>
            {
                o.HasElementName("Kids");
                o.Property(k => k.Size).HasConversion<string>();
            }),
            optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });

    // The R5 seed: the owner with Id 1 is tracked and its second element's W changed to 99, unsaved; the stored values are 1 and 2.
    private static SingleEntityDbContext<Owner> CreateWithUnsavedChange(IMongoCollection<Owner> collection, MongoQueryMode mode)
    {
        var db = Create(collection, mode);
        var root = db.Entities.Single(e => e.Id == 1);
        root.Kids[1].W = 99;
        return db;
    }

    // The oracle: the stored roots, materialized without tracking, so a query evaluated over them in memory reads the stored values.
    private static List<Owner> StoredRoots(IMongoCollection<Owner> collection)
    {
        using var db = Create(collection, MongoQueryMode.NativeOnly);
        return db.Entities.AsNoTracking().OrderBy(e => e.Id).ToList();
    }

    private IMongoCollection<Owner> Seed()
    {
        var name = TemporaryDatabaseFixtureBase.CreateCollectionName("kids") + Guid.NewGuid().ToString("N")[..8];
        var collection = database.MongoDatabase.GetCollection<Owner>(name);
        using var db = Create(collection, MongoQueryMode.DriverLinq);
        db.Entities.AddRange(
            new Owner
            {
                Id = 1,
                Name = "x",
                Kids =
                [
                    new Kid { Label = "a", Weight = 10.5m, W = 1, Size = Size.Small },
                    new Kid { Label = "b", Weight = 20.25m, W = 2, Size = Size.Big }
                ]
            },
            new Owner { Id = 2, Name = "y", Kids = [] });
        db.SaveChanges();
        return collection;
    }

    private IMongoCollection<Owner> SeedWithElementMissingW()
    {
        var name = TemporaryDatabaseFixtureBase.CreateCollectionName("kidsMissing") + Guid.NewGuid().ToString("N")[..8];
        database.MongoDatabase.GetCollection<BsonDocument>(name).InsertOne(
            new BsonDocument
            {
                { "_id", 3 },
                { "Name", "z" },
                { "Kids", new BsonArray { new BsonDocument { { "Label", "c" }, { "Weight", "1.5" }, { "Size", "Small" } } } }
            });
        return database.MongoDatabase.GetCollection<Owner>(name);
    }

    private static SingleEntityDbContext<Animal> CreateAnimals(IMongoCollection<Animal> collection, MongoQueryMode mode)
        => SingleEntityDbContext.Create(
            collection,
            mb =>
            {
                mb.Entity<Animal>().OwnsMany(e => e.Kids, o => o.Property(k => k.Size).HasConversion<string>());
                mb.Entity<Dog>();
            },
            optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });

    private IMongoCollection<Animal> SeedAnimals()
    {
        var name = TemporaryDatabaseFixtureBase.CreateCollectionName("animals") + Guid.NewGuid().ToString("N")[..8];
        var collection = database.MongoDatabase.GetCollection<Animal>(name);
        using var db = CreateAnimals(collection, MongoQueryMode.DriverLinq);
        db.Entities.Add(new Dog { Id = 1, Name = "rex", Breed = "lab", Kids = [new Kid { Label = "a", W = 1 }, new Kid { Label = "b", W = 2 }] });
        db.SaveChanges();
        return collection;
    }
}
