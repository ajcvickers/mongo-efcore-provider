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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.EntityFrameworkCore.Query.Expressions;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

internal static class NativeQueryParameter
{
    /// <summary>EF8/EF9 prefixed <see cref="ParameterExpression"/> or EF10 <c>QueryParameterExpression</c>; extracts its name.</summary>
    public static bool TryGetQueryParameterName(Expression expr, [NotNullWhen(true)] out string? name)
    {
#if EF8 || EF9
        if (expr is ParameterExpression param
            && param.Name?.StartsWith(QueryCompilationContext.QueryParameterPrefix, StringComparison.Ordinal) == true)
        {
            name = param.Name;
            return true;
        }
#else
        if (expr is QueryParameterExpression queryParam)
        {
            name = queryParam.Name;
            return true;
        }
#endif

        name = null;
        return false;
    }

    /// <summary>
    /// Recognizes a constant-index access into a query-parameter array (<c>args[0]</c> in a compiled query). EF doesn't
    /// pre-evaluate it, so the element is resolved per execution (<see cref="Expressions.MongoParameterExpression.ArrayElementIndex"/>).
    /// Matches an <see cref="ExpressionType.ArrayIndex"/> node and the <c>Enumerable.ElementAt</c> call EF10 rewrites it to.
    /// </summary>
    public static bool TryGetParameterArrayElementIndex(Expression expr, [NotNullWhen(true)] out string? name, out int index)
    {
        if (expr is BinaryExpression { NodeType: ExpressionType.ArrayIndex } arrayIndex
            && TryGetQueryParameterName(arrayIndex.Left, out name)
            && arrayIndex.Right is ConstantExpression { Value: int constantArrayIndex })
        {
            index = constantArrayIndex;
            return true;
        }

        if (expr is MethodCallExpression { Method.Name: nameof(Enumerable.ElementAt) } call
            && call.Method.IsStatic && call.Method.DeclaringType == typeof(Enumerable) && call.Arguments.Count == 2
            && TryGetQueryParameterName(call.Arguments[0], out name)
            && call.Arguments[1] is ConstantExpression { Value: int constantElementAtIndex })
        {
            index = constantElementAtIndex;
            return true;
        }

        name = null;
        index = 0;
        return false;
    }

    /// <summary>
    /// Replaces each query parameter in <paramref name="body"/> (each node <see cref="TryGetQueryParameterName"/> accepts) with
    /// <c>Convert(queryContext.Parameters[name], node.Type)</c>, so a selector compiled into a shaper reads the parameter's value per execution from
    /// <paramref name="queryContext"/> (the shaper's <see cref="QueryCompilationContext.QueryContextParameter"/>), never a value baked into the
    /// cached plan. <see cref="IsQueryParameterRead"/> recognizes the result.
    /// </summary>
    internal static Expression ReplaceQueryParameters(Expression body, Expression queryContext)
        => new QueryParameterReplacer(queryContext).Visit(body);

    /// <summary>
    /// Whether <paramref name="node"/> is a read of a query parameter's value that <see cref="ReplaceQueryParameters"/> wrote: a dictionary
    /// lookup on <paramref name="queryContext"/>, free of side effects.
    /// </summary>
    internal static bool IsQueryParameterRead(Expression node, Expression queryContext)
        => node is UnaryExpression
           {
               NodeType: ExpressionType.Convert,
               Operand: IndexExpression
               {
                   Object: MemberExpression { Member.Name: ParameterValuesPropertyName } parameters,
                   Arguments: [ConstantExpression { Value: string }]
               }
           }
           && parameters.Expression == queryContext;

#if EF8 || EF9
    private const string ParameterValuesPropertyName = nameof(QueryContext.ParameterValues);
#else
    private const string ParameterValuesPropertyName = nameof(QueryContext.Parameters);
#endif

    private sealed class QueryParameterReplacer(Expression queryContext) : ExpressionVisitor
    {
        [return: NotNullIfNotNull(nameof(node))]
        public override Expression? Visit(Expression? node)
            => node is not null && TryGetQueryParameterName(node, out var name)
                ? Expression.Convert(
                    Expression.Property(
                        Expression.Property(queryContext, ParameterValuesPropertyName), "Item", Expression.Constant(name)),
                    node.Type)
                : base.Visit(node);
    }

    /// <summary>
    /// Translates a literal to a baked-in <see cref="MongoConstantExpression"/> or a query parameter to a per-execution
    /// <see cref="MongoParameterExpression"/>, both serialized through <paramref name="forSerialization"/>; <see langword="null"/>
    /// otherwise. Unlike <c>MongoExpressionTranslator.TranslateValue</c>: no array-element parameters, no value type, no convert unwrapping.
    /// </summary>
    public static MongoExpression? TranslateConstantOrParameter(Expression expr, IProperty? forSerialization)
    {
        if (expr is ConstantExpression constant)
            return new MongoConstantExpression(constant.Value, forSerialization);

        if (TryGetQueryParameterName(expr, out var name))
            return new MongoParameterExpression(name, forSerialization);

        return null;
    }
}
