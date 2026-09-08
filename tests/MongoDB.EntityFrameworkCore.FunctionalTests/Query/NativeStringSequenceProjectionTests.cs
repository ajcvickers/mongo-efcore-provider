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

using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// `e.City.AsEnumerable()`/`.ToList()`/`.ToArray()` — treating a string as its own <c>IEnumerable&lt;char&gt;</c>
/// (EF's own AsEnumerable_over_string/ToList_over_string/ToArray_over_string conformance shapes) — used to
/// decline the WHOLE projecting Select outright. It needs no server-side computation: the wrapping call only
/// changes the leaf's CLR materialization, never the stored value, so
/// <c>NativeProjectionBinder.TryTranslateLeaf</c> now admits it as a bare field leaf (pushing down only the raw
/// string) and the read side re-applies the original .NET call to the raw value it reads back — see
/// <c>MongoProjectionBindingExpressionVisitor.Visit</c> and
/// <c>MongoProjectionBindingRemovingExpressionVisitor</c>.
/// </summary>
[XUnitCollection("QueryTests")]
public class NativeStringSequenceProjectionTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Row
    {
        public ObjectId Id { get; set; }
        public string Label { get; set; } = "";
        public string City { get; set; } = "";
    }

    private static readonly (string Label, string City)[] Rows =
    [
        ("a", "London"),
        ("b", "Berlin")
    ];

    [Fact]
    public void Wrapped_AsEnumerable_over_string_projection_goes_native()
    {
        var collection = Seed(nameof(Wrapped_AsEnumerable_over_string_projection_goes_native));

        using var nativeOnly = CreateContext(collection, MongoQueryMode.NativeOnly);
        var nativeOnlyResult = nativeOnly.Entities.AsNoTracking().OrderBy(x => x.Label)
            .Select(x => new { x.Label, Property = x.City.AsEnumerable() }).ToList();
        Assert.Equal(
            new[] { ("a", "London"), ("b", "Berlin") },
            nativeOnlyResult.Select(r => (r.Label, new string(r.Property.ToArray()))));

        using var native = CreateContext(collection, MongoQueryMode.Native);
        var nativeResult = native.Entities.AsNoTracking().OrderBy(x => x.Label)
            .Select(x => new { x.Label, Property = x.City.AsEnumerable() }).ToList();
        Assert.Equal(
            nativeOnlyResult.Select(r => (r.Label, new string(r.Property.ToArray()))),
            nativeResult.Select(r => (r.Label, new string(r.Property.ToArray()))));

        // Deliberately no DriverLinq leg here: the MongoDB driver's own LINQ v3 provider cannot derive a
        // serializer for an anonymous-type projection containing a bare IEnumerable<char> produced by
        // AsEnumerable() over a string — a pre-existing, standalone driver limitation, unrelated to this
        // provider's native/fallback logic (the same family Query/AGENTS.md already documents as having "no
        // driver-LINQ oracle at all": SelectMany over a reference collection, Intersect/Except, the
        // correlated-reducer leaf, Reverse). ToList()/ToArray() don't hit this — they return concrete
        // List<char>/char[], which the driver CAN serialize.
    }

    [Fact]
    public void Wrapped_ToList_over_string_projection_goes_native()
    {
        var collection = Seed(nameof(Wrapped_ToList_over_string_projection_goes_native));

        using var nativeOnly = CreateContext(collection, MongoQueryMode.NativeOnly);
        var result = nativeOnly.Entities.AsNoTracking().OrderBy(x => x.Label)
            .Select(x => new { x.Label, Property = x.City.ToList() }).ToList();
        Assert.Equal(
            new[] { ("a", "London"), ("b", "Berlin") },
            result.Select(r => (r.Label, new string(r.Property.ToArray()))));
    }

    [Fact]
    public void Wrapped_ToArray_over_string_projection_goes_native()
    {
        var collection = Seed(nameof(Wrapped_ToArray_over_string_projection_goes_native));

        using var nativeOnly = CreateContext(collection, MongoQueryMode.NativeOnly);
        var result = nativeOnly.Entities.AsNoTracking().OrderBy(x => x.Label)
            .Select(x => new { x.Label, Property = x.City.ToArray() }).ToList();
        Assert.Equal(
            new[] { ("a", "London"), ("b", "Berlin") },
            result.Select(r => (r.Label, new string(r.Property))));
    }

    [Fact]
    public void Bare_AsEnumerable_over_string_projection_goes_native()
    {
        var collection = Seed(nameof(Bare_AsEnumerable_over_string_projection_goes_native));

        using var nativeOnly = CreateContext(collection, MongoQueryMode.NativeOnly);
        var result = nativeOnly.Entities.AsNoTracking().OrderBy(x => x.Label)
            .Select(x => x.City.AsEnumerable()).ToList();
        Assert.Equal(new[] { "London", "Berlin" }, result.Select(r => new string(r.ToArray())));
    }

    private IMongoCollection<Row> Seed(string name)
    {
        var collection = database.CreateCollection<Row>(name);
        collection.InsertMany(Rows.Select(r => new Row { Label = r.Label, City = r.City }));
        return collection;
    }

    private static SingleEntityDbContext<Row> CreateContext(IMongoCollection<Row> collection, MongoQueryMode mode)
        => SingleEntityDbContext.Create(
            collection,
            optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });
}
