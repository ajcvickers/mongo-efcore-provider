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
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.Bson;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// Walks the captured EF query method chain (single collection) and produces an aggregation
/// pipeline of <c>$match</c>/<c>$sort</c>/<c>$skip</c>/<c>$limit</c> stages. A trailing pure
/// <c>Select</c> is ignored (full documents returned; the existing shaper projects client-side).
/// Throws <see cref="NativeTranslationNotSupportedException"/> on anything else.
/// </summary>
internal sealed class MongoPipelineTranslator
{
    private readonly IEntityType _entityType;
    private readonly QueryContext _queryContext;
    private readonly MongoPredicateTranslator _predicates;

    public MongoPipelineTranslator(IEntityType entityType, QueryContext queryContext)
    {
        _entityType = entityType;
        _queryContext = queryContext;
        _predicates = new MongoPredicateTranslator(entityType, queryContext);
    }

    public IReadOnlyList<BsonDocument> Translate(Expression? capturedExpression)
    {
        var calls = new List<MethodCallExpression>();
        var node = capturedExpression;
        while (node is MethodCallExpression call)
        {
            calls.Add(call);
            node = call.Arguments.Count > 0 ? call.Arguments[0] : null;
        }
        calls.Reverse(); // root-first

        BsonDocument? match = null;
        BsonDocument? sort = null;
        int? skip = null, limit = null;

        foreach (var call in calls)
        {
            switch (call.Method.Name)
            {
                case "Where":
                    var filter = _predicates.Translate(Unquote(call.Arguments[1]).Body);
                    match = match is null ? filter : new BsonDocument("$and", new BsonArray { match, filter });
                    break;

                case "OrderBy":
                case "ThenBy":
                    AddSort(ref sort, call, ascending: true);
                    break;
                case "OrderByDescending":
                case "ThenByDescending":
                    AddSort(ref sort, call, ascending: false);
                    break;

                case "Skip":
                    skip = Convert.ToInt32(NativeExpressionHelpers.EvaluateValue(call.Arguments[1], _queryContext));
                    break;
                case "Take":
                    limit = Convert.ToInt32(NativeExpressionHelpers.EvaluateValue(call.Arguments[1], _queryContext));
                    break;

                case "Select":
                    // pure projection: ignored -- full docs returned, shaper projects client-side
                    break;

                default:
                    throw new NativeTranslationNotSupportedException($"Native pipeline does not support operator '{call.Method.Name}'.");
            }
        }

        var pipeline = new List<BsonDocument>();
        if (match is not null) pipeline.Add(new BsonDocument("$match", match));
        if (sort is not null) pipeline.Add(new BsonDocument("$sort", sort));
        if (skip is { } s) pipeline.Add(new BsonDocument("$skip", s));
        if (limit is { } l) pipeline.Add(new BsonDocument("$limit", l));
        return pipeline;
    }

    private void AddSort(ref BsonDocument? sort, MethodCallExpression call, bool ascending)
    {
        var key = Unquote(call.Arguments[1]);
        var body = key.Body is UnaryExpression { NodeType: ExpressionType.Convert } u ? u.Operand : key.Body;
        if (!NativeExpressionHelpers.TryResolveMemberProperty(body, _entityType, out _, out var element))
            throw new NativeTranslationNotSupportedException($"Unsupported sort key: {body}.");
        sort ??= new BsonDocument();
        sort.Add(element, ascending ? 1 : -1);
    }

    private static LambdaExpression Unquote(Expression e)
        => e is UnaryExpression { NodeType: ExpressionType.Quote } q ? (LambdaExpression)q.Operand : (LambdaExpression)e;
}
