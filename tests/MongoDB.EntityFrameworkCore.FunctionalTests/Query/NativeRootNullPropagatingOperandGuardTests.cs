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
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.CompositionAssert;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// ROOT (entity scope, no complex collection) pins of ruling R18: <c>MayBeNull</c> is structural for a date-add, a
/// conditional and a coalesce, so a relational comparison over one of them whose operand may be null gets the same
/// null guard (<c>$gt: [operand, null]</c>) as every other relational comparison, and rows whose operand is null no
/// longer match <c>&lt;</c>/<c>&lt;=</c>. An owner-visible root behaviour change (spec exception (e), ruling R22).
/// </summary>
/// <remarks>
/// <para>
/// Measured at the base before R18 (<c>b4ddcf95</c>) and at HEAD on the same seed; driver-LINQ is main's path. Rows: v (every
/// value present), h (high values; <c>A</c> null, <c>B</c> 7), n (every nullable null), f (as n, <c>Flag</c> false), m (every
/// nullable MISSING). The rows that changed are exactly those whose operand evaluates to null (n, f, m); the change
/// removes them from <c>&lt;</c>, matching what the already-guarded shapes (a nullable-typed date-add, a conditional with a
/// null-typed branch, a nullable coalesce) answered at the base, and moving native AWAY from the driver's rows in the
/// SAME direction those shapes already differed from it (the driver orders missing/null below every value and has no
/// guard: the existing relational-guard policy). The CLR answer: <c>.Value</c> throws; EF's null propagation says false.
/// </para>
/// </remarks>
[XUnitCollection("QueryTests")]
public class NativeRootNullPropagatingOperandGuardTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Reading
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public bool Flag { get; set; }
        public int Rank { get; set; }
        public DateTime Date { get; set; }
        public DateTime? NullableDate { get; set; }
        public double? Amount { get; set; }
        public int? Score { get; set; }
        public int? A { get; set; }
        public int? B { get; set; }
        public string? Label { get; set; }
    }

    private static readonly DateTime May = new(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Cutoff = May.AddDays(5);

    private static BsonDocument Doc(string name, bool flag, BsonValue nullableDate, BsonValue amount, BsonValue score, BsonValue a, BsonValue b, BsonValue label)
        => new()
        {
            { "_id", ObjectId.GenerateNewId() }, { "Name", name }, { "Flag", flag }, { "Rank", 2 }, { "Date", May },
            { "NullableDate", nullableDate }, { "Amount", amount }, { "Score", score }, { "A", a }, { "B", b }, { "Label", label }
        };

    /// <summary>
    /// v: Flag, Date May, NullableDate May, Amount 1, Score 2, A 2, B 2, Label "ab". h: Flag, NullableDate May+10d, Amount 10,
    /// Score 7, A null, B 7, Label "abcdefgh". n: Flag, every nullable BSON null. f: as n but Flag false. m: Flag, every nullable
    /// MISSING. Rank is 2 and Date is May on every row.
    /// </summary>
    private Func<MongoQueryMode, List<string>> Readings(Expression<Func<Reading, bool>> predicate, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = database.CreateCollection<Reading>(TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8]);
        collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName).InsertMany(
        [
            Doc("v", true, May, 1.0, 2, 2, 2, "ab"),
            Doc("h", true, May.AddDays(10), 10.0, 7, BsonNull.Value, 7, "abcdefgh"),
            Doc("n", true, BsonNull.Value, BsonNull.Value, BsonNull.Value, BsonNull.Value, BsonNull.Value, BsonNull.Value),
            Doc("f", false, BsonNull.Value, BsonNull.Value, BsonNull.Value, BsonNull.Value, BsonNull.Value, BsonNull.Value),
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "m" }, { "Flag", true }, { "Rank", 2 }, { "Date", May } }
        ]);
        return mode =>
        {
            using var db = SingleEntityDbContext.Create(collection, optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });
            return db.Entities.AsNoTracking().Where(predicate).Select(x => x.Name).ToList().Order(StringComparer.Ordinal).ToList();
        };
    }

    /// <summary>
    /// Native modes serve <paramref name="expected"/> (the guarded answer); driver-LINQ (main's path) is pinned to its own
    /// measured rows, which include the null-operand rows. <paramref name="baseRows"/> documents what native answered at
    /// b4ddcf95, before R18 (a comment-level record made assertable: it must differ from <paramref name="expected"/> exactly by
    /// null-operand rows).
    /// </summary>
    private static void Guarded(Func<MongoQueryMode, List<string>> run, string[] expected, string[] driverRows, string[] baseRows)
    {
        Assert.True(expected.All(baseRows.Contains), "the guard can only remove rows");
        Assert.True(baseRows.Except(expected).All(r => r is "n" or "f" or "m"), "only null-operand rows may be removed");
        foreach (var mode in new[] { MongoQueryMode.NativeOnly, MongoQueryMode.Native })
        {
            var rows = run(mode);
            Assert.True(expected.SequenceEqual(rows), $"{mode}: expected [{string.Join("; ", expected)}], got [{string.Join("; ", rows)}]");
        }

        var driver = run(MongoQueryMode.DriverLinq);
        Assert.True(driverRows.SequenceEqual(driver), $"DriverLinq: expected [{string.Join("; ", driverRows)}], got [{string.Join("; ", driver)}]");
    }

    [Fact]
    public void Date_add_over_a_null_operand_at_the_root_gets_the_relational_guard()
    {
        // Already guarded at the base (the date-add's CLR type is DateTime?): unchanged by R18; the driver includes n, f, m.
        Guarded(Readings(x => x.NullableDate!.Value.AddDays(1) < Cutoff), ["v"], ["f", "m", "n", "v"], ["v"]);
        Guarded(Readings(x => x.NullableDate!.Value.AddDays(1) <= Cutoff), ["v"], ["f", "m", "n", "v"], ["v"]);
        // CHANGED by R18: a non-nullable start date with a nullable AMOUNT. Base native [f, m, n, v]; now [v]; the driver [f, m, n, v].
        Guarded(Readings(x => x.Date.AddDays(x.Amount!.Value) < Cutoff), ["v"], ["f", "m", "n", "v"], ["f", "m", "n", "v"]);
        // CHANGED: the amount is a conditional whose false branch is nullable (f takes it). Base [f, h, m, n, v]; now [h, m, n, v].
        Guarded(Readings(x => x.Date.AddDays(x.Flag ? 1 : x.Amount!.Value) < Cutoff), ["h", "m", "n", "v"], ["f", "h", "m", "n", "v"], ["f", "h", "m", "n", "v"]);
        // CHANGED: the START date is a conditional whose false branch is nullable. Base [f, h, m, n, v]; now [h, m, n, v].
        Guarded(Readings(x => (x.Flag ? x.Date : x.NullableDate!.Value).AddDays(1) < Cutoff), ["h", "m", "n", "v"], ["f", "h", "m", "n", "v"], ["f", "h", "m", "n", "v"]);
        // `>` never matched a null operand (null is below every value): unchanged in every mode.
        Native(Readings(x => x.NullableDate!.Value.AddDays(1) > May), "h", "v");
        Native(Readings(x => x.Date.AddDays(x.Amount!.Value) > May), "h", "v");
    }

    [Fact]
    public void Conditional_over_a_null_branch_at_the_root_gets_the_relational_guard()
    {
        // Already guarded at the base when the TRUE branch is the nullable read (`.Value` is peeled, so the branch stays int?-typed
        // and the conditional takes its type): v takes Score 2; f takes the constant/Rank; h takes 7; n and m take a null Score.
        // Unchanged by R18; the driver includes m and n.
        Guarded(Readings(x => (x.Flag ? x.Score!.Value : 0) < 5), ["f", "v"], ["f", "m", "n", "v"], ["f", "v"]);
        Guarded(Readings(x => (x.Flag ? x.Score!.Value : x.Rank) < 5), ["f", "v"], ["f", "m", "n", "v"], ["f", "v"]);
        Guarded(Readings(x => (x.Flag ? x.Score : 0) < 5), ["f", "v"], ["f", "m", "n", "v"], ["f", "v"]);
        // CHANGED by R18: the FALSE branch is the nullable read (only f takes it, with a null Score). Base [f, h, m, n, v]; now
        // [h, m, n, v] (h, m, n, v take the constant 0 / Rank 2). The driver includes f.
        Guarded(Readings(x => (x.Flag ? 0 : x.Score!.Value) < 5), ["h", "m", "n", "v"], ["f", "h", "m", "n", "v"], ["f", "h", "m", "n", "v"]);
        Guarded(Readings(x => (x.Flag ? x.Rank : x.Score!.Value) < 5), ["h", "m", "n", "v"], ["f", "h", "m", "n", "v"], ["f", "h", "m", "n", "v"]);
    }

    [Fact]
    public void Coalesce_whose_both_sides_may_be_null_at_the_root_gets_the_relational_guard()
    {
        // Already guarded at the base (`A ?? B` is int?-typed): unchanged; the driver includes f, m, n.
        Guarded(Readings(x => (x.A ?? x.B) < 5), ["v"], ["f", "m", "n", "v"], ["v"]);
        Guarded(Readings(x => (x.A ?? x.B!.Value) < 5), ["v"], ["f", "m", "n", "v"], ["v"]);
        // CHANGED by R18: the fallback is a null-guarded `Length` over a nullable string (int-typed; null when Label is null).
        // Base native [f, m, n, v]; now [v]. The driver's $strLenCP over a null/missing string is a server error (loud).
        PerMode(Readings(x => (x.A ?? x.Label!.Length) < 5), ["v"], Serves, Serves, Throws<MongoDB.Driver.MongoCommandException>("$strLenCP requires a string argument"));
        // CHANGED: the fallback is a conditional with a nullable false branch. Base [f, h, m, n, v]; now [h, m, n, v].
        Guarded(Readings(x => (x.A ?? (x.Flag ? 0 : x.B!.Value)) < 5), ["h", "m", "n", "v"], ["f", "h", "m", "n", "v"], ["f", "h", "m", "n", "v"]);
        // A non-nullable fallback (`Rank`) is never null: no guard, unchanged, every row matches in every mode.
        Native(Readings(x => (x.A ?? x.Rank) < 5), "f", "h", "m", "n", "v");
        // `>`: unchanged in every mode.
        Native(Readings(x => (x.A ?? x.B) > 1), "h", "v");
    }
}
