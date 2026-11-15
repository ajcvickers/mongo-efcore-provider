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
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// A projected element leaf over a cross-document collection (<c>a.Methods.Where(..).Select(..).ToList()</c>) that the native binder
/// recognizes and then declines fails with EF's "could not be translated" naming the part that is not supported
/// (<see cref="ElementLeafDeclineReasons"/>), never just <c>DbSet&lt;Method&gt;()</c>. Element leaves have no driver-LINQ fallback, so the
/// message is the same in every query mode.
/// </summary>
public class NativeElementLeafRefusalTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    public static readonly TheoryData<MongoQueryMode> Modes =
        [MongoQueryMode.Native, MongoQueryMode.NativeOnly, MongoQueryMode.DriverLinq];

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
        public Animal Animal { get; set; } = null!;
        public Detail Detail { get; set; } = new();
        public List<Piece> Pieces { get; set; } = [];
        public List<Tag> Tags { get; set; } = null!;
    }

    private class Tag
    {
        public ObjectId Id { get; set; }
        public ObjectId MethodId { get; set; }
    }

    private class Detail
    {
        public string Label { get; set; } = "";
    }

    private class Piece
    {
        public int P { get; set; }
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void A_captured_value_in_the_element_selector_is_named(MongoQueryMode mode)
    {
        using var db = CreateContext(mode, nameof(A_captured_value_in_the_element_selector_is_named));
        var sfx = "!";

        AssertDeclined(
            ElementLeafDeclineReasons.CapturedValueInSelector,
            () => db.Animals.Select(a => a.Methods.Where(m => m.Rank == 2).Select(m => m.Name + sfx).ToList()).ToList());
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void The_outer_entity_in_the_element_selector_is_named(MongoQueryMode mode)
    {
        using var db = CreateContext(mode, nameof(The_outer_entity_in_the_element_selector_is_named));

        AssertDeclined(
            ElementLeafDeclineReasons.OuterEntityInSelector,
            () => db.Animals.Select(a => a.Methods.OrderBy(m => m.Rank).Select(m => m.Name + a.Name).ToList()).ToList());
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void A_captured_value_in_a_subquery_of_the_element_selector_is_named(MongoQueryMode mode)
    {
        using var db = CreateContext(mode, nameof(A_captured_value_in_a_subquery_of_the_element_selector_is_named));
        var name = "Rex";

        AssertDeclined(
            ElementLeafDeclineReasons.CapturedValueInSelector,
            () => db.Animals.Select(a => a.Methods.OrderBy(m => m.Rank).Select(m => db.Animals.Count(x => x.Name == name)).ToList()).ToList());
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void EF_Property_in_the_element_selector_is_named(MongoQueryMode mode)
    {
        using var db = CreateContext(mode, nameof(EF_Property_in_the_element_selector_is_named));

        AssertDeclined(
            ElementLeafDeclineReasons.SelectorUsesEFProperty,
            () => db.Animals.Select(a => a.Methods.OrderBy(m => m.Rank).Select(m => EF.Property<string>(m, "Name")).ToList()).ToList());
    }

    /// <summary>
    /// A subquery in the selector is neither a captured value, an owned navigation nor <c>EF.Property</c>: the reason is the neutral one, which
    /// names no cause the binder did not establish.
    /// </summary>
    [Theory]
    [MemberData(nameof(Modes))]
    public void A_subquery_in_the_element_selector_gets_the_neutral_reason(MongoQueryMode mode)
    {
        using var db = CreateContext(mode, nameof(A_subquery_in_the_element_selector_gets_the_neutral_reason));

        AssertDeclined(
            ElementLeafDeclineReasons.SelectorNotSelfContained,
            () => db.Animals.Select(a => a.Methods.OrderBy(m => m.Rank).Select(m => db.Animals.Count(x => x.Name == "Rex")).ToList()).ToList());
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void An_owned_reference_read_in_the_element_selector_is_named(MongoQueryMode mode)
    {
        using var db = CreateContext(mode, nameof(An_owned_reference_read_in_the_element_selector_is_named), withOwned: true);

        AssertDeclined(
            ElementLeafDeclineReasons.SelectorReadsOwnedNavigation,
            () => db.Animals.Select(a => a.Methods.OrderBy(m => m.Rank).Select(m => m.Detail.Label).ToList()).ToList());
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void An_owned_collection_read_in_the_element_selector_is_named(MongoQueryMode mode)
    {
        using var db = CreateContext(mode, nameof(An_owned_collection_read_in_the_element_selector_is_named), withOwned: true);

        AssertDeclined(
            ElementLeafDeclineReasons.SelectorReadsOwnedNavigation,
            () => db.Animals.Select(a => a.Methods.OrderBy(m => m.Rank).Select(m => m.Pieces.Count).ToList()).ToList());
    }

    /// <summary>
    /// Nav-expansion includes the element's owned navigations through a selector of their own (<c>m =&gt; Include(m, m.Detail)</c>), so an
    /// element leaf whose type owns anything is declined even when it selects nothing.
    /// </summary>
    [Theory]
    [MemberData(nameof(Modes))]
    public void An_element_type_with_owned_navigations_is_named(MongoQueryMode mode)
    {
        using var db = CreateContext(mode, nameof(An_element_type_with_owned_navigations_is_named), withOwned: true);

        AssertDeclined(
            ElementLeafDeclineReasons.OwnedNavigations,
            () => db.Animals.Select(a => new { a.Name, Methods = a.Methods.OrderBy(m => m.Rank).ToList() }).ToList());
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void An_eagerly_loaded_navigation_of_the_element_type_is_named(MongoQueryMode mode)
    {
        using var db = CreateContext(mode, nameof(An_eagerly_loaded_navigation_of_the_element_type_is_named), autoIncludeTags: true);

        AssertDeclined(
            ElementLeafDeclineReasons.EagerLoadedNavigation,
            () => db.Animals.Select(a => new { a.Name, Methods = a.Methods.OrderBy(m => m.Rank).ToList() }).ToList());
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void A_correlation_that_is_not_a_key_equality_is_named(MongoQueryMode mode)
    {
        using var db = CreateContext(mode, nameof(A_correlation_that_is_not_a_key_equality_is_named));

        AssertDeclined(
            ElementLeafDeclineReasons.Correlation,
            () => db.Animals.Select(a => db.Methods.Where(m => m.Name == a.Name + "x").ToList()).ToList());
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void An_ordering_key_that_is_not_a_stored_scalar_is_named(MongoQueryMode mode)
    {
        using var db = CreateContext(mode, nameof(An_ordering_key_that_is_not_a_stored_scalar_is_named));

        AssertDeclined(
            ElementLeafDeclineReasons.SortKey,
            () => db.Animals.Select(a => a.Methods.OrderBy(m => m.Name.Length).ToList()).ToList());
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void An_element_filter_that_reads_the_outer_entity_is_named(MongoQueryMode mode)
    {
        using var db = CreateContext(mode, nameof(An_element_filter_that_reads_the_outer_entity_is_named));

        AssertDeclined(
            ElementLeafDeclineReasons.ElementFilterReadsOuterEntity,
            () => db.Animals.Select(a => a.Methods.Where(m => m.Name == a.Name).ToList()).ToList());
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void An_element_filter_that_does_not_translate_is_named(MongoQueryMode mode)
    {
        using var db = CreateContext(mode, nameof(An_element_filter_that_does_not_translate_is_named));

        AssertDeclined(
            ElementLeafDeclineReasons.ElementFilter,
            () => db.Animals.Select(a => a.Methods.Where(m => m.Name.Normalize() == "x").ToList()).ToList());
    }

    private static void AssertDeclined(string reason, Action query)
    {
        var exception = Assert.Throws<InvalidOperationException>(query);
        Assert.Contains("could not be translated", exception.Message);
        Assert.Contains(reason, exception.Message);
    }

    private AnimalDbContext CreateContext(MongoQueryMode mode, string name, bool autoIncludeTags = false, bool withOwned = false)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var animals = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "A" + suffix;
        var methods = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "M" + suffix;
        return new AnimalDbContext(database, animals, methods, mode, autoIncludeTags, withOwned);
    }

    private class AnimalDbContext(
        TemporaryDatabaseFixture database, string animals, string methods, MongoQueryMode mode, bool autoIncludeTags, bool withOwned)
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
            modelBuilder.Entity<Tag>().ToCollection(animals + "T");
            modelBuilder.Entity<Animal>().HasMany(a => a.Methods).WithOne(m => m.Animal).HasForeignKey(m => m.AnimalId);
            modelBuilder.Entity<Method>(b =>
            {
                b.ToCollection(methods);
                if (withOwned)
                {
                    b.OwnsOne(m => m.Detail);
                    b.OwnsMany(m => m.Pieces);
                }
                else
                {
                    b.Ignore(m => m.Detail);
                    b.Ignore(m => m.Pieces);
                }

                b.HasMany(m => m.Tags).WithOne().HasForeignKey(t => t.MethodId);
                if (autoIncludeTags)
                    b.Navigation(m => m.Tags).AutoInclude();
            });
        }

        private sealed class IgnoreCacheKeyFactory : IModelCacheKeyFactory
        {
            private static int _count;
            public object Create(DbContext context, bool designTime) => Interlocked.Increment(ref _count);
        }
    }
}
