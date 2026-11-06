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
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// <c>GroupBy(k).Select(g =&gt; g.First())</c> and its ordered form (<c>g.OrderBy(..).First()</c>, which is also what EF11 rewrites
/// <c>MinBy</c>/<c>MaxBy</c> to) run natively as <c>$group</c> + <c>$first: "$$ROOT"</c> + <c>$replaceRoot</c>, with the ordering as a
/// <c>$sort</c> ahead of the <c>$group</c>. Shapes that cannot be answered that way must not run natively: under
/// <see cref="MongoQueryMode.NativeOnly"/> they throw rather than return a wrong document.
/// </summary>
public class NativeGroupByWholeEntityFirstTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    private class Item
    {
        public int Id { get; set; }
        public string Category { get; set; } = "";

        // Deliberately named like the grouping's Key: a translator that resolves members by name would sort by this
        // property when the query orders by g.Key.
        public int Key { get; set; }
        public int Amount { get; set; }
    }

    private static Item[] Seed() =>
    [
        new() { Id = 1, Category = "A", Key = 30, Amount = 10 },
        new() { Id = 2, Category = "A", Key = 10, Amount = 40 },
        new() { Id = 3, Category = "A", Key = 20, Amount = 25 },
        new() { Id = 4, Category = "B", Key = 5, Amount = 7 },
        new() { Id = 5, Category = "B", Key = 6, Amount = 90 },
        new() { Id = 6, Category = "C", Key = 1, Amount = 55 },
    ];

    private SingleEntityDbContext<Item> CreateContext(MongoQueryMode mode, string name)
    {
        var collectionName = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
        var collection = database.MongoDatabase.GetCollection<Item>(collectionName);
        collection.InsertMany(Seed());

        return SingleEntityDbContext.Create(
            collection,
            modelBuilderAction: null,
            optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });
    }

    private static List<int> Ids(IEnumerable<Item?> items) => items.Select(i => i!.Id).OrderBy(i => i).ToList();

    [Fact]
    public void Ordered_First_takes_the_first_document_in_each_group_by_the_ordering()
    {
        using var db = CreateContext(MongoQueryMode.NativeOnly, nameof(Ordered_First_takes_the_first_document_in_each_group_by_the_ordering));

        var result = db.Entities.GroupBy(e => e.Category).Select(g => g.OrderByDescending(e => e.Amount).ThenBy(e => e.Id).First()).ToList();

        // A: Amount 40 (Id 2); B: 90 (Id 5); C: 55 (Id 6).
        Assert.Equal([2, 5, 6], Ids(result));
    }

    [Fact]
    public void Ordered_First_with_a_repeated_field_keeps_the_first_direction()
    {
        // OrderBy(a).ThenByDescending(a) must not put "Amount" in the $sort document twice (a server/BSON error); the first direction wins.
        using var db = CreateContext(MongoQueryMode.NativeOnly, nameof(Ordered_First_with_a_repeated_field_keeps_the_first_direction));

        var result = db.Entities.GroupBy(e => e.Category).Select(g => g.OrderBy(e => e.Amount).ThenByDescending(e => e.Amount).First()).ToList();

        // Ascending wins: A: Amount 10 (Id 1); B: 7 (Id 4); C: 55 (Id 6).
        Assert.Equal([1, 4, 6], Ids(result));
    }

    [Fact]
    public void Ordered_First_by_the_grouping_key_is_not_answered_natively()
    {
        // g.Key is the group's constant key, not the entity's Key property: sorting by the entity's property would pick the wrong document.
        using var db = CreateContext(MongoQueryMode.NativeOnly, nameof(Ordered_First_by_the_grouping_key_is_not_answered_natively));

        Assert.ThrowsAny<Exception>(() => db.Entities.GroupBy(e => e.Category)
            .Select(g => g.OrderBy(e => g.Key).ThenBy(e => e.Id).First()).ToList());
    }

    [Fact]
    public void Whole_entity_First_beside_another_member_is_not_answered_natively()
    {
        // Beside Count() the entity would be read through a generic alias serializer, not the EF materializer.
        using var db = CreateContext(MongoQueryMode.NativeOnly, nameof(Whole_entity_First_beside_another_member_is_not_answered_natively));

        Assert.ThrowsAny<Exception>(() => db.Entities.GroupBy(e => e.Category)
            .Select(g => new { Entity = g.First(), N = g.Count() }).ToList());
    }

    [Fact]
    public void A_custom_reducer_named_like_First_is_not_replaced_with_first()
    {
        using var db = CreateContext(MongoQueryMode.NativeOnly, nameof(A_custom_reducer_named_like_First_is_not_replaced_with_first));

        Assert.ThrowsAny<Exception>(() => db.Entities.GroupBy(e => e.Category)
            .Select(g => CustomReducers.First(g)).ToList());
    }

    private static class CustomReducers
    {
        // Named like Enumerable.First but answers the LAST document.
        public static Item First(IEnumerable<Item> source) => source.Last();
    }
}
