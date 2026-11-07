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
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// A relational comparison at the ROOT over a date-add, a coalesce or a conditional whose only non-field operand is a
/// CAPTURED NON-NULLABLE value (<c>x.Date.AddDays(days)</c>, <c>x.Amount ?? fallback</c>, <c>x.Flag ? x.Rank : other</c>)
/// renders no relational null guard: a captured <c>int</c>/<c>double</c> is never null, so the operand can't be. The
/// expected MQL is the base's (<c>f5027aa7</c>, before ruling R18 made these nodes structural), byte for byte; only
/// null-operand shapes (pinned in <see cref="NativeRootNullPropagatingOperandGuardTests"/>, spec exception (e)) changed.
/// </summary>
[XUnitCollection("QueryTests")]
public class NativeRootCapturedParameterOperandMqlTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Reading
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public bool Flag { get; set; }
        public int Rank { get; set; }
        public DateTime Date { get; set; }
        public double? Amount { get; set; }
    }

    private static readonly DateTime May = new(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private (List<string> Rows, string Mql) Run(Expression<Func<Reading, bool>> predicate, string name)
    {
        var collection = database.CreateCollection<Reading>(TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8]);
        collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "a" }, { "Flag", true }, { "Rank", 1 }, { "Date", May }, { "Amount", 1.0 } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "b" }, { "Flag", false }, { "Rank", 9 }, { "Date", May.AddDays(20) }, { "Amount", BsonNull.Value } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "c" }, { "Flag", true }, { "Rank", 3 }, { "Date", May.AddDays(1) } }
        ]);

        var (loggerFactory, spy) = SpyLoggerProvider.Create();
        using var db = SingleEntityDbContext.Create(collection, loggerFactory, optionsBuilderAction: b =>
        {
            b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            b.EnableSensitiveDataLogging();
            new MongoDbContextOptionsBuilder(b).UseQueryMode(MongoQueryMode.NativeOnly);
        });
        var rows = db.Entities.AsNoTracking().Where(predicate).Select(x => x.Name).ToList().Order(StringComparer.Ordinal).ToList();
        var mql = spy.GetLogMessageByEventId(MongoEventId.ExecutedMqlQuery);
        return (rows, mql[mql.IndexOf(".aggregate(", StringComparison.Ordinal)..]);
    }

    [Fact]
    public void Date_add_with_a_captured_amount_renders_no_null_guard()
    {
        var days = 3;
        var cutoff = May.AddDays(5);
        var (rows, mql) = Run(x => x.Date.AddDays(days) < cutoff, nameof(Date_add_with_a_captured_amount_renders_no_null_guard));

        Assert.Equal(["a", "c"], rows);
        Assert.Equal(
            """.aggregate([{ "$match" : { "$expr" : { "$lt" : [{ "$dateAdd" : { "startDate" : "$Date", "unit" : "day", "amount" : 3.0 } }, { "$literal" : { "$date" : "2024-05-06T00:00:00Z" } }] } } }, { "$project" : { "Name" : "$Name", "_id" : 0 } }])""",
            mql);
    }

    [Fact]
    public void Coalesce_with_a_captured_fallback_renders_no_null_guard()
    {
        var fallback = 0.5;
        var limit = 2.0;
        var (rows, mql) = Run(x => (x.Amount ?? fallback) < limit, nameof(Coalesce_with_a_captured_fallback_renders_no_null_guard));

        Assert.Equal(["a", "b", "c"], rows);
        // The base guarded this one too (the coalesce takes its CLR type from the parameter fallback, which the CLR-type
        // check judges nullable), so the conjunct below is the base's, not R18's: unchanged either way.
        Assert.Equal(
            """.aggregate([{ "$match" : { "$expr" : { "$and" : [{ "$gt" : [{ "$ifNull" : ["$Amount", { "$literal" : 0.5 }] }, null] }, { "$lt" : [{ "$ifNull" : ["$Amount", { "$literal" : 0.5 }] }, { "$literal" : 2.0 }] }] } } }, { "$project" : { "Name" : "$Name", "_id" : 0 } }])""",
            mql);
    }

    [Fact]
    public void Conditional_with_a_captured_branch_renders_no_null_guard()
    {
        var other = 4;
        var (rows, mql) = Run(x => (x.Flag ? x.Rank : other) < 3, nameof(Conditional_with_a_captured_branch_renders_no_null_guard));

        Assert.Equal(["a"], rows);
        Assert.Equal(
            """.aggregate([{ "$match" : { "$expr" : { "$lt" : [{ "$cond" : { "if" : "$Flag", "then" : "$Rank", "else" : { "$literal" : 4 } } }, 3] } } }, { "$project" : { "Name" : "$Name", "_id" : 0 } }])""",
            mql);
    }
}
