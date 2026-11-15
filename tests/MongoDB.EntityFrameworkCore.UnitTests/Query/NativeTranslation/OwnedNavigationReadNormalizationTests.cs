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
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;
using MongoDB.EntityFrameworkCore.UnitTests.TestUtilities;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation;

/// <summary>
/// <see cref="NativeReferencedElementMatcher.TryMatch"/> normalizes nav-expansion's spelling of an owned collection read in an element selector
/// (<c>EF.Property&lt;List&lt;Piece&gt;&gt;(m, "Pieces").AsQueryable().Count()</c>) to the member read the user wrote, and its <c>Queryable</c>
/// operators over that read to the matching <c>Enumerable</c> ones: the shaper runs the selector per element and row, where a <c>Queryable</c>
/// operator over an <c>EnumerableQuery</c> compiles an expression tree on every call.
/// </summary>
public class OwnedNavigationReadNormalizationTests
{
    private class Item
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public List<Piece> Pieces { get; set; } = [];
    }

    private class Piece
    {
        public string Name { get; set; } = "";
    }

    private static LambdaExpression? NormalizedSelector(Func<ParameterExpression, Expression> body)
    {
        using var db = SingleEntityDbContext.Create<Item>(mb => mb.Entity<Item>().OwnsMany(i => i.Pieces));
        var itemType = db.Model.FindEntityType(typeof(Item))!;

        var root = new EntityQueryRootExpression(itemType);
        Expression<Func<Item, bool>> correlation = i => i.OwnerId == 1;
        var where = Expression.Call(
            typeof(Queryable), nameof(Queryable.Where), [typeof(Item)], root, Expression.Quote(correlation));

        var m = Expression.Parameter(typeof(Item), "m");
        var selectorBody = body(m);
        var selector = Expression.Lambda(selectorBody, m);
        var select = Expression.Call(
            typeof(Queryable), nameof(Queryable.Select), [typeof(Item), selectorBody.Type], where, Expression.Quote(selector));
        var leaf = Expression.Call(typeof(Enumerable), nameof(Enumerable.ToList), [selectorBody.Type], select);

        Assert.True(NativeReferencedElementMatcher.TryMatch(leaf, out var shape));
        return shape.Selector;
    }

    // nav-expansion's `EF.Property<List<Piece>>(m, "Pieces").AsQueryable()`.
    private static Expression OwnedPieces(ParameterExpression m)
        => Expression.Call(
            typeof(Queryable), nameof(Queryable.AsQueryable), [typeof(Piece)],
            Expression.Call(typeof(EF), nameof(EF.Property), [typeof(List<Piece>)], m, Expression.Constant(nameof(Item.Pieces))));

    private static bool ContainsQueryableCall(Expression expression)
    {
        var finder = new QueryableCallFinder();
        finder.Visit(expression);
        return finder.Found;
    }

    private sealed class QueryableCallFinder : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Found |= node.Method.DeclaringType == typeof(Queryable);
            return base.VisitMethodCall(node);
        }
    }

    [Fact]
    public void A_count_of_an_owned_collection_reads_the_member_through_Enumerable()
    {
        var selector = NormalizedSelector(
            m => Expression.Call(typeof(Queryable), nameof(Queryable.Count), [typeof(Piece)], OwnedPieces(m)));

        Assert.NotNull(selector);
        Assert.False(ContainsQueryableCall(selector.Body));
        var call = Assert.IsAssignableFrom<MethodCallExpression>(selector.Body);
        Assert.Equal(typeof(Enumerable), call.Method.DeclaringType);
        Assert.IsAssignableFrom<MemberExpression>(call.Arguments[0]);
    }

    [Fact]
    public void A_chain_of_operators_with_lambdas_over_an_owned_collection_is_Enumerable()
    {
        Expression<Func<Piece, bool>> filter = p => p.Name != "x";
        Expression<Func<Piece, string>> name = p => p.Name;
        var selector = NormalizedSelector(
            m => Expression.Call(
                typeof(Queryable), nameof(Queryable.Count), [typeof(string)],
                Expression.Call(
                    typeof(Queryable), nameof(Queryable.Select), [typeof(Piece), typeof(string)],
                    Expression.Call(
                        typeof(Queryable), nameof(Queryable.Where), [typeof(Piece)], OwnedPieces(m), Expression.Quote(filter)),
                    Expression.Quote(name))));

        Assert.NotNull(selector);
        Assert.False(ContainsQueryableCall(selector.Body));

        var compiled = (Func<Item, int>)selector.Compile();
        Assert.Equal(2, compiled(new Item { Pieces = [new Piece { Name = "a" }, new Piece { Name = "x" }, new Piece { Name = "b" }] }));
    }
}
