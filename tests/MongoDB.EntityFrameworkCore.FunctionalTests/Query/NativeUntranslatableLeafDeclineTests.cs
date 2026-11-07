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
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

#nullable enable

/// <summary>
/// Leaf spellings over an owned-then-complex chain (owned <c>Home</c> holding a <c>[ComplexType]</c> struct
/// <c>Spot</c>). Each runs NativeOnly + Native + DriverLinq against a hand-written answer, or, where the projection
/// binder can't bind a leaf (<c>MongoProjectionBindingExpressionVisitor.TranslationFailed</c>), must DECLINE (NativeOnly
/// throws, Native falls back with the right rows): a binder failure used to become a <c>default(T)</c> read, i.e. silent
/// null rows.
/// </summary>
[XUnitCollection("QueryTests")]
public class NativeUntranslatableLeafDeclineTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    // [ComplexType] only applies to classes and OwnedNavigationBuilder has no ComplexProperty overload, so a STRUCT
    // complex hop inside an owned hop is not modelable; the class hop exercises the same binder arms.
    [ComplexType]
    public class Spot
    {
        public double Lat { get; set; }
        public string Label { get; set; } = "";
    }

    public class Home
    {
        public string City { get; set; } = "";
        public Spot Spot { get; set; } = null!;
    }

    public class House
    {
        public ObjectId Id { get; set; }
        public string Title { get; set; } = "";
        public Home Home { get; set; } = null!;
    }

    private IMongoCollection<House> Seed(string name)
    {
        var raw = database.MongoDatabase.GetCollection<BsonDocument>(
            TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8]);
        raw.InsertMany(
        [
            Doc("a", "Bristol", 1.5, "x"),
            Doc("b", "Cardiff", 2.5, "y")
        ]);
        return database.MongoDatabase.GetCollection<House>(raw.CollectionNamespace.CollectionName);

        static BsonDocument Doc(string title, string city, double lat, string label)
            => new()
            {
                { "_id", ObjectId.GenerateNewId() }, { "Title", title },
                { "Home", new BsonDocument { { "City", city }, { "Spot", new BsonDocument { { "Lat", lat }, { "Label", label } } } } }
            };
    }

    private static List<T> Run<T>(IMongoCollection<House> collection, MongoQueryMode mode, Func<IQueryable<House>, IEnumerable<T>> query)
    {
        using var db = SingleEntityDbContext.Create(collection, mb => mb.Entity<House>().OwnsOne(h => h.Home), optionsBuilderAction: b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });
        return query(db.Entities.AsNoTracking()).ToList();
    }

    public static readonly Dictionary<string, (Func<IQueryable<House>, IEnumerable<string>> Run, string[] Expected)> Shapes = new()
    {
        ["member_chain"] = (q => q.OrderBy(h => h.Title).Select(h => new { V = h.Home.Spot.Lat }).ToList().Select(x => x.V.ToString()),
            ["1.5", "2.5"]),
        ["efprop_complex_hop_in_owned_hop"] = (q => q.OrderBy(h => h.Title)
            .Select(h => new { V = EF.Property<double>(h.Home.Spot, "Lat") }).ToList().Select(x => x.V.ToString()), ["1.5", "2.5"]),
        ["efprop_nested_owned_then_complex"] = (q => q.OrderBy(h => h.Title)
            .Select(h => new { V = EF.Property<string>(EF.Property<Spot>(EF.Property<Home>(h, "Home"), "Spot"), "Label") }).ToList().Select(x => x.V),
            ["x", "y"]),
        ["bare_efprop_nested_owned_then_complex"] = (q => q.OrderBy(h => h.Title)
            .Select(h => EF.Property<double>(EF.Property<Spot>(EF.Property<Home>(h, "Home"), "Spot"), "Lat")).ToList().Select(x => x.ToString()),
            ["1.5", "2.5"]),
        ["efprop_value_member"] = (q => q.OrderBy(h => h.Title).Select(h => new { V = EF.Property<Spot>(h.Home, "Spot").Label }).ToList().Select(x => x.V),
            ["x", "y"]),
        ["efprop_beside_owned_scalar"] = (q => q.OrderBy(h => h.Title)
            .Select(h => new { h.Home.City, L = EF.Property<string>(EF.Property<Spot>(EF.Property<Home>(h, "Home"), "Spot"), "Label") }).ToList()
            .Select(x => x.City + ":" + x.L), ["Bristol:x", "Cardiff:y"]),
    };

    public static TheoryData<string> ShapeNames => [.. Shapes.Keys];

    [Theory]
    [MemberData(nameof(ShapeNames))]
    public void Owned_then_complex_leaf_spelling_reads_the_leaf(string shape)
    {
        // Never null rows: if the binder can't bind a spelling, TranslationFailed declines it (pinned by mutation of the
        // whole-leaf arm: these rows then decline under NativeOnly instead of reading default).
        var collection = Seed(nameof(Owned_then_complex_leaf_spelling_reads_the_leaf) + shape);
        var (run, expected) = Shapes[shape];
        NativeModeAssert.NativeAndExpected(m => Run(collection, m, run), [.. expected]);
    }

    [Fact]
    public void Narrowing_cast_over_an_EF_Property_chain_fails_loudly_in_every_mode()
    {
        // `(float)EF.Property<double>(...)`: natively a narrowing cast declines; driver-LINQ then refuses the expression.
        // Before the fix the binder threw ArgumentNullException (a null operand under Convert). Loud, never null rows.
        var collection = Seed(nameof(Narrowing_cast_over_an_EF_Property_chain_fails_loudly_in_every_mode));
        Func<IQueryable<House>, IEnumerable<float>> run = q => q.OrderBy(h => h.Title)
            .Select(h => new { V = (float)EF.Property<double>(EF.Property<Spot>(EF.Property<Home>(h, "Home"), "Spot"), "Lat") }).ToList()
            .Select(x => x.V);
        Assert.Throws<MongoDB.EntityFrameworkCore.Query.NativeTranslation.NativeTranslationNotSupportedException>(
            () => Run(collection, MongoQueryMode.NativeOnly, run));
        foreach (var mode in new[] { MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            var ex = Assert.ThrowsAny<Exception>(() => Run(collection, mode, run));
            Assert.Contains("Expression not supported", ex.Message);
        }
    }
    [Fact]
    public void EF_Property_of_an_unmapped_member_declines_and_falls_back_to_driver_linq()
    {
        // Unbindable spellings (an unknown member as the leaf or as the hop): the native path declines rather than
        // reading default; driver-LINQ answers null for the missing element.
        var collection = Seed(nameof(EF_Property_of_an_unmapped_member_declines_and_falls_back_to_driver_linq));
        Assert.Equal(["<null>", "<null>"], NativeModeAssert.DeclinesCleanly(m => Run(collection, m,
            q => q.OrderBy(h => h.Title).Select(h => new { V = EF.Property<string>(h.Home, "Nope") }).ToList().Select(x => x.V ?? "<null>"))));
        Assert.Equal(["<null>", "<null>"], NativeModeAssert.DeclinesCleanly(m => Run(collection, m,
            q => q.OrderBy(h => h.Title).Select(h => new { V = EF.Property<string>(EF.Property<Home>(h, "Nope"), "City") }).ToList()
                .Select(x => x.V ?? "<null>"))));
    }
}
