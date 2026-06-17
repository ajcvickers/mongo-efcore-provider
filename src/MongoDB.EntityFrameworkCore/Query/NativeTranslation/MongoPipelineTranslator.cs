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
/// pipeline of <c>$match</c>/<c>$sort</c>/<c>$skip</c>/<c>$limit</c> stages over whole entities.
/// Throws <see cref="NativeTranslationNotSupportedException"/> on anything else — including a
/// trailing <c>Select</c> projection, which falls back to the driver-LINQ path.
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

        // Each Where becomes its own $match stage (the driver-LINQ path emits one $match per Where rather than
        // merging them), preserving encounter order among the filters.
        var matches = new List<BsonDocument>();
        BsonDocument? sort = null;
        int? skip = null, limit = null;

        // The pipeline is emitted in the fixed order $match -> $sort -> $skip -> $limit. That only matches
        // LINQ semantics for a single, canonically-ordered query: filtering/sorting must precede paging, and
        // paging must be a single Skip optionally followed by a single Take. Anything else (Take before Skip,
        // repeated Skip/Take, filter/sort after paging) composes differently and is rejected so it falls back.
        bool pagingSeen = false;
        foreach (var call in calls)
        {
            switch (call.Method.Name)
            {
                case "Where":
                    if (pagingSeen)
                        throw new NativeTranslationNotSupportedException("Native pipeline does not support Where after Skip/Take.");
                    matches.Add(_predicates.Translate(Unquote(call.Arguments[1]).Body));
                    break;

                case "OrderBy":
                case "ThenBy":
                    if (pagingSeen)
                        throw new NativeTranslationNotSupportedException("Native pipeline does not support ordering after Skip/Take.");
                    AddSort(ref sort, call, ascending: true);
                    break;
                case "OrderByDescending":
                case "ThenByDescending":
                    if (pagingSeen)
                        throw new NativeTranslationNotSupportedException("Native pipeline does not support ordering after Skip/Take.");
                    AddSort(ref sort, call, ascending: false);
                    break;

                case "Skip":
                    // A Skip must come before any Take (Take-then-Skip skips within the limited set) and there
                    // can be only one of each — the single $skip/$limit pair cannot express repeated paging.
                    if (skip is not null || limit is not null)
                        throw new NativeTranslationNotSupportedException("Native pipeline does not support repeated or out-of-order Skip/Take.");
                    skip = Convert.ToInt32(NativeExpressionHelpers.EvaluateValue(call.Arguments[1], _queryContext));
                    pagingSeen = true;
                    break;
                case "Take":
                    if (limit is not null)
                        throw new NativeTranslationNotSupportedException("Native pipeline does not support repeated Take.");
                    limit = Convert.ToInt32(NativeExpressionHelpers.EvaluateValue(call.Arguments[1], _queryContext));
                    // $limit must be positive; the driver-LINQ path validates Take(0) client-side and throws
                    // ArgumentOutOfRangeException. Fall back so that validation (and its empty MQL) is preserved.
                    if (limit <= 0)
                        throw new NativeTranslationNotSupportedException("Native pipeline does not support a non-positive Take.");
                    pagingSeen = true;
                    break;

                case "Select":
                    // Sub-project B's native slice is filter/sort/paging over whole entities only. A trailing
                    // Select may be a server-side projection (e.g. $project with $toString / $dateAdd) that the
                    // driver-LINQ path renders and that the client-side shaper does not reproduce. Silently
                    // dropping it returns the wrong document shape, so reject it and fall back to driver-LINQ.
                    throw new NativeTranslationNotSupportedException(
                        "Native pipeline does not support a trailing projection (Select).");

                default:
                    throw new NativeTranslationNotSupportedException($"Native pipeline does not support operator '{call.Method.Name}'.");
            }
        }

        var pipeline = new List<BsonDocument>();
        foreach (var match in matches) pipeline.Add(new BsonDocument("$match", match));
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
