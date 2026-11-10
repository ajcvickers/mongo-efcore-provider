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

#if !EF8

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Expressions;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.Expressions;

public static class MongoNonQueryExpressionUnwrapTests
{
    private class Product
    {
        public ObjectId _id { get; set; }
        public string Name { get; set; } = null!;
    }

    private static IQueryable<Product> Source => new Product[0].AsQueryable();

    private static MethodInfo EfExtension(string name)
        => typeof(EntityFrameworkQueryableExtensions).GetMethod(name)!.MakeGenericMethod(typeof(Product));

    [Theory]
    [InlineData(nameof(EntityFrameworkQueryableExtensions.ExecuteDelete))]
    [InlineData(nameof(EntityFrameworkQueryableExtensions.ExecuteDeleteAsync))]
    public static void Unwraps_ExecuteDelete_returning_the_source_chain(string operatorName)
    {
        var chain = Source.Where(p => p.Name == "x");
        var method = EfExtension(operatorName);
        var captured = method.GetParameters().Length == 1
            ? Expression.Call(method, chain.Expression)
            : Expression.Call(method, chain.Expression, Expression.Constant(CancellationToken.None));

        Assert.Same(chain.Expression, MongoNonQueryExpression.UnwrapBulkOperator(captured));
    }

    [Fact]
    public static void Returns_expression_unchanged_when_not_a_bulk_operator()
    {
        var chain = Source.Where(p => p.Name == "x");

        Assert.Same(chain.Expression, MongoNonQueryExpression.UnwrapBulkOperator(chain.Expression));
    }

    [Theory]
    [InlineData(nameof(EntityFrameworkQueryableExtensions.AsNoTracking))]
    [InlineData(nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters))]
    [InlineData(nameof(EntityFrameworkQueryableExtensions.TagWith))]
    public static void Does_not_unwrap_a_non_bulk_EntityFrameworkQueryableExtensions_call(string methodName)
    {
        // These are declared on EntityFrameworkQueryableExtensions but are not bulk operators. Unwrapping one would silently
        // discard it from the chain the classifier walks, instead of leaving it for the classifier to see and reject.
        var chain = Source.Where(p => p.Name == "x");
        var method = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .First(m => m.Name == methodName && m.IsGenericMethodDefinition)
            .MakeGenericMethod(typeof(Product));
        var arguments = method.GetParameters().Length == 1
            ? new Expression[] { chain.Expression }
            : new Expression[] { chain.Expression, Expression.Constant("tag") };
        var captured = Expression.Call(method, arguments);

        Assert.Same(captured, MongoNonQueryExpression.UnwrapBulkOperator(captured));
    }

    [Fact]
    public static void Returns_null_for_null()
        => Assert.Null(MongoNonQueryExpression.UnwrapBulkOperator(null));
}

#endif
