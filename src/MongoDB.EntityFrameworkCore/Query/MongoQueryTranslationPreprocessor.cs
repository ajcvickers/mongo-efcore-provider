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
using MongoDB.EntityFrameworkCore.Query.Visitors;

namespace MongoDB.EntityFrameworkCore.Query;

/// <inheritdoc />
public class MongoQueryTranslationPreprocessor : QueryTranslationPreprocessor
{
    /// <inheritdoc />
    public MongoQueryTranslationPreprocessor(
        QueryTranslationPreprocessorDependencies dependencies,
        QueryCompilationContext queryCompilationContext)
        : base(dependencies, queryCompilationContext)
    {
    }

    /// <inheritdoc />
    public override Expression Process(Expression query)
    {
        query = FinalPredicateHoistingVisitor.Hoist(query);
        query = new EntityFrameworkDetourExpressionVisitor(QueryCompilationContext).Visit(query);
        query = new LocalCollectionFilterFoldingVisitor(QueryCompilationContext).Visit(query);
#if EF11
        query = DefaultIfEmptyBeforeFirstRewriter.Rewrite(query);
#endif

        // Nav expansion throws for IQueryable methods that it is not aware of, so we remove
        // any VectorSearch call from the root and then put it back after. This only works because
        // nav-expansion has nothing to do for this call.
        query = VectorSearchExtractor.RemoveVectorSearchCalls(query, out var removed);

        // A derived-type root (Set<Dog>() over a Dog : Animal hierarchy) reads the whole collection unless it is narrowed by
        // discriminator, so it would return, or fail to materialize, documents of sibling types. Not applied with a vector
        // search: the replacer below re-attaches the call at the first root, which the narrowing would sit beneath.
        if (removed == null)
        {
            query = DerivedEntityRootNarrower.Narrow(query);
        }

        query = base.Process(query);
        query = VectorSearchReplacer.ReplaceVectorSearchCalls(query, removed);

        return query;
    }

#if !EF8

    /// <inheritdoc />
    protected override bool IsEfConstantSupported => true;

#endif

    /// <summary>
    /// Rewrites the query root of a TPH-derived entity type, <c>Set&lt;Dog&gt;()</c>, to <c>Set&lt;Animal&gt;().OfType&lt;Dog&gt;()</c>.
    /// </summary>
    /// <remarks>
    /// Relational providers add the discriminator condition when they create the root query; this provider's
    /// <c>MongoQueryExpression</c> reads the whole collection. Expressing the narrowing as <c>OfType</c> reuses the one
    /// discriminator predicate (<c>MongoDiscriminatorPredicate</c>) in every mode, including the driver-LINQ fallback, which
    /// works from the captured EF chain and never sees the native <c>$match</c>. Applies wherever a root appears (a join
    /// side, a subquery), because it rewrites the root node itself.
    /// </remarks>
    private sealed class DerivedEntityRootNarrower : ExpressionVisitor
    {
        public static Expression Narrow(Expression query)
            => new DerivedEntityRootNarrower().Visit(query);

        protected override Expression VisitExtension(Expression node)
        {
            if (node is EntityQueryRootExpression { EntityType: { BaseType: not null } derivedType }
                && derivedType.FindDiscriminatorProperty() != null)
            {
                var rootType = derivedType.GetRootType();
                return Expression.Call(
                    QueryableMethods.OfType.MakeGenericMethod(derivedType.ClrType),
                    new EntityQueryRootExpression(rootType));
            }

            return base.VisitExtension(node);
        }
    }

#if EF11

    /// <summary>
    /// Rewrites <c>source.DefaultIfEmpty().MinBy(k)</c>, <c>MaxBy(k)</c> and <c>source.DefaultIfEmpty().OrderBy(k)....First()</c>
    /// to <c>source.OrderBy(k)....FirstOrDefault()</c>.
    /// </summary>
    /// <remarks>
    /// Without an explicit default value, <c>DefaultIfEmpty</c> is the identity on a non-empty source and yields a single
    /// <c>default(T)</c> on an empty one. The first element is therefore the first ordered element, or <c>default(T)</c> for an
    /// empty source: what <c>FirstOrDefault</c> returns. Ordering one defaulted element is a no-op, so the keys are never
    /// evaluated against it. The rewrite runs before navigation expansion, which cannot fold a projection into a sort key across
    /// <c>DefaultIfEmpty</c>, and before EF normalizes <c>MinBy</c>/<c>MaxBy</c> to <c>First</c>, which would throw for an empty
    /// value-type source. Any other use of <c>DefaultIfEmpty</c> is left alone. Applies wherever the pattern appears, including
    /// subqueries.
    /// </remarks>
    private sealed class DefaultIfEmptyBeforeFirstRewriter : ExpressionVisitor
    {
        public static Expression Rewrite(Expression query)
            => new DefaultIfEmptyBeforeFirstRewriter().Visit(query);

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var visited = base.VisitMethodCall(node);
            if (visited is not MethodCallExpression call
                || call.Method.DeclaringType != typeof(Queryable)
                || !call.Method.IsGenericMethod)
            {
                return visited;
            }

            var definition = call.Method.GetGenericMethodDefinition();
            var elementType = call.Method.GetGenericArguments()[0];

            if (definition == QueryableMethods.MinBy || definition == QueryableMethods.MaxBy)
            {
                if (!TryGetDefaultIfEmptySource(call.Arguments[0], out var source))
                {
                    return call;
                }

                var keyType = call.Method.GetGenericArguments()[1];
                var ordered = Expression.Call(
                    (definition == QueryableMethods.MinBy ? QueryableMethods.OrderBy : QueryableMethods.OrderByDescending)
                    .MakeGenericMethod(elementType, keyType),
                    source,
                    call.Arguments[1]);

                return FirstOrDefault(elementType, ordered);
            }

            if (definition != QueryableMethods.FirstWithoutPredicate
                && definition != QueryableMethods.FirstOrDefaultWithoutPredicate)
            {
                return call;
            }

            var orderings = new List<MethodCallExpression>();
            var current = call.Arguments[0];
            while (current is MethodCallExpression ordering
                   && ordering.Method.DeclaringType == typeof(Queryable)
                   && ordering.Method.Name is nameof(Queryable.OrderBy) or nameof(Queryable.OrderByDescending)
                       or nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending))
            {
                orderings.Add(ordering);
                current = ordering.Arguments[0];
            }

            if (!TryGetDefaultIfEmptySource(current, out var rebuilt))
            {
                return call;
            }

            for (var i = orderings.Count - 1; i >= 0; i--)
            {
                var arguments = orderings[i].Arguments.ToArray();
                arguments[0] = rebuilt;
                rebuilt = orderings[i].Update(orderings[i].Object, arguments);
            }

            return FirstOrDefault(elementType, rebuilt);
        }

        private static bool TryGetDefaultIfEmptySource(Expression expression, out Expression source)
        {
            if (expression is MethodCallExpression { Method: { DeclaringType: var type, IsGenericMethod: true } method } call
                && type == typeof(Queryable)
                && method.GetGenericMethodDefinition() == QueryableMethods.DefaultIfEmptyWithoutArgument)
            {
                source = call.Arguments[0];
                return true;
            }

            source = expression;
            return false;
        }

        private static MethodCallExpression FirstOrDefault(Type elementType, Expression source)
            => Expression.Call(QueryableMethods.FirstOrDefaultWithoutPredicate.MakeGenericMethod(elementType), source);
    }

#endif

    private sealed class VectorSearchExtractor : ExpressionVisitor
    {
        private MethodCallExpression? _removed;

        private VectorSearchExtractor()
        {
        }

        public static Expression RemoveVectorSearchCalls(Expression expression, out MethodCallExpression? removed)
        {
            var visitor = new VectorSearchExtractor();
            var processed = visitor.Visit(expression);
            removed = visitor._removed;
            return processed;
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.IsVectorSearch()
                && methodCallExpression.Arguments[0] is QueryRootExpression)
            {
                _removed = methodCallExpression;
                return Visit(methodCallExpression.Arguments[0]);
            }

            return base.VisitMethodCall(methodCallExpression);
        }
    }

    private sealed class VectorSearchReplacer : ExpressionVisitor
    {
        private readonly MethodCallExpression _removed;

        private VectorSearchReplacer(MethodCallExpression removed)
        {
            _removed = removed;
        }

        public static Expression ReplaceVectorSearchCalls(Expression expression, MethodCallExpression? removed)
            => removed == null ? expression : new VectorSearchReplacer(removed).Visit(expression)!;

        public override Expression? Visit(Expression? node)
        {
            if (node is EntityQueryRootExpression)
            {
                var arguments = _removed.Arguments.ToList();
                arguments[0] = node;
                return Expression.Call(_removed.Method, arguments);
            }

            return base.Visit(node);
        }
    }
}

