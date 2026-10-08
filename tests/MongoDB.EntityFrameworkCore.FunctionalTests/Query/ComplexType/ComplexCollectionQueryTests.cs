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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// Quantifiers (<c>Any</c>/<c>All</c>/<c>Count</c>) over complex collections translate natively to
/// <c>$elemMatch</c> / quantifier / <c>$size</c> shapes; whole-entity reads materialize the collection from its
/// stored array. Ragged states are seeded as raw <c>BsonDocument</c>s (the ragged-seed rule). Row counts alone
/// cannot discriminate — the emitted MQL is asserted alongside results (the EF-322 lesson). EF10-only: complex
/// collections cannot be modeled on EF8/EF9.
/// </summary>
#if EF10
[XUnitCollection("QueryTests")]
public class ComplexCollectionQueryTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    private class Order
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public List<Line> Lines { get; set; } = [];
    }

    private class Line
    {
        public string Sku { get; set; } = "";
        public int Quantity { get; set; }
    }

    private static readonly Action<ModelBuilder> OrderModel = mb =>
        mb.Entity<Order>().ComplexCollection(o => o.Lines);

    private static SingleEntityDbContext<T> CreateContext<T>(
        IMongoCollection<T> collection, MongoQueryMode mode, Action<ModelBuilder>? model = null)
        where T : class
        => SingleEntityDbContext.Create(
            collection,
            modelBuilderAction: model,
            optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });

    private SingleEntityDbContext<T> CreateContextWithLogging<T>(
        IMongoCollection<T> collection, MongoQueryMode mode, Action<ModelBuilder>? model, out SpyLoggerProvider spyLogger)
        where T : class
    {
        var (loggerFactory, provider) = SpyLoggerProvider.Create();
        spyLogger = provider;

        return SingleEntityDbContext.Create(
            collection,
            loggerFactory,
            modelBuilderAction: model,
            optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                b.EnableSensitiveDataLogging();
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });
    }

    // Ragged seed: one matching row (a "Widget" line with Quantity 10), one non-matching (no Widget),
    // one with an empty array, one with the element missing entirely, and one with an explicit BSON null.
    private void SeedOrders(IMongoCollection<BsonDocument> raw)
    {
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Name", "match" },
            { "Lines", new BsonArray
                {
                    new BsonDocument { { "Sku", "Widget" }, { "Quantity", 10 } },
                    new BsonDocument { { "Sku", "Gadget" }, { "Quantity", 2 } }
                }
            }
        });
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Name", "no-match" },
            { "Lines", new BsonArray
                {
                    new BsonDocument { { "Sku", "Gadget" }, { "Quantity", 2 } }
                }
            }
        });
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", "empty" }, { "Lines", new BsonArray() }
        });
        raw.InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "missing" } });
        raw.InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "null" }, { "Lines", BsonNull.Value } });
    }

    [Fact]
    public void Any_over_complex_collection_translates_to_elemMatch()
    {
        var collection = database.CreateCollection<Order>();
        SeedOrders(database.GetCollection<BsonDocument>(collection.CollectionNamespace));

        using var context = CreateContextWithLogging(collection, MongoQueryMode.NativeOnly, OrderModel, out var spy);

        var matched = context.Entities.Where(o => o.Lines.Any(l => l.Sku == "Widget")).ToList();

        var mql = spy.GetLogMessageByEventId(MongoEventId.ExecutedMqlQuery);
        Assert.Contains("$elemMatch", mql, StringComparison.Ordinal);
        Assert.Contains("Lines", mql, StringComparison.Ordinal);
        Assert.DoesNotContain("$expr", mql, StringComparison.Ordinal);
        Assert.Single(matched);
        Assert.Equal("match", matched[0].Name);
        Assert.Equal(10, matched[0].Lines.Single(l => l.Sku == "Widget").Quantity);
    }

    [Fact]
    public void Filter_on_element_leaf_matches_only_documents_with_matching_element()
    {
        var collection = database.CreateCollection<Order>(values: ["qty"]);
        SeedOrders(database.GetCollection<BsonDocument>(collection.CollectionNamespace));

        using var context = CreateContextWithLogging(collection, MongoQueryMode.NativeOnly, OrderModel, out var spy);

        var matched = context.Entities.Where(o => o.Lines.Any(l => l.Quantity > 5)).ToList();

        var mql = spy.GetLogMessageByEventId(MongoEventId.ExecutedMqlQuery);
        Assert.Contains("$elemMatch", mql, StringComparison.Ordinal);
        Assert.Contains("Quantity", mql, StringComparison.Ordinal);
        Assert.DoesNotContain("$expr", mql, StringComparison.Ordinal);
        // Only the "match" row has an element with Quantity > 5.
        Assert.Single(matched);
        Assert.Equal("match", matched[0].Name);
    }

    [Fact]
    public void All_over_complex_collection_translates_to_negated_elemMatch()
    {
        var collection = database.CreateCollection<Order>(values: ["all"]);
        SeedOrders(database.GetCollection<BsonDocument>(collection.CollectionNamespace));

        using var context = CreateContextWithLogging(collection, MongoQueryMode.NativeOnly, OrderModel, out var spy);

        // All(Quantity > 0): true for match, no-match, empty, missing and null (All over an empty/absent array
        // is vacuously true, matching LINQ).
        var matched = context.Entities.Where(o => o.Lines.All(l => l.Quantity > 0)).ToList();

        var mql = spy.GetLogMessageByEventId(MongoEventId.ExecutedMqlQuery);
        Assert.Contains("$elemMatch", mql, StringComparison.Ordinal);
        Assert.DoesNotContain("$expr", mql, StringComparison.Ordinal);
        Assert.Equal(5, matched.Count);
    }

    [Fact]
    public void Count_with_predicate_over_complex_collection_translates_natively()
    {
        var collection = database.CreateCollection<Order>(values: ["count"]);
        SeedOrders(database.GetCollection<BsonDocument>(collection.CollectionNamespace));

        using var context = CreateContextWithLogging(collection, MongoQueryMode.NativeOnly, OrderModel, out var spy);

        var matched = context.Entities.Where(o => o.Lines.Count(l => l.Sku == "Widget") > 0).ToList();

        var mql = spy.GetLogMessageByEventId(MongoEventId.ExecutedMqlQuery);
        // Count over a complex collection rides the $size+$filter dialect, distinct from $elemMatch.
        Assert.Contains("$size", mql);
        Assert.DoesNotContain("$elemMatch", mql);
        Assert.Single(matched);
        Assert.Equal("match", matched[0].Name);
    }

    [Fact]
    public void Whole_entity_read_materializes_complex_collection()
    {
        var collection = database.CreateCollection<Order>(values: ["materialize"]);
        SeedOrders(database.GetCollection<BsonDocument>(collection.CollectionNamespace));

        // Streaming shaper (ToList).
        using var nativeOnly = CreateContext(collection, MongoQueryMode.NativeOnly, OrderModel);
        var orders = nativeOnly.Entities.ToList();

        Assert.Equal(5, orders.Count);
        var match = orders.Single(o => o.Name == "match");
        Assert.Equal(2, match.Lines.Count);
        Assert.Equal("Widget", match.Lines[0].Sku);
        Assert.Equal(10, match.Lines[0].Quantity);
        Assert.Equal("Gadget", match.Lines[1].Sku);
        Assert.Equal(2, match.Lines[1].Quantity);
        // Absent states: a missing element or an empty array reads as the instance's own (empty) collection —
        // the assignment is skipped on missing, driver-LINQ parity. An explicit BSON null reads as null, also
        // driver-LINQ parity (the driver assigns null for a null element).
        Assert.Empty(orders.Single(o => o.Name == "empty").Lines);
        Assert.Empty(orders.Single(o => o.Name == "missing").Lines);
        Assert.Null(orders.Single(o => o.Name == "null").Lines);
    }

    [Fact]
    public void Whole_entity_read_via_dom_shaper_declines_for_complex_collections()
    {
        // The DOM shaper's whole-entity path has no complex-collection read (EF's materializer block silently
        // assigns null); a DOM-routed shape (here: Single's cardinality) declines loudly rather than returning
        // null collections. Follow-up: a DOM complex-collection read slice.
        var collection = database.CreateCollection<Order>(values: ["dommat"]);
        SeedOrders(database.GetCollection<BsonDocument>(collection.CollectionNamespace));

        using var domContext = CreateContext(collection, MongoQueryMode.NativeOnly, OrderModel);

        var decline = Assert.Throws<NativeTranslationNotSupportedException>(
            () => domContext.Entities.Single(o => o.Name == "match"));
        Assert.Contains("complex collection", decline.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectMany_over_complex_collection_declines_cleanly()
    {
        // Whole-element re-rooting ($unwind + $replaceRoot) for complex elements is not built: it needs a
        // complex-type row shaper (the SelectMany machinery is entity-based end to end — ReRootProjectionAt,
        // EntityProjectionExpression, the materializers). EF fails the translation cleanly ("could not be
        // translated") in every mode; the decline (not wrong data) is the pinned contract for this slice.
        // Follow-up: a complex-type SelectMany slice on top of this one.
        var collection = database.CreateCollection<Order>(values: ["unwind"]);
        SeedOrders(database.GetCollection<BsonDocument>(collection.CollectionNamespace));

        using var context = CreateContext(collection, MongoQueryMode.NativeOnly, OrderModel);

        Assert.Throws<InvalidOperationException>(
            () => context.Entities.SelectMany(o => o.Lines).ToList());
    }

    [Fact]
    public void Nested_complex_collection_declines_loudly()
    {
        // A complex collection inside a complex property has no materializer on any native shaper; the query
        // declines loudly in every mode rather than silently materializing null collections.
        var collection = database.CreateCollection<Cart>(values: ["nested"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Info", new BsonDocument { { "Tags", new BsonArray { new BsonDocument { { "Label", "x" } } } } } }
        });

        using var context = CreateContext(collection, MongoQueryMode.NativeOnly, NestedModel);

        var decline = Assert.Throws<NativeTranslationNotSupportedException>(
            () => context.Entities.ToList());
        Assert.Contains("complex collection", decline.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Element_filter_matches_driver_linq_parity()
    {
        // Driver-LINQ has a working complex-collection oracle (the entity serializer) — but only for rows that
        // actually carry an array: over a missing/null Lines the driver renders an unguarded $anyElementTrue and
        // the SERVER rejects the command (a driver bug, outside this slice). Parity is pinned on populated rows;
        // the ragged missing/null/empty semantics are pinned natively by the tests above.
        var collection = database.CreateCollection<Order>(values: ["parity"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Name", "match" },
            { "Lines", new BsonArray { new BsonDocument { { "Sku", "Widget" }, { "Quantity", 10 } } } }
        });
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Name", "no-match" },
            { "Lines", new BsonArray { new BsonDocument { { "Sku", "Gadget" }, { "Quantity", 2 } } } }
        });

        using var driverContext = CreateContext(collection, MongoQueryMode.DriverLinq, OrderModel);
        var driverRows = driverContext.Entities.Where(o => o.Lines.Any(l => l.Sku == "Widget"))
            .Select(o => o.Name).ToList();

        using var nativeOnlyContext = CreateContext(collection, MongoQueryMode.NativeOnly, OrderModel);
        var nativeRows = nativeOnlyContext.Entities.Where(o => o.Lines.Any(l => l.Sku == "Widget"))
            .Select(o => o.Name).ToList();

        Assert.Equal(driverRows, nativeRows);
        Assert.Equal(["match"], nativeRows);
    }

    private class Cart
    {
        public ObjectId Id { get; set; }
        public Info Info { get; set; } = null!;
    }

    private class Info
    {
        public List<Tag> Tags { get; set; } = [];
    }

    private class Tag
    {
        public string Label { get; set; } = "";
    }

    private static readonly Action<ModelBuilder> NestedModel = mb =>
        mb.Entity<Cart>().ComplexProperty(c => c.Info).ComplexCollection(i => i.Tags);
}
#endif
