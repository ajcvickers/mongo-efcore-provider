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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

#if !EF8 && !EF9
/// <summary>
/// A NON-NULLABLE leaf under an OPTIONAL complex parent is MISSING whenever the parent is null or absent. The aggregation
/// dialect orders MISSING below every value, so a relational comparison rendered there (a value-position projection, a
/// ternary test, a group key, or any operand wrapped in arithmetic, <c>Math</c> or a date part) answered true for such a row
/// where C# (null propagation) answers false. A plain <c>Where(Opt.I &lt; 5)</c> renders in the query dialect and was never
/// affected. Rows: a (I 1), b (Opt BSON null), c (I 0), d (Opt missing); answers are hand-written.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeOptionalParentAggregationTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public enum Col { Red, Blue, Green }

    public class Inner
    {
        public int? N { get; set; }
        public int I { get; set; }
        public Col E { get; set; }
        public DateTimeOffset D { get; set; }
        public decimal M { get; set; }
    }

    public class Plain
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Inner? Opt { get; set; }
    }

    private static readonly DateTimeOffset Cut = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private IMongoCollection<Plain> Seed(string name)
    {
        var collection = database.CreateCollection<Plain>(TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..6]);
        using (var db = Context(collection, MongoQueryMode.Native))
        {
            db.Entities.Add(new Plain { Name = "a", Opt = new Inner { I = 1, E = Col.Green, D = new DateTimeOffset(2021, 6, 1, 0, 0, 0, TimeSpan.Zero), M = 10m } });
            db.Entities.Add(new Plain { Name = "b", Opt = null });
            db.Entities.Add(new Plain { Name = "c", Opt = new Inner { I = 0, E = Col.Red, D = new DateTimeOffset(2019, 6, 1, 0, 0, 0, TimeSpan.Zero), M = 1m } });
            db.Entities.Add(new Plain { Name = "d", Opt = null });
            db.SaveChanges();
        }

        collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName)
            .UpdateOne(Builders<BsonDocument>.Filter.Eq("Name", "d"), Builders<BsonDocument>.Update.Unset("Opt"));
        return collection;
    }

    private static SingleEntityDbContext<Plain> Context(IMongoCollection<Plain> collection, MongoQueryMode mode)
        => SingleEntityDbContext.Create(collection, mb => mb.Entity<Plain>().ComplexProperty(p => p.Opt), null, b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            b.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    // NativeOnly answers or declines cleanly; Native answers. (Explicit DriverLinq is the driver's own semantics, not asserted.)
    private void AssertServed<TResult>(
        string name, Func<IQueryable<Plain>, IEnumerable<TResult>> query, params TResult[] expected)
    {
        var collection = Seed(name);
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native })
        {
            using var db = Context(collection, mode);
            List<TResult> rows;
            try
            {
                rows = query(db.Entities.OrderBy(p => p.Name)).ToList();
            }
            catch (NativeTranslationNotSupportedException) when (mode == MongoQueryMode.NativeOnly)
            {
                continue;
            }

            Assert.True(
                expected.SequenceEqual(rows),
                $"{mode}: expected [{string.Join("; ", expected)}], got [{string.Join("; ", rows)}]");
        }
    }

    [Fact]
    public void Relational_comparison_projected_over_a_missing_optional_parent_is_false()
    {
        // a I=1, b null, c I=0, d missing.
        AssertServed(nameof(Relational_comparison_projected_over_a_missing_optional_parent_is_false),
            q => q.Select(p => p.Opt!.I < 5), true, false, true, false);
        AssertServed(nameof(Relational_comparison_projected_over_a_missing_optional_parent_is_false) + "2",
            q => q.Select(p => p.Opt!.I <= 0), false, false, true, false);
        AssertServed(nameof(Relational_comparison_projected_over_a_missing_optional_parent_is_false) + "3",
            q => q.Select(p => p.Opt!.I >= 0), true, false, true, false);
        AssertServed(nameof(Relational_comparison_projected_over_a_missing_optional_parent_is_false) + "4",
            q => q.Select(p => 5 > p.Opt!.I), true, false, true, false);
    }

    [Fact]
    public void Relational_comparison_as_a_conditional_test_over_a_missing_optional_parent_takes_the_false_branch()
        => AssertServed(nameof(Relational_comparison_as_a_conditional_test_over_a_missing_optional_parent_takes_the_false_branch),
            q => q.Select(p => p.Opt!.I < 5 ? 1 : 0), 1, 0, 1, 0);

    [Fact]
    public void Operand_wrapped_in_arithmetic_math_or_a_date_part_over_a_missing_optional_parent_does_not_match()
    {
        AssertServed(nameof(Operand_wrapped_in_arithmetic_math_or_a_date_part_over_a_missing_optional_parent_does_not_match) + "1",
            q => q.Where(p => p.Opt!.I + 1 < 5).Select(p => p.Name), "a", "c");
        AssertServed(nameof(Operand_wrapped_in_arithmetic_math_or_a_date_part_over_a_missing_optional_parent_does_not_match) + "2",
            q => q.Where(p => p.Opt!.I * 2 < 5).Select(p => p.Name), "a", "c");
        AssertServed(nameof(Operand_wrapped_in_arithmetic_math_or_a_date_part_over_a_missing_optional_parent_does_not_match) + "3",
            q => q.Where(p => Math.Abs(p.Opt!.I) < 5).Select(p => p.Name), "a", "c");
        AssertServed(nameof(Operand_wrapped_in_arithmetic_math_or_a_date_part_over_a_missing_optional_parent_does_not_match) + "4",
            q => q.Where(p => p.Opt!.D.Year < 2020).Select(p => p.Name), "c");
        AssertServed(nameof(Operand_wrapped_in_arithmetic_math_or_a_date_part_over_a_missing_optional_parent_does_not_match) + "5",
            q => q.Select(p => p.Opt!.D.Year < 2020), false, false, true, false);
    }

    [Fact]
    public void Enum_date_and_decimal_leaves_compared_over_a_missing_optional_parent_are_false()
    {
        AssertServed(nameof(Enum_date_and_decimal_leaves_compared_over_a_missing_optional_parent_are_false) + "1",
            q => q.Select(p => p.Opt!.E < Col.Blue), false, false, true, false);
        AssertServed(nameof(Enum_date_and_decimal_leaves_compared_over_a_missing_optional_parent_are_false) + "2",
            q => q.Select(p => p.Opt!.D < Cut), false, false, true, false);
        AssertServed(nameof(Enum_date_and_decimal_leaves_compared_over_a_missing_optional_parent_are_false) + "3",
            q => q.Select(p => p.Opt!.M < 5m), false, false, true, false);
    }

    [Fact]
    public void Plain_filters_and_nullable_leaves_over_an_optional_parent_are_unchanged()
    {
        AssertServed(nameof(Plain_filters_and_nullable_leaves_over_an_optional_parent_are_unchanged) + "1",
            q => q.Where(p => p.Opt!.I < 5).Select(p => p.Name), "a", "c");
        AssertServed(nameof(Plain_filters_and_nullable_leaves_over_an_optional_parent_are_unchanged) + "2",
            q => q.Where(p => p.Opt!.I == 0).Select(p => p.Name), "c");
    }
}
#endif
