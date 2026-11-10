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

// MongoQueryableMethodTranslatingExpressionVisitor.VisitMethodCall admits only Queryable, MongoQueryableExtensions and
// MongoDB.Driver.Linq.MongoQueryable (plus, on EF9+, the EntityFrameworkQueryableExtensions bulk operators, which it forwards to the
// base). A queryable extension method declared anywhere else must still produce EF's canonical translation failure. This pins that
// gate on every EF version: it is deliberately not #if'd, because the gate exists on every version and the EF9+ bulk early-return
// sits directly above it.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

public class InvalidQueryTranslationTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class SimpleEntity
    {
        public ObjectId _id { get; set; }
        public string name { get; set; } = null!;
    }

    [Fact]
    public void Foreign_queryable_extension_throws_translation_failure()
    {
        using var db = SingleEntityDbContext.Create(database.CreateCollection<SimpleEntity>());

        var ex = Assert.Throws<InvalidOperationException>(() => db.Entities.FakeExecuteDelete());

        Assert.Contains("could not be translated", ex.Message);
        Assert.Contains("FakeExecuteDelete", ex.Message);
    }
}

internal static class FakeQueryableExtensions
{
    internal static int FakeExecuteDelete<TSource>(this IQueryable<TSource> source)
        => source.Provider.Execute<int>(
            Expression.Call(FakeExecuteDeleteMethodInfo.MakeGenericMethod(typeof(TSource)), source.Expression));

    private static readonly MethodInfo FakeExecuteDeleteMethodInfo
        = typeof(FakeQueryableExtensions).GetTypeInfo().GetDeclaredMethod(nameof(FakeExecuteDelete))!;
}
