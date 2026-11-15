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
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// End-to-end coverage for a reference-collection-nav <c>First</c>/<c>FirstOrDefault</c> reduced to a scalar member
/// in a projection (<c>a.IdentificationMethods.FirstOrDefault().Method</c>).
/// </summary>
/// <remarks>
/// <c>FirstOrDefault().Member</c> on no match yields a missing field, read back as <c>default(T)</c> for a nullable
/// type. <c>First().Member</c> must throw on empty (<c>ThrowOnEmpty</c> leaf in <c>CorrelatedReducerLeaves</c>), which
/// is only sound for a non-nullable member (a matched row with a null member looks like no row), so
/// <c>TryGetCorrelatedReducerLeaf</c> declines <c>First()</c> over a nullable member and those tests reduce to <c>Rank</c>.
/// </remarks>
public class NativeCorrelatedReducerProjectionTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Animal
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public System.Collections.Generic.List<IdentificationMethod> IdentificationMethods { get; set; } = null!;
    }

    /// <summary>
    /// Enum member, i.e. a non-nullable value type, as in
    /// <c>BuiltInDataTypesMongoTest.Can_read_back_mapped_enum_from_collection_first_or_default</c>. See the
    /// "nullable-widened" section below.
    /// </summary>
    private enum IdentificationKind
    {
        Unknown = 0,
        Implant = 1,
        Visual = 2
    }

    private class IdentificationMethod
    {
        public ObjectId Id { get; set; }
        public string Method { get; set; } = "";
        public int Rank { get; set; }
        public IdentificationKind Kind { get; set; }
        public ObjectId AnimalId { get; set; }
        public Animal Animal { get; set; } = null!;

        /// <summary>A client-side method over the whole element, so a selector calling it uses the element itself.</summary>
        public string Describe() => Method;
    }

    private AnimalDbContext CreateContext(
        MongoQueryMode mode, string name, out ObjectId withMethods, out ObjectId withoutMethods,
        ILoggerFactory? loggerFactory = null, QueryTrackingBehavior? tracking = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var animals = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "A" + suffix;
        var methods = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "M" + suffix;

        withMethods = ObjectId.GenerateNewId();
        withoutMethods = ObjectId.GenerateNewId();

        database.MongoDatabase.GetCollection<Animal>(animals).InsertMany(
        [
            new Animal { Id = withMethods, Name = "Rex" },
            new Animal { Id = withoutMethods, Name = "Fido" },
        ]);
        database.MongoDatabase.GetCollection<IdentificationMethod>(methods).InsertMany(
        [
            new IdentificationMethod
            {
                Id = ObjectId.GenerateNewId(), Method = "Microchip", Rank = 1, Kind = IdentificationKind.Implant,
                AnimalId = withMethods
            },
            new IdentificationMethod
            {
                Id = ObjectId.GenerateNewId(), Method = "Tattoo", Rank = 2, Kind = IdentificationKind.Visual,
                AnimalId = withMethods
            },
        ]);

        return new AnimalDbContext(database, animals, methods, mode, loggerFactory, tracking);
    }

    [Fact]
    public void FirstOrDefault_member_returns_correct_value_when_a_related_row_exists()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(FirstOrDefault_member_returns_correct_value_when_a_related_row_exists),
            out var withMethods, out _);

        var result = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new { a.Id, M = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault()!.Method })
            .Single();

        Assert.Equal("Microchip", result.M);
    }

    /// <summary>
    /// No match: the dotted <c>$project</c> read through the nulled lookup field must produce a missing field, read
    /// back as null. Fails if MongoDB's <c>$project</c> semantics differ.
    /// </summary>
    [Fact]
    public void FirstOrDefault_over_empty_reference_collection_reads_as_default()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(FirstOrDefault_over_empty_reference_collection_reads_as_default),
            out _, out var withoutMethods);

        var result = db.Animals
            .Where(a => a.Id == withoutMethods)
            .Select(a => new { a.Id, M = a.IdentificationMethods.FirstOrDefault()!.Method })
            .Single();

        Assert.Null(result.M);
    }

    [Fact]
    public void First_member_returns_correct_value_when_a_related_row_exists()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(First_member_returns_correct_value_when_a_related_row_exists),
            out var withMethods, out _);

        var result = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new { a.Id, R = a.IdentificationMethods.OrderBy(m => m.Rank).First().Rank })
            .Single();

        Assert.Equal(1, result.R);
    }

    [Fact]
    public async Task First_over_empty_reference_collection_throws()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(First_over_empty_reference_collection_throws),
            out _, out var withoutMethods);

        var query = db.Animals
            .Where(a => a.Id == withoutMethods)
            .Select(a => new { a.Id, R = a.IdentificationMethods.First().Rank });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => query.SingleAsync());
        Assert.Equal("Sequence contains no elements", ex.Message);
    }

    /// <summary>Throw-on-empty under the default <see cref="MongoQueryMode.Native"/> mode too, not just
    /// <c>NativeOnly</c>.</summary>
    [Fact]
    public void First_over_empty_reference_collection_throws_under_default_native_mode()
    {
        using var db = CreateContext(
            MongoQueryMode.Native, nameof(First_over_empty_reference_collection_throws_under_default_native_mode),
            out _, out var withoutMethods);

        var query = db.Animals
            .Where(a => a.Id == withoutMethods)
            .Select(a => new { a.Id, R = a.IdentificationMethods.First().Rank });

        var ex = Assert.Throws<InvalidOperationException>(() => query.Single());
        Assert.Equal("Sequence contains no elements", ex.Message);
    }

    /// <summary>
    /// <c>First()</c> over a nullable member (<c>Method</c>): the correlated reducer can't tell "no row" from "row with a null Method" and
    /// declines it, but the element leaf keeps the row and runs the real <c>First</c> over it in the shaper, so the row's member is read.
    /// </summary>
    [Fact]
    public void First_over_nullable_typed_member_reads_the_member_of_the_first_row()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(First_over_nullable_typed_member_reads_the_member_of_the_first_row),
            out var withMethods, out _);

        var result = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new { a.Id, M = a.IdentificationMethods.OrderBy(m => m.Rank).First().Method })
            .Single();

        Assert.Equal("Microchip", result.M);
    }

    /// <summary>The empty group still throws, as LINQ's <c>First</c> does, rather than reading a null member.</summary>
    [Fact]
    public void First_over_nullable_typed_member_of_an_empty_collection_throws()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(First_over_nullable_typed_member_of_an_empty_collection_throws),
            out _, out var withoutMethods);

        var query = db.Animals
            .Where(a => a.Id == withoutMethods)
            .Select(a => new { a.Id, M = a.IdentificationMethods.OrderBy(m => m.Rank).First().Method });

        var ex = Assert.Throws<InvalidOperationException>(() => query.Single());
        Assert.Equal("Sequence contains no elements", ex.Message);
    }

    /// <summary>
    /// Control for <see cref="First_over_nullable_typed_member_reads_the_member_of_the_first_row"/>: <c>FirstOrDefault()</c> over the same
    /// nullable member reads correctly too.
    /// </summary>
    [Fact]
    public void FirstOrDefault_over_nullable_typed_member_still_works()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(FirstOrDefault_over_nullable_typed_member_still_works),
            out var withMethods, out _);

        var result = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new { a.Id, M = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault()!.Method })
            .Single();

        Assert.Equal("Microchip", result.M);
    }

    // The shapes the correlated reducer leaf declines (it has one row per animal and one sub-pipeline per alias) are element leaves: a keyed
    // lookup per distinct (filter, sort, skip, limit) that the shaper reads with the real LINQ operator. Run under NativeOnly, so each is
    // answered natively or the test fails. Rex holds Microchip (rank 1) and Tattoo (rank 2).

    [Fact]
    public void Two_leaves_over_one_navigation_read_their_own_rows()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(Two_leaves_over_one_navigation_read_their_own_rows), out var withMethods, out _);

        var result = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new
            {
                Low = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault()!.Method,
                High = a.IdentificationMethods.OrderByDescending(m => m.Rank).FirstOrDefault()!.Method
            })
            .Single();

        Assert.Equal("Microchip", result.Low);
        Assert.Equal("Tattoo", result.High);
    }

    /// <summary>
    /// An unordered leaf (the correlated reducer's) beside an ordered one (an element leaf) over the same navigation: the second must read its own
    /// row, not the row the first one's lookup holds under the shared navigation alias.
    /// </summary>
    [Fact]
    public void A_reducer_leaf_beside_an_element_leaf_over_one_navigation_reads_their_own_rows()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(A_reducer_leaf_beside_an_element_leaf_over_one_navigation_reads_their_own_rows),
            out var withMethods, out _);

        var result = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new
            {
                Any = a.IdentificationMethods.FirstOrDefault()!.Method,
                Best = a.IdentificationMethods.OrderByDescending(m => m.Rank).FirstOrDefault()!.Method
            })
            .Single();

        Assert.Contains(result.Any, new[] { "Microchip", "Tattoo" });
        Assert.Equal("Tattoo", result.Best);
    }

    /// <summary>
    /// <c>Single</c> keeps two rows in its lookup so a second match is seen and throws, as LINQ does; a single match is read.
    /// </summary>
    [Fact]
    public void Single_over_a_navigation_throws_for_two_rows_and_reads_one()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(Single_over_a_navigation_throws_for_two_rows_and_reads_one), out var withMethods, out var withoutMethods);

        var ambiguous = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new { M = a.IdentificationMethods.OrderBy(m => m.Rank).Single().Method });
        Assert.Throws<InvalidOperationException>(() => ambiguous.Single());

        var one = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new { M = a.IdentificationMethods.Single(m => m.Rank == 2).Method })
            .Single();
        Assert.Equal("Tattoo", one.M);

        var none = db.Animals
            .Where(a => a.Id == withoutMethods)
            .Select(a => new { M = a.IdentificationMethods.SingleOrDefault(m => m.Rank == 2)!.Method })
            .Single();
        Assert.Null(none.M);
    }

    /// <summary>
    /// A scalar selector over a single-result terminal projects the stored value: it must not resolve the identity of, or track, the intermediate
    /// entity, so an unsaved change to an already tracked row is not returned. Tracking is the default, and the row is tracked here.
    /// </summary>
    [Theory]
    [InlineData("First")]
    [InlineData("FirstOrDefault")]
    [InlineData("Single")]
    [InlineData("Last")]
    [InlineData("ElementAt")]
    public void A_scalar_selector_returns_the_stored_value_not_an_unsaved_change_to_a_tracked_row(string terminal)
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(A_scalar_selector_returns_the_stored_value_not_an_unsaved_change_to_a_tracked_row)
            + terminal, out var withMethods, out _);

        // Rank 2 is the only row each query below can return.
        var tracked = db.IdentificationMethods.Single(m => m.AnimalId == withMethods && m.Rank == 2);
        tracked.Method = "Unsaved";
        var trackedBefore = db.ChangeTracker.Entries<IdentificationMethod>().Count();

        var query = db.Animals.Where(a => a.Id == withMethods);
        var result = terminal switch
        {
            "First" => query.Select(a => a.IdentificationMethods.Where(m => m.Rank == 2).OrderBy(m => m.Rank).First().Method),
            "FirstOrDefault" => query.Select(a => a.IdentificationMethods.Where(m => m.Rank == 2).OrderBy(m => m.Rank).FirstOrDefault()!.Method),
            "Single" => query.Select(a => a.IdentificationMethods.Where(m => m.Rank == 2).Single().Method),
            "Last" => query.Select(a => a.IdentificationMethods.OrderBy(m => m.Rank).Last().Method),
            _ => query.Select(a => a.IdentificationMethods.OrderBy(m => m.Rank).ElementAt(1).Method)
        };

        Assert.Equal("Tattoo", result.Single());
        Assert.Equal("Unsaved", tracked.Method);
        // Nothing was tracked for the projection: the one tracked row is still the only one.
        Assert.Equal(trackedBefore, db.ChangeTracker.Entries<IdentificationMethod>().Count());
    }

    /// <summary>
    /// The scalar selector of a list with a computed element, and several beside other projected members all read the stored values too: no element entity is
    /// resolved against the change tracker, so the unsaved change to the tracked row is not seen, and nothing is tracked for the projection.
    /// </summary>
    [Fact]
    public void Scalar_selectors_of_lists_and_constructions_return_stored_values_for_a_tracked_row()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly,
            nameof(Scalar_selectors_of_lists_and_constructions_return_stored_values_for_a_tracked_row),
            out var withMethods, out _);

        var tracked = db.IdentificationMethods.Single(m => m.AnimalId == withMethods && m.Rank == 2);
        tracked.Method = "Unsaved";
        tracked.Rank = 20;
        var trackedBefore = db.ChangeTracker.Entries<IdentificationMethod>().Count();

        var list = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => a.IdentificationMethods.Where(m => m.Id == tracked.Id).OrderBy(m => m.Id).Select(m => m.Method + "!" + m.Rank * 2).ToList())
            .Single();
        Assert.Equal(["Tattoo!4"], list);

        var computed = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new
            {
                a.Id,
                Method = a.IdentificationMethods.Where(m => m.Id == tracked.Id).OrderBy(m => m.Id).First().Method,
                Rank = a.IdentificationMethods.Where(m => m.Id == tracked.Id).OrderBy(m => m.Id).First().Rank
            })
            .Single();
        Assert.Equal(withMethods, computed.Id);
        Assert.Equal("Tattoo", computed.Method);
        Assert.Equal(2, computed.Rank);

        Assert.Equal("Unsaved", tracked.Method);
        Assert.Equal(20, tracked.Rank);
        Assert.Equal(trackedBefore, db.ChangeTracker.Entries<IdentificationMethod>().Count());
    }

    /// <summary>
    /// The stored value is read in every query mode and tracking behavior of the query: tracked, <c>AsNoTracking</c> and
    /// <c>AsNoTrackingWithIdentityResolution</c>, with an unsaved change to the same row held by a tracked context.
    /// </summary>
    [Theory]
    [InlineData(MongoQueryMode.Native, "Tracked")]
    [InlineData(MongoQueryMode.Native, "NoTracking")]
    [InlineData(MongoQueryMode.Native, "NoTrackingWithIdentityResolution")]
    [InlineData(MongoQueryMode.NativeOnly, "NoTracking")]
    [InlineData(MongoQueryMode.NativeOnly, "NoTrackingWithIdentityResolution")]
    public void A_scalar_selector_returns_the_stored_value_in_every_query_mode_and_tracking_behavior(MongoQueryMode mode, string tracking)
    {
        using var db = CreateContext(
            mode, nameof(A_scalar_selector_returns_the_stored_value_in_every_query_mode_and_tracking_behavior) + tracking,
            out var withMethods, out _);

        var tracked = db.IdentificationMethods.Single(m => m.AnimalId == withMethods && m.Rank == 2);
        tracked.Method = "Unsaved";

        var animals = tracking switch
        {
            "NoTracking" => db.Animals.AsNoTracking(),
            "NoTrackingWithIdentityResolution" => db.Animals.AsNoTrackingWithIdentityResolution(),
            _ => db.Animals.AsQueryable()
        };

        var single = animals.Where(a => a.Id == withMethods)
            .Select(a => a.IdentificationMethods.Where(m => m.Rank == 2).OrderBy(m => m.Rank).First().Method).Single();
        var list = animals.Where(a => a.Id == withMethods)
            .Select(a => a.IdentificationMethods.OrderBy(m => m.Rank).Select(m => m.Method).ToList()).Single();

        Assert.Equal("Tattoo", single);
        Assert.Equal(["Microchip", "Tattoo"], list);
        Assert.Equal("Unsaved", tracked.Method);
    }

    /// <summary>
    /// A group with no element reads as LINQ does for the fused scalar selector: <c>First</c> throws, <c>FirstOrDefault</c> reads null and the list is
    /// empty, in the same query as a parent that has elements.
    /// </summary>
    [Fact]
    public void A_scalar_selector_over_an_empty_group_reads_as_LINQ_does()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(A_scalar_selector_over_an_empty_group_reads_as_LINQ_does), out var withMethods, out var withoutMethods);

        var rows = db.Animals
            .OrderBy(a => a.Name)
            .Select(a => new
            {
                a.Id,
                First = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault()!.Method,
                All = a.IdentificationMethods.OrderBy(m => m.Rank).Select(m => m.Method).ToList()
            })
            .ToList();

        Assert.Equal(2, rows.Count);
        var fido = rows.Single(r => r.Id == withoutMethods);
        Assert.Null(fido.First);
        Assert.Empty(fido.All);
        var rex = rows.Single(r => r.Id == withMethods);
        Assert.Equal("Microchip", rex.First);
        Assert.Equal(["Microchip", "Tattoo"], rex.All);

        Assert.Throws<InvalidOperationException>(
            () => db.Animals.Where(a => a.Id == withoutMethods)
                .Select(a => a.IdentificationMethods.OrderBy(m => m.Rank).First().Method).Single());
    }

    /// <summary>
    /// A tracked root entity beside a fused leaf keeps its identity, and the leaf and the list beside it read the stored values.
    /// </summary>
    [Fact]
    public void A_tracked_root_beside_a_scalar_selector_leaf_keeps_its_identity()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(A_tracked_root_beside_a_scalar_selector_leaf_keeps_its_identity), out var withMethods, out _);

        var tracked = db.IdentificationMethods.Single(m => m.AnimalId == withMethods && m.Rank == 2);
        tracked.Method = "Unsaved";
        var animal = db.Animals.Single(a => a.Id == withMethods);

        var row = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new
            {
                Animal = a,
                Last = a.IdentificationMethods.OrderBy(m => m.Rank).Last().Method,
                All = a.IdentificationMethods.OrderBy(m => m.Rank).Select(m => new { m.Method, m.Rank }).ToList()
            })
            .Single();

        Assert.Same(animal, row.Animal);
        Assert.Equal("Tattoo", row.Last);
        Assert.Equal(["Microchip", "Tattoo"], row.All.Select(x => x.Method));
        Assert.Equal("Unsaved", tracked.Method);
    }

    /// <summary>
    /// A scalar selector the streaming fusion declines (<c>Rank.ToString()</c>, <c>Kind.ToString()</c>) is still answered from the stored row: its
    /// elements are built outside the change tracker, whichever terminal reads them.
    /// </summary>
    [Theory]
    [InlineData("First", "2")]
    [InlineData("LastOrDefault", "2")]
    [InlineData("SingleOrDefault", "2")]
    [InlineData("ElementAtOrDefault", "2")]
    [InlineData("SkipTake", "2")]
    [InlineData("List", "1,2")]
    [InlineData("Enum", "Visual")]
    [InlineData("Concatenated", "Tattoo2")]
    public void A_scalar_selector_the_fusion_declines_returns_the_stored_row(string form, string expected)
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(A_scalar_selector_the_fusion_declines_returns_the_stored_row) + form, out var withMethods, out _);

        var tracked = db.IdentificationMethods.Single(m => m.AnimalId == withMethods && m.Rank == 2);
        var trackedId = tracked.Id;
        tracked.Method = "Unsaved";
        tracked.Rank = 99;
        tracked.Kind = IdentificationKind.Unknown;
        var trackedBefore = db.ChangeTracker.Entries<IdentificationMethod>().Count();

        var query = db.Animals.Where(a => a.Id == withMethods);
        string? result = form switch
        {
            "First" => query
                .Select(a => a.IdentificationMethods.Where(m => m.Id == trackedId).OrderBy(m => m.Id).Select(m => m.Rank.ToString()).First()).Single(),
            "LastOrDefault" => query
                .Select(a => a.IdentificationMethods.OrderBy(m => m.Id).Select(m => m.Rank.ToString()).LastOrDefault()).Single(),
            "SingleOrDefault" => query
                .Select(a => a.IdentificationMethods.Where(m => m.Id == trackedId).Select(m => m.Rank.ToString()).SingleOrDefault()).Single(),
            "ElementAtOrDefault" => query
                .Select(a => a.IdentificationMethods.OrderBy(m => m.Id).Select(m => m.Rank.ToString()).ElementAtOrDefault(1)).Single(),
            "SkipTake" => query
                .Select(a => a.IdentificationMethods.OrderBy(m => m.Id).Skip(1).Take(1).Select(m => m.Rank.ToString()).ToList())
                .Single().Single(),
            "List" => string.Join(
                ",", query.Select(a => a.IdentificationMethods.OrderBy(m => m.Id).Select(m => m.Rank.ToString()).ToList()).Single()),
            "Enum" => query
                .Select(a => a.IdentificationMethods.Where(m => m.Id == trackedId).Select(m => m.Kind.ToString()).ToList())
                .Single().Single(),
            _ => query
                .Select(a => a.IdentificationMethods.Where(m => m.Id == trackedId).Select(m => m.Method + m.Rank.ToString()).ToList())
                .Single().Single()
        };

        Assert.Equal(expected, result);
        Assert.Equal("Unsaved", tracked.Method);
        Assert.Equal(99, tracked.Rank);
        Assert.Equal(trackedBefore, db.ChangeTracker.Entries<IdentificationMethod>().Count());
    }

    [Fact]
    public async Task A_scalar_selector_the_fusion_declines_returns_the_stored_row_when_enumerated_asynchronously()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(A_scalar_selector_the_fusion_declines_returns_the_stored_row_when_enumerated_asynchronously),
            out var withMethods, out _);

        var tracked = await db.IdentificationMethods.SingleAsync(m => m.AnimalId == withMethods && m.Rank == 2);
        var trackedId = tracked.Id;
        tracked.Rank = 99;
        var query = db.Animals.Where(a => a.Id == withMethods);

        Assert.Equal(
            "2",
            await query.Select(a => a.IdentificationMethods.Where(m => m.Id == trackedId).OrderBy(m => m.Id).Select(m => m.Rank.ToString()).First())
                .SingleAsync());
        Assert.Equal(
            ["2"],
            await query.Select(a => a.IdentificationMethods.Where(m => m.Id == trackedId).Select(m => m.Rank.ToString()).ToList()).SingleAsync());

        Assert.Equal(99, tracked.Rank);
        Assert.Single(db.ChangeTracker.Entries<IdentificationMethod>());
    }

    /// <summary>
    /// What follows a declined scalar leaf in the same projection is built as usual: the tracked root entity beside it keeps its identity, and a whole
    /// element beside it is the tracked instance, while the leaf itself reads the stored row.
    /// </summary>
    [Theory]
    [InlineData("RootBesideLeaf")]
    [InlineData("ElementBesideLeaf")]
    public void An_entity_projected_beside_a_declined_scalar_leaf_is_still_the_tracked_instance(string form)
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(An_entity_projected_beside_a_declined_scalar_leaf_is_still_the_tracked_instance) + form,
            out var withMethods, out _);

        var tracked = db.IdentificationMethods.Single(m => m.AnimalId == withMethods && m.Rank == 2);
        var trackedId = tracked.Id;
        tracked.Rank = 99;
        var animal = db.Animals.Single(a => a.Id == withMethods);
        var query = db.Animals.Where(a => a.Id == withMethods);

        if (form == "RootBesideLeaf")
        {
            var row = query
                .Select(a => new { a, R = a.IdentificationMethods.Where(m => m.Id == trackedId).OrderBy(m => m.Id).Select(m => m.Rank.ToString()).First() })
                .Single();
            Assert.Equal("2", row.R);
            Assert.Same(animal, row.a);
        }
        else
        {
            var row = query
                .Select(a => new
                {
                    R = a.IdentificationMethods.Where(m => m.Id == trackedId).OrderBy(m => m.Id).Select(m => m.Rank.ToString()).First(),
                    E = a.IdentificationMethods.Where(m => m.Id == trackedId).OrderBy(m => m.Id).First()
                })
                .Single();
            Assert.Equal("2", row.R);
            Assert.Same(tracked, row.E);
            Assert.Equal(99, row.E.Rank);
        }
    }

    /// <summary>
    /// A selector that uses the element itself (returns it, keeps it beside a scalar, calls a method on it) runs over the element as a tracking query
    /// materializes it: the tracked instance, with its unsaved change. That is EF Core's identity resolution, which never overwrites a tracked instance
    /// with what the query read, so it is not refused. A query that does not track, or that resolves against its own state manager, reads the stored row.
    /// Whether tracking applies comes from the context's default or from the query. The value of a scalar read beside the element (<c>m.Method</c> in
    /// <c>new { m, m.Method }</c>) is not pinned for a tracking query: EF Core's other providers read it per leaf as a stored value, this one reads it from
    /// the tracked instance (see Query/AGENTS.md), and the test must not make either a rule.
    /// </summary>
    [Theory]
    [InlineData(QueryTrackingBehavior.TrackAll, "Default", true)]
    [InlineData(QueryTrackingBehavior.TrackAll, "AsTracking", true)]
    [InlineData(QueryTrackingBehavior.TrackAll, "AsNoTracking", false)]
    [InlineData(QueryTrackingBehavior.TrackAll, "AsNoTrackingWithIdentityResolution", false)]
    [InlineData(QueryTrackingBehavior.NoTracking, "Default", false)]
    [InlineData(QueryTrackingBehavior.NoTracking, "AsTracking", true)]
    [InlineData(QueryTrackingBehavior.NoTrackingWithIdentityResolution, "Default", false)]
    public void A_selector_that_uses_the_element_sees_the_tracked_instance_only_when_the_query_tracks(
        QueryTrackingBehavior defaultBehavior, string perQuery, bool expectTrackedInstance)
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly,
            nameof(A_selector_that_uses_the_element_sees_the_tracked_instance_only_when_the_query_tracks) + defaultBehavior + perQuery,
            out var withMethods, out _, tracking: defaultBehavior);

        var tracked = db.IdentificationMethods.AsTracking().Single(m => m.AnimalId == withMethods && m.Rank == 2);
        tracked.Method = "Unsaved";

        var animals = perQuery switch
        {
            "AsTracking" => db.Animals.AsTracking(),
            "AsNoTracking" => db.Animals.AsNoTracking(),
            "AsNoTrackingWithIdentityResolution" => db.Animals.AsNoTrackingWithIdentityResolution(),
            _ => db.Animals.AsQueryable()
        };
        var query = animals.Where(a => a.Id == withMethods);
        var expected = expectTrackedInstance ? "Unsaved" : "Tattoo";

        Assert.Equal(
            expected,
            query.Select(a => a.IdentificationMethods.Where(m => m.Rank == 2).Select(m => m.Describe()).ToList()).Single().Single());

        var element = query.Select(a => a.IdentificationMethods.Where(m => m.Rank == 2).Select(m => m).ToList()).Single().Single();
        Assert.Equal(expectTrackedInstance, ReferenceEquals(tracked, element));
        Assert.Equal(expected, element.Method);

        var beside = query.Select(a => a.IdentificationMethods.Where(m => m.Rank == 2).Select(m => new { m, m.Method }).ToList()).Single().Single();
        Assert.Equal(expectTrackedInstance, ReferenceEquals(tracked, beside.m));
        if (!expectTrackedInstance)
        {
            Assert.Equal("Tattoo", beside.Method);
        }

        Assert.Equal("Unsaved", tracked.Method);
    }

    /// <summary>
    /// A captured index is read per execution: <c>ElementAtOrDefault</c> of a negative one is the default (as in LINQ), while <c>ElementAt</c> of it
    /// throws. The same projection runs with index 0 first, so a cached plan cannot hide the difference.
    /// </summary>
    [Fact]
    public void A_negative_captured_ElementAt_index_is_default_for_OrDefault_and_throws_for_ElementAt()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(A_negative_captured_ElementAt_index_is_default_for_OrDefault_and_throws_for_ElementAt),
            out var withMethods, out _);

        string? OrDefault(int index)
            => db.Animals.Where(a => a.Id == withMethods)
                .Select(a => a.IdentificationMethods.OrderBy(m => m.Rank).ElementAtOrDefault(index)!.Method)
                .Single();

        string AtIndex(int index)
            => db.Animals.Where(a => a.Id == withMethods)
                .Select(a => a.IdentificationMethods.OrderBy(m => m.Rank).ElementAt(index).Method)
                .Single();

        Assert.Equal("Microchip", OrDefault(0));
        Assert.Null(OrDefault(-1));
        Assert.Equal("Tattoo", OrDefault(1));
        Assert.Null(OrDefault(2));

        Assert.Equal("Microchip", AtIndex(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => AtIndex(-1));
    }

    [Fact]
    public void ThenBy_and_Skip_in_the_navigation_chain_read_the_right_row()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(ThenBy_and_Skip_in_the_navigation_chain_read_the_right_row), out var withMethods, out _);

        var result = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new
            {
                Chained = a.IdentificationMethods.OrderBy(m => m.Rank).ThenBy(m => m.Method).FirstOrDefault()!.Method,
                Skipped = a.IdentificationMethods.OrderBy(m => m.Rank).Skip(1).FirstOrDefault()!.Method,
                PastTheEnd = a.IdentificationMethods.OrderBy(m => m.Rank).Skip(2).FirstOrDefault()!.Method
            })
            .Single();

        Assert.Equal("Microchip", result.Chained);
        Assert.Equal("Tattoo", result.Skipped);
        Assert.Null(result.PastTheEnd);
    }

    [Fact]
    public void A_count_beside_a_leaf_over_the_same_navigation_reads_both()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(A_count_beside_a_leaf_over_the_same_navigation_reads_both), out var withMethods, out _);

        var result = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new
            {
                N = a.IdentificationMethods.Count,
                M = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault()!.Method
            })
            .Single();

        Assert.Equal(2, result.N);
        Assert.Equal("Microchip", result.M);
    }

    [Fact]
    public void A_captured_predicate_value_is_read_per_execution()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(A_captured_predicate_value_is_read_per_execution), out var withMethods, out _);

        string? Run(int threshold)
            => db.Animals
                .Where(a => a.Id == withMethods)
                .Select(a => new
                {
                    M = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault(m => m.Rank > threshold)!.Method
                })
                .Single().M;

        Assert.Equal("Microchip", Run(0));
        Assert.Equal("Tattoo", Run(1));
        Assert.Null(Run(2));
    }

    /// <summary>
    /// A captured variable in the element selector (#119) is read from the query context on every execution, never baked into the cached shaper:
    /// the same query runs twice in one context with different values. Each form takes a different read path: fused into the streaming cursor
    /// (<c>Fused</c>), materialized untracked because the cursor declines the lambda (<c>Declined</c>), run over the materialized (and under
    /// <c>TrackAll</c> tracked) element (<c>Other</c>), under a single-result terminal (<c>Terminal</c>), and beside the root entity
    /// (<c>BesideRoot</c>). <c>DriverLinq</c> reads whole documents with the mixed shaper, the native modes with the streaming one.
    /// </summary>
    [Theory]
    [InlineData(MongoQueryMode.NativeOnly, QueryTrackingBehavior.TrackAll)]
    [InlineData(MongoQueryMode.NativeOnly, QueryTrackingBehavior.NoTracking)]
    [InlineData(MongoQueryMode.Native, QueryTrackingBehavior.TrackAll)]
    [InlineData(MongoQueryMode.DriverLinq, QueryTrackingBehavior.TrackAll)]
    [InlineData(MongoQueryMode.DriverLinq, QueryTrackingBehavior.NoTracking)]
    public void Captured_variable_in_selector_reads_per_execution(MongoQueryMode mode, QueryTrackingBehavior tracking)
    {
        using var db = CreateContext(
            mode, nameof(Captured_variable_in_selector_reads_per_execution) + mode + tracking, out var withMethods, out _, tracking: tracking);
        var query = db.Animals.Where(a => a.Id == withMethods);

        List<string> Fused(string suffix)
            => query.Select(a => a.IdentificationMethods.Where(m => m.Rank == 2).Select(m => m.Method + suffix).ToList()).Single();

        List<string> Declined(string suffix)
            => query.Select(a => a.IdentificationMethods.Where(m => m.Rank == 2).Select(m => m.Rank.ToString() + suffix).ToList()).Single();

        List<string> Other(string suffix)
            => query.Select(a => a.IdentificationMethods.Where(m => m.Rank == 2).Select(m => m.Describe() + suffix).ToList()).Single();

        string Terminal(string suffix)
            => query.Select(a => a.IdentificationMethods.OrderBy(m => m.Rank).Select(m => m.Method + suffix).First()).Single();

        string BesideRoot(string suffix)
        {
            var row = query.Select(a => new { a, L = a.IdentificationMethods.OrderBy(m => m.Rank).Select(m => m.Method + suffix).ToList() }).Single();
            return row.a.Name + ":" + string.Join(",", row.L);
        }

        Assert.Equal(["Tattoo!"], Fused("!"));
        Assert.Equal(["Tattoo?"], Fused("?"));
        Assert.Equal(["2!"], Declined("!"));
        Assert.Equal(["2?"], Declined("?"));
        Assert.Equal(["Tattoo!"], Other("!"));
        Assert.Equal(["Tattoo?"], Other("?"));
        Assert.Equal("Microchip!", Terminal("!"));
        Assert.Equal("Microchip?", Terminal("?"));
        Assert.Equal("Rex:Microchip!,Tattoo!", BesideRoot("!"));
        Assert.Equal("Rex:Microchip?,Tattoo?", BesideRoot("?"));
    }

    /// <summary>A captured value admitted in the selector admits nothing else: the outer entity beside it is still declined, naming the outer entity.</summary>
    [Fact]
    public void Captured_variable_beside_the_outer_entity_in_selector_is_still_declined()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(Captured_variable_beside_the_outer_entity_in_selector_is_still_declined), out var withMethods, out _);
        var suffix = "!";

        var exception = Assert.Throws<InvalidOperationException>(
            () => db.Animals.Where(a => a.Id == withMethods)
                .Select(a => a.IdentificationMethods.Where(m => m.Rank == 2).Select(m => m.Method + suffix + a.Name).ToList()).Single());
        Assert.Contains(MongoDB.EntityFrameworkCore.Query.NativeTranslation.ElementLeafDeclineReasons.OuterEntityInSelector, exception.Message);
    }

    [Fact]
    public void A_bare_selector_body_reads_the_leaf()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(A_bare_selector_body_reads_the_leaf), out var withMethods, out var withoutMethods);

        var methods = db.Animals
            .Where(a => a.Id == withMethods || a.Id == withoutMethods)
            .OrderBy(a => a.Name)
            .Select(a => a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault()!.Method)
            .ToList();

        // Fido (no methods) sorts before Rex.
        Assert.Equal(new string?[] { null, "Microchip" }, methods);
    }

    // An element leaf with only the correlation (no Where/OrderBy in the chain) as a member of a construction (#122).
    // An unsaved change to a tracked method row shows whether the leaf reads the stored values (R3) or the materialized element (R4).

    private static IdentificationMethod TrackAndChangeRank2(AnimalDbContext db, ObjectId animalId)
    {
        var tracked = db.IdentificationMethods.AsTracking().Single(m => m.AnimalId == animalId && m.Rank == 2);
        tracked.Method = "Unsaved";
        tracked.Rank = 20;
        return tracked;
    }

    [Theory]
    [InlineData(QueryTrackingBehavior.TrackAll)]
    [InlineData(QueryTrackingBehavior.NoTracking)]
    public void Correlation_only_leaf_beside_root_reads_stored_values(QueryTrackingBehavior tracking)
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(Correlation_only_leaf_beside_root_reads_stored_values) + tracking,
            out var withMethods, out var withoutMethods, tracking: tracking);
        var tracked = TrackAndChangeRank2(db, withMethods);

        var results = db.Animals
            .Where(a => a.Id == withMethods || a.Id == withoutMethods)
            .Select(a => new { a, M = a.IdentificationMethods.Select(m => m.Rank.ToString()).ToList() })
            .ToList();

        var rex = results.Single(r => r.a.Id == withMethods);
        var fido = results.Single(r => r.a.Id == withoutMethods);
        Assert.Equal("Rex", rex.a.Name);
        Assert.Equal(["1", "2"], rex.M.Order());
        Assert.Empty(fido.M);
        Assert.Equal(20, tracked.Rank);
    }

    [Theory]
    [InlineData(QueryTrackingBehavior.TrackAll)]
    [InlineData(QueryTrackingBehavior.NoTracking)]
    public void Correlation_only_leaf_beside_scalar(QueryTrackingBehavior tracking)
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(Correlation_only_leaf_beside_scalar) + tracking,
            out var withMethods, out var withoutMethods, tracking: tracking);
        var tracked = TrackAndChangeRank2(db, withMethods);
        var trackedBefore = db.ChangeTracker.Entries<IdentificationMethod>().Count();

        var results = db.Animals
            .Where(a => a.Id == withMethods || a.Id == withoutMethods)
            .Select(a => new { a.Name, M = a.IdentificationMethods.Select(m => m.Method).ToList() })
            .ToList();

        Assert.Equal(["Microchip", "Tattoo"], results.Single(r => r.Name == "Rex").M.Order());
        Assert.Empty(results.Single(r => r.Name == "Fido").M);
        Assert.Equal("Unsaved", tracked.Method);
        Assert.Equal(trackedBefore, db.ChangeTracker.Entries<IdentificationMethod>().Count());
    }

    [Theory]
    [InlineData(QueryTrackingBehavior.TrackAll)]
    [InlineData(QueryTrackingBehavior.NoTracking)]
    public void Correlation_only_entity_list_beside_root(QueryTrackingBehavior tracking)
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(Correlation_only_entity_list_beside_root) + tracking,
            out var withMethods, out var withoutMethods, tracking: tracking);
        var tracked = TrackAndChangeRank2(db, withMethods);

        var results = db.Animals
            .Where(a => a.Id == withMethods || a.Id == withoutMethods)
            .Select(a => new { a, M = a.IdentificationMethods.ToList() })
            .ToList();

        var rex = results.Single(r => r.a.Id == withMethods);
        Assert.Equal("Rex", rex.a.Name);
        Assert.Empty(results.Single(r => r.a.Id == withoutMethods).M);
        Assert.Equal(2, rex.M.Count);
        var row = rex.M.Single(m => m.Id == tracked.Id);
        if (tracking == QueryTrackingBehavior.TrackAll)
        {
            // R4: the element as materialized is the tracked instance, with its unsaved change.
            Assert.Same(tracked, row);
        }
        else
        {
            Assert.NotSame(tracked, row);
            Assert.Equal("Tattoo", row.Method);
            Assert.Equal(2, row.Rank);
        }
    }

    // No driver-LINQ oracle: expected values are hand-computed from the seed, with several candidates per case so a
    // wrong pick gives a different answer.

    private AnimalDbContext CreateEmptyContext(MongoQueryMode mode, string name, out string animals, out string methods)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        animals = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "A" + suffix;
        methods = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "M" + suffix;
        return new AnimalDbContext(database, animals, methods, mode, null);
    }

    [Fact]
    public void FirstOrDefault_with_constant_predicate_returns_the_matching_row_not_the_positional_first()
    {
        using var db = CreateEmptyContext(
            MongoQueryMode.NativeOnly,
            nameof(FirstOrDefault_with_constant_predicate_returns_the_matching_row_not_the_positional_first),
            out var animals, out var methods);

        var animalId = ObjectId.GenerateNewId();
        database.MongoDatabase.GetCollection<Animal>(animals).InsertOne(new Animal { Id = animalId, Name = "Rex" });
        database.MongoDatabase.GetCollection<IdentificationMethod>(methods).InsertMany(
        [
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "Microchip", Rank = 1, AnimalId = animalId },
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "Tattoo", Rank = 2, AnimalId = animalId },
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "EarTag", Rank = 3, AnimalId = animalId },
        ]);

        var result = db.Animals
            .Where(a => a.Id == animalId)
            .Select(a => new { a.Id, M = a.IdentificationMethods.FirstOrDefault(m => m.Method == "Tattoo")!.Method })
            .Single();

        // "Microchip" is the positional-first row; a broken predicate (or one silently ignored) would return
        // it instead of "Tattoo", which is the actual match.
        Assert.Equal("Tattoo", result.M);
    }

    [Fact]
    public void OrderBy_and_OrderByDescending_FirstOrDefault_pick_opposite_rows()
    {
        using var db = CreateEmptyContext(
            MongoQueryMode.NativeOnly,
            nameof(OrderBy_and_OrderByDescending_FirstOrDefault_pick_opposite_rows),
            out var animals, out var methods);

        var animalId = ObjectId.GenerateNewId();
        database.MongoDatabase.GetCollection<Animal>(animals).InsertOne(new Animal { Id = animalId, Name = "Rex" });
        // Inserted out of rank order so a naive "positional first" read would be wrong either way.
        database.MongoDatabase.GetCollection<IdentificationMethod>(methods).InsertMany(
        [
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "Tattoo", Rank = 2, AnimalId = animalId },
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "Microchip", Rank = 3, AnimalId = animalId },
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "EarTag", Rank = 1, AnimalId = animalId },
        ]);

        var ascending = db.Animals
            .Where(a => a.Id == animalId)
            .Select(a => new { a.Id, M = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault()!.Method })
            .Single();

        var descending = db.Animals
            .Where(a => a.Id == animalId)
            .Select(a => new { a.Id, M = a.IdentificationMethods.OrderByDescending(m => m.Rank).FirstOrDefault()!.Method })
            .Single();

        Assert.Equal("EarTag", ascending.M);   // Rank 1: lowest
        Assert.Equal("Microchip", descending.M); // Rank 3: highest
    }

    [Fact]
    public void OrderBy_combined_with_predicate_excludes_the_row_that_would_otherwise_sort_first()
    {
        using var db = CreateEmptyContext(
            MongoQueryMode.NativeOnly,
            nameof(OrderBy_combined_with_predicate_excludes_the_row_that_would_otherwise_sort_first),
            out var animals, out var methods);

        var animalId = ObjectId.GenerateNewId();
        database.MongoDatabase.GetCollection<Animal>(animals).InsertOne(new Animal { Id = animalId, Name = "Rex" });
        database.MongoDatabase.GetCollection<IdentificationMethod>(methods).InsertMany(
        [
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "Microchip", Rank = 1, AnimalId = animalId },
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "Tattoo", Rank = 2, AnimalId = animalId },
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "EarTag", Rank = 3, AnimalId = animalId },
        ]);

        // Without the predicate, OrderBy(Rank) would pick "Microchip" (Rank 1); with it, the answer is "Tattoo"
        // (Rank 2), not "EarTag".
        var result = db.Animals
            .Where(a => a.Id == animalId)
            .Select(a => new
            {
                a.Id,
                M = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault(m => m.Method != "Microchip")!.Method
            })
            .Single();

        Assert.Equal("Tattoo", result.M);
    }

    [Fact]
    public void Each_outer_document_picks_its_own_first_row_not_a_global_one()
    {
        using var db = CreateEmptyContext(
            MongoQueryMode.NativeOnly,
            nameof(Each_outer_document_picks_its_own_first_row_not_a_global_one),
            out var animals, out var methods);

        var animal1 = ObjectId.GenerateNewId();
        var animal2 = ObjectId.GenerateNewId();
        database.MongoDatabase.GetCollection<Animal>(animals).InsertMany(
        [
            new Animal { Id = animal1, Name = "Rex" },
            new Animal { Id = animal2, Name = "Fido" },
        ]);
        database.MongoDatabase.GetCollection<IdentificationMethod>(methods).InsertMany(
        [
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "Microchip", Rank = 1, AnimalId = animal1 },
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "Tattoo", Rank = 2, AnimalId = animal1 },
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "EarTag", Rank = 1, AnimalId = animal2 },
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "Collar", Rank = 2, AnimalId = animal2 },
        ]);

        var results = db.Animals
            .OrderBy(a => a.Name)
            .Select(a => new { a.Id, M = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault()!.Method })
            .ToList();

        var rex = results.Single(r => r.Id == animal1);
        var fido = results.Single(r => r.Id == animal2);

        // A correlation leak across documents (global rather than per-document first) gives both the same Method.
        Assert.Equal("Microchip", rex.M);
        Assert.Equal("EarTag", fido.M);
    }

    [Fact]
    public async Task Correlated_reducer_projection_with_predicate_and_ordering_succeeds_under_NativeOnly()
    {
        using var db = CreateEmptyContext(
            MongoQueryMode.NativeOnly,
            nameof(Correlated_reducer_projection_with_predicate_and_ordering_succeeds_under_NativeOnly),
            out var animals, out var methods);

        var animalId = ObjectId.GenerateNewId();
        database.MongoDatabase.GetCollection<Animal>(animals).InsertOne(new Animal { Id = animalId, Name = "Rex" });
        database.MongoDatabase.GetCollection<IdentificationMethod>(methods).InsertMany(
        [
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "Microchip", Rank = 1, AnimalId = animalId },
            new IdentificationMethod { Id = ObjectId.GenerateNewId(), Method = "Tattoo", Rank = 2, AnimalId = animalId },
        ]);

        var result = await db.Animals
            .Where(a => a.Id == animalId)
            .Select(a => new
            {
                a.Id,
                M = a.IdentificationMethods.OrderByDescending(m => m.Rank).FirstOrDefault(m => m.Method == "Tattoo")!.Method
            })
            .SingleAsync();

        Assert.Equal("Tattoo", result.M);
    }

    //
    // EF's nav-expansion widens the member to Nullable<T> inside the inner Select and converts back to T at the
    // end, so the recognizer sees a Convert leaf rather than a MethodCallExpression. NativeOnly throughout.

    [Fact]
    public void FirstOrDefault_over_a_non_nullable_int_member_returns_the_correct_value()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(FirstOrDefault_over_a_non_nullable_int_member_returns_the_correct_value),
            out var withMethods, out _);

        var result = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new { a.Id, R = a.IdentificationMethods.OrderByDescending(m => m.Rank).FirstOrDefault()!.Rank })
            .Single();

        // 2 (Tattoo) is the highest rank; 1 (Microchip) is both the lowest and the positional first, so a
        // dropped $sort or a mis-resolved member would produce a different value.
        Assert.Equal(2, result.R);
    }

    /// <summary>
    /// Empty collection: <c>FirstOrDefault()</c> must yield <c>default(T)</c>, not throw. The generic alias read throws on a
    /// missing non-nullable field; <c>IsDefaultOnEmptyCorrelatedReducerLeaf</c> emits the absent-or-null default conditional.
    /// </summary>
    [Fact]
    public void FirstOrDefault_over_a_non_nullable_int_member_reads_as_default_for_an_empty_collection()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly,
            nameof(FirstOrDefault_over_a_non_nullable_int_member_reads_as_default_for_an_empty_collection),
            out _, out var withoutMethods);

        var result = db.Animals
            .Where(a => a.Id == withoutMethods)
            .Select(a => new { a.Id, R = a.IdentificationMethods.FirstOrDefault()!.Rank })
            .Single();

        Assert.Equal(0, result.R);
    }

    [Fact]
    public void FirstOrDefault_over_an_enum_member_returns_the_correct_value()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(FirstOrDefault_over_an_enum_member_returns_the_correct_value),
            out var withMethods, out _);

        var result = db.Animals
            .Where(a => a.Id == withMethods)
            .Select(a => new { a.Id, K = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault()!.Kind })
            .Single();

        // Microchip (Rank 1) carries Implant; Tattoo (Rank 2) carries Visual — so picking the wrong row, or
        // reading the enum through the wrong representation, gives a different, detectably wrong answer.
        Assert.Equal(IdentificationKind.Implant, result.K);
    }

    [Fact]
    public void FirstOrDefault_over_an_enum_member_reads_as_default_for_an_empty_collection()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly,
            nameof(FirstOrDefault_over_an_enum_member_reads_as_default_for_an_empty_collection),
            out _, out var withoutMethods);

        var result = db.Animals
            .Where(a => a.Id == withoutMethods)
            .Select(a => new { a.Id, K = a.IdentificationMethods.FirstOrDefault()!.Kind })
            .Single();

        Assert.Equal(IdentificationKind.Unknown, result.K);
    }

    /// <summary>
    /// Populated and empty rows in one query: a per-document correlation failure would make the empty row read
    /// the populated row's value.
    /// </summary>
    [Fact]
    public async Task FirstOrDefault_over_a_value_type_member_mixes_populated_and_empty_rows_in_one_query()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly,
            nameof(FirstOrDefault_over_a_value_type_member_mixes_populated_and_empty_rows_in_one_query),
            out var withMethods, out var withoutMethods);

        // One leaf per query, deliberately: two reducer leaves over the SAME navigation collide on the
        // `_lookup_IdentificationMethods` alias and are declined by design (see
        // NativeCorrelatedReducerLeafTests.Two_leaves_over_the_same_navigation_decline).
        var ranks = await db.Animals
            .Select(a => new { a.Id, R = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault()!.Rank })
            .ToListAsync();
        var kinds = await db.Animals
            .Select(a => new { a.Id, K = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault()!.Kind })
            .ToListAsync();

        Assert.Equal(1, ranks.Single(r => r.Id == withMethods).R);
        Assert.Equal(0, ranks.Single(r => r.Id == withoutMethods).R);
        Assert.Equal(IdentificationKind.Implant, kinds.Single(r => r.Id == withMethods).K);
        Assert.Equal(IdentificationKind.Unknown, kinds.Single(r => r.Id == withoutMethods).K);
    }

    /// <summary>
    /// Widened shape with <c>OrderBy</c> and a constant predicate; nests like the unwrapped shape, so the chain
    /// walk handles it once the two <c>Convert</c>s are peeled.
    /// </summary>
    [Fact]
    public void FirstOrDefault_over_a_value_type_member_with_predicate_and_ordering_returns_the_correct_value()
    {
        using var db = CreateEmptyContext(
            MongoQueryMode.NativeOnly,
            nameof(FirstOrDefault_over_a_value_type_member_with_predicate_and_ordering_returns_the_correct_value),
            out var animals, out var methods);

        var animalId = ObjectId.GenerateNewId();
        database.MongoDatabase.GetCollection<Animal>(animals).InsertOne(new Animal { Id = animalId, Name = "Rex" });
        database.MongoDatabase.GetCollection<IdentificationMethod>(methods).InsertMany(
        [
            new IdentificationMethod
            {
                Id = ObjectId.GenerateNewId(), Method = "Microchip", Rank = 1, Kind = IdentificationKind.Implant,
                AnimalId = animalId
            },
            new IdentificationMethod
            {
                Id = ObjectId.GenerateNewId(), Method = "Tattoo", Rank = 2, Kind = IdentificationKind.Visual,
                AnimalId = animalId
            },
            new IdentificationMethod
            {
                Id = ObjectId.GenerateNewId(), Method = "EarTag", Rank = 3, Kind = IdentificationKind.Visual,
                AnimalId = animalId
            },
        ]);

        var result = db.Animals
            .Where(a => a.Id == animalId)
            .Select(a => new
            {
                a.Id,
                R = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault(m => m.Rank > 1)!.Rank
            })
            .Single();

        // Rank 1 sorts first but is excluded by the predicate; Rank 3 is the wrong end of the sort. Only the
        // correct predicate+sort combination yields 2.
        Assert.Equal(2, result.R);
    }

    // `new { a, M = a.Nav.FirstOrDefault().Member }` is admitted. The leaves read from different places ($$ROOT vs
    // `_lookup_<Nav>.<Member>`), so one shadowing the other shows up as a wrong Name/Rank.

    /// <summary>
    /// Reducer leaf beside a whole-entity <c>$$ROOT</c> leaf, over the widened (non-nullable member) shape.
    /// No driver-LINQ fallback exists, so a decline would be a hard failure.
    /// </summary>
    [Fact]
    public void Whole_root_entity_leaf_beside_a_reducer_leaf_reads_both_correctly()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly, nameof(Whole_root_entity_leaf_beside_a_reducer_leaf_reads_both_correctly),
            out var withMethods, out var withoutMethods);

        var results = db.Animals
            .Select(a => new { a, R = a.IdentificationMethods.OrderByDescending(m => m.Rank).FirstOrDefault()!.Rank })
            .ToList();

        var rex = results.Single(r => r.a.Id == withMethods);
        var fido = results.Single(r => r.a.Id == withoutMethods);

        // The whole-entity leaf materialized its own stored scalars off $$ROOT ...
        Assert.Equal("Rex", rex.a.Name);
        Assert.Equal("Fido", fido.a.Name);
        // ... and the reducer leaf read the correct per-document row (Tattoo, Rank 2, is the highest-ranked of
        // Rex's two methods; Fido has none, so FirstOrDefault's contract is default(int)).
        Assert.Equal(2, rex.R);
        Assert.Equal(0, fido.R);
    }

    /// <summary>
    /// Same pairing over the non-widened shape (<c>string</c> member, no <c>Convert</c> peel), which reaches the
    /// recognizer by a different path.
    /// </summary>
    [Fact]
    public async Task Whole_root_entity_leaf_beside_a_string_reducer_leaf_reads_both_correctly()
    {
        using var db = CreateContext(
            MongoQueryMode.NativeOnly,
            nameof(Whole_root_entity_leaf_beside_a_string_reducer_leaf_reads_both_correctly),
            out var withMethods, out var withoutMethods);

        var results = await db.Animals
            .Select(a => new { a, M = a.IdentificationMethods.OrderBy(m => m.Rank).FirstOrDefault()!.Method })
            .ToListAsync();

        var rex = results.Single(r => r.a.Id == withMethods);
        var fido = results.Single(r => r.a.Id == withoutMethods);

        Assert.Equal("Rex", rex.a.Name);
        Assert.Equal("Microchip", rex.M);
        Assert.Equal("Fido", fido.a.Name);
        Assert.Null(fido.M);
    }

    private class AnimalDbContext : DbContext
    {
        private readonly string _animals;
        private readonly string _methods;

        public AnimalDbContext(
            TemporaryDatabaseFixture database, string animals, string methods, MongoQueryMode mode,
            ILoggerFactory? loggerFactory, QueryTrackingBehavior? tracking = null)
            : base(Configure(database, mode, loggerFactory, tracking))
        {
            _animals = animals;
            _methods = methods;
        }

        private static DbContextOptions<AnimalDbContext> Configure(
            TemporaryDatabaseFixture database, MongoQueryMode mode, ILoggerFactory? loggerFactory, QueryTrackingBehavior? tracking)
        {
            var builder = new DbContextOptionsBuilder<AnimalDbContext>()
                .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName,
                    b => b.UseQueryMode(mode))
                .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

            if (loggerFactory != null)
            {
                builder = builder.UseLoggerFactory(loggerFactory).EnableSensitiveDataLogging();
            }

            if (tracking is { } behavior)
            {
                builder = builder.UseQueryTrackingBehavior(behavior);
            }

            return builder.Options;
        }

        public DbSet<Animal> Animals { get; set; } = null!;
        public DbSet<IdentificationMethod> IdentificationMethods { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Animal>().ToCollection(_animals);
            modelBuilder.Entity<Animal>().HasMany(a => a.IdentificationMethods).WithOne(m => m.Animal)
                .HasForeignKey(m => m.AnimalId);
            modelBuilder.Entity<IdentificationMethod>().ToCollection(_methods);
        }

        private sealed class IgnoreCacheKeyFactory : IModelCacheKeyFactory
        {
            private static int _count;
            public object Create(DbContext context, bool designTime) => Interlocked.Increment(ref _count);
        }
    }
}
