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
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// An element leaf over a cross-document collection passed as an argument of a construction that names no members
/// (<c>new Holder(a.Name, a.Methods.Select(m =&gt; new Info(..)).ToList())</c>) used to be read back by argument position through the
/// result type's serializer, which ignores the element selector: with a DTO that ignores extra elements that is silently the stored
/// values. It is refused, naming the shape (<see cref="ElementLeafDeclineReasons.ConstructorArgument"/>), in every mode. The same leaf as a
/// member of an anonymous or named type runs the selector.
/// </summary>
public class NativeElementLeafConstructorArgumentTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    public static readonly TheoryData<MongoQueryMode, QueryTrackingBehavior> ModesAndTracking = new()
    {
        { MongoQueryMode.NativeOnly, QueryTrackingBehavior.NoTracking },
        { MongoQueryMode.NativeOnly, QueryTrackingBehavior.TrackAll },
        { MongoQueryMode.Native, QueryTrackingBehavior.NoTracking },
        { MongoQueryMode.Native, QueryTrackingBehavior.TrackAll },
        { MongoQueryMode.DriverLinq, QueryTrackingBehavior.NoTracking },
        { MongoQueryMode.DriverLinq, QueryTrackingBehavior.TrackAll },
    };

    private class Animal
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public List<Method> Methods { get; set; } = null!;
    }

    private class Method
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public int Rank { get; set; }
        public ObjectId AnimalId { get; set; }
    }

    [BsonIgnoreExtraElements]
    private class Info(int rank, string name)
    {
        public int Rank { get; } = rank;
        public string Name { get; } = name;
    }

    private class Holder(string name, List<Info> infos)
    {
        public string Name { get; } = name;
        public List<Info> Infos { get; } = infos;
    }

    private class HolderDto
    {
        public string Name { get; set; } = "";
        public List<Info> Infos { get; set; } = null!;
    }

    [Theory]
    [MemberData(nameof(ModesAndTracking))]
    public void A_correlation_only_leaf_as_a_constructor_argument_is_refused(MongoQueryMode mode, QueryTrackingBehavior tracking)
    {
        using var db = CreateContext(mode, tracking, nameof(A_correlation_only_leaf_as_a_constructor_argument_is_refused));

        AssertDeclined(() => db.Animals
            .Select(a => new Holder(a.Name, a.Methods.Select(m => new Info(m.Rank * 10, m.Name + "!")).ToList()))
            .ToList());
    }

    [Theory]
    [MemberData(nameof(ModesAndTracking))]
    public void A_filtered_leaf_as_a_constructor_argument_is_refused(MongoQueryMode mode, QueryTrackingBehavior tracking)
    {
        using var db = CreateContext(mode, tracking, nameof(A_filtered_leaf_as_a_constructor_argument_is_refused));

        AssertDeclined(() => db.Animals
            .Select(a => new Holder(a.Name, a.Methods.Where(m => m.Rank > 0).Select(m => new Info(m.Rank * 10, m.Name + "!")).ToList()))
            .ToList());
    }

    [Theory]
    [MemberData(nameof(ModesAndTracking))]
    public void A_leaf_in_a_positional_container_is_refused(MongoQueryMode mode, QueryTrackingBehavior tracking)
    {
        using var db = CreateContext(mode, tracking, nameof(A_leaf_in_a_positional_container_is_refused));

        AssertDeclined(() => db.Animals
            .Select(a => new object[] { a.Name, a.Methods.Where(m => m.Rank > 0).Select(m => new Info(m.Rank * 10, m.Name + "!")).ToList() })
            .ToList());
    }

    [Theory]
    [MemberData(nameof(ModesAndTracking))]
    public void A_leaf_as_an_anonymous_type_member_runs_the_selector(MongoQueryMode mode, QueryTrackingBehavior tracking)
    {
        using var db = CreateContext(mode, tracking, nameof(A_leaf_as_an_anonymous_type_member_runs_the_selector));

        var correlationOnly = db.Animals
            .Select(a => new { a.Name, Infos = a.Methods.Select(m => new Info(m.Rank * 10, m.Name + "!")).ToList() })
            .ToList()
            .Select(r => Describe(r.Name, r.Infos));
        var filtered = db.Animals
            .Select(a => new { a.Name, Infos = a.Methods.Where(m => m.Rank > 0).Select(m => new Info(m.Rank * 10, m.Name + "!")).ToList() })
            .ToList()
            .Select(r => Describe(r.Name, r.Infos));

        Assert.Equal(["Fido:", "Rex:10:Microchip!,20:Tattoo!"], correlationOnly.Order());
        Assert.Equal(["Fido:", "Rex:10:Microchip!,20:Tattoo!"], filtered.Order());
    }

    [Theory]
    [MemberData(nameof(ModesAndTracking))]
    public void A_leaf_as_a_member_init_member_runs_the_selector(MongoQueryMode mode, QueryTrackingBehavior tracking)
    {
        using var db = CreateContext(mode, tracking, nameof(A_leaf_as_a_member_init_member_runs_the_selector));

        var correlationOnly = db.Animals
            .Select(a => new HolderDto { Name = a.Name, Infos = a.Methods.Select(m => new Info(m.Rank * 10, m.Name + "!")).ToList() })
            .ToList()
            .Select(r => Describe(r.Name, r.Infos));
        var filtered = db.Animals
            .Select(a => new HolderDto
            {
                Name = a.Name, Infos = a.Methods.Where(m => m.Rank > 0).Select(m => new Info(m.Rank * 10, m.Name + "!")).ToList()
            })
            .ToList()
            .Select(r => Describe(r.Name, r.Infos));

        Assert.Equal(["Fido:", "Rex:10:Microchip!,20:Tattoo!"], correlationOnly.Order());
        Assert.Equal(["Fido:", "Rex:10:Microchip!,20:Tattoo!"], filtered.Order());
    }

    private class PairE(string name, List<Method> methods)
    {
        public string Name { get; } = name;
        public List<Method> Methods { get; } = methods;
    }

    /// <summary>
    /// A whole navigation list with no selector as a constructor argument reads the element entities through their own serializer, so
    /// there is no selector to ignore: it is not refused, and works natively as on release. DriverLinq never supported it (the driver-LINQ
    /// bridge's cross-DbSet refusal, unchanged).
    /// </summary>
    [Theory]
    [MemberData(nameof(ModesAndTracking))]
    public void A_navigation_list_with_no_selector_as_a_constructor_argument_is_read(MongoQueryMode mode, QueryTrackingBehavior tracking)
    {
        using var db = CreateContext(mode, tracking, nameof(A_navigation_list_with_no_selector_as_a_constructor_argument_is_read));

        var query = db.Animals.Select(a => new PairE(a.Name, a.Methods.ToList()));

        if (mode == MongoQueryMode.DriverLinq)
        {
            var exception = Assert.Throws<InvalidOperationException>(() => query.ToList());
            Assert.Contains("Unsupported cross-DbSet query", exception.Message);
            return;
        }

        var rows = query
            .ToList()
            .Select(r => $"{r.Name}:{string.Join(",", r.Methods.OrderBy(m => m.Rank).Select(m => $"{m.Rank}:{m.Name}"))}");

        Assert.Equal(["Fido:", "Rex:1:Microchip,2:Tattoo"], rows.Order());
    }

    private class OnlyInfos(List<Info> infos)
    {
        public List<Info> Infos { get; } = infos;
    }

    /// <summary>A one-argument constructor is read through the bare-body path, which binds the leaf itself and runs the selector.</summary>
    [Theory]
    [MemberData(nameof(ModesAndTracking))]
    public void A_leaf_as_the_only_constructor_argument_runs_the_selector(MongoQueryMode mode, QueryTrackingBehavior tracking)
    {
        using var db = CreateContext(mode, tracking, nameof(A_leaf_as_the_only_constructor_argument_runs_the_selector));

        var rows = db.Animals
            .Select(a => new OnlyInfos(a.Methods.Select(m => new Info(m.Rank * 10, m.Name + "!")).ToList()))
            .ToList()
            .Select(r => Describe("", r.Infos));

        Assert.Equal([":", ":10:Microchip!,20:Tattoo!"], rows.Order());
    }

    /// <summary>A member-less construction nested in an anonymous type is not translated (no wrong rows); the leaf is not reached, so no reason is named.</summary>
    [Theory]
    [MemberData(nameof(ModesAndTracking))]
    public void A_leaf_in_a_nested_member_less_construction_is_not_translated(MongoQueryMode mode, QueryTrackingBehavior tracking)
    {
        using var db = CreateContext(mode, tracking, nameof(A_leaf_in_a_nested_member_less_construction_is_not_translated));

        var exception = Assert.Throws<InvalidOperationException>(() => db.Animals
            .Select(a => new { a.Name, H = new Holder(a.Name, a.Methods.Select(m => new Info(m.Rank * 10, m.Name + "!")).ToList()) })
            .ToList());
        Assert.Contains("could not be translated", exception.Message);
    }
    private static string Describe(string name, List<Info> infos)
        => $"{name}:{string.Join(",", infos.OrderBy(i => i.Rank).Select(i => $"{i.Rank}:{i.Name}"))}";

    private static void AssertDeclined(Action query)
    {
        var exception = Assert.Throws<InvalidOperationException>(query);
        Assert.Contains("could not be translated", exception.Message);
        Assert.Contains(ElementLeafDeclineReasons.ConstructorArgument, exception.Message);
    }

    private AnimalDbContext CreateContext(MongoQueryMode mode, QueryTrackingBehavior tracking, string name)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var animals = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "A" + suffix;
        var methods = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "M" + suffix;

        var rex = ObjectId.GenerateNewId();
        database.MongoDatabase.GetCollection<Animal>(animals).InsertMany(
        [
            new Animal { Id = rex, Name = "Rex" },
            new Animal { Id = ObjectId.GenerateNewId(), Name = "Fido" },
        ]);
        database.MongoDatabase.GetCollection<Method>(methods).InsertMany(
        [
            new Method { Id = ObjectId.GenerateNewId(), Name = "Microchip", Rank = 1, AnimalId = rex },
            new Method { Id = ObjectId.GenerateNewId(), Name = "Tattoo", Rank = 2, AnimalId = rex },
        ]);

        var db = new AnimalDbContext(database, animals, methods, mode);
        db.ChangeTracker.QueryTrackingBehavior = tracking;
        return db;
    }

    private class AnimalDbContext(TemporaryDatabaseFixture database, string animals, string methods, MongoQueryMode mode)
        : DbContext(new DbContextOptionsBuilder<AnimalDbContext>()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName, b => b.UseQueryMode(mode))
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options)
    {
        public DbSet<Animal> Animals { get; set; } = null!;
        public DbSet<Method> Methods { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Animal>().ToCollection(animals);
            modelBuilder.Entity<Animal>().HasMany(a => a.Methods).WithOne().HasForeignKey(m => m.AnimalId);
            modelBuilder.Entity<Method>().ToCollection(methods);
        }

        private sealed class IgnoreCacheKeyFactory : IModelCacheKeyFactory
        {
            private static int _count;
            public object Create(DbContext context, bool designTime) => Interlocked.Increment(ref _count);
        }
    }
}
