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
using Microsoft.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.Query.Expressions;

/// <summary>
/// Marker node produced by ExecuteDelete / ExecuteUpdate translation. Carries the source
/// query (collection + captured predicate chain) and, for updates, the ordered setters.
/// Recognized by <see cref="Visitors.MongoShapedQueryCompilingExpressionVisitor"/>.
/// </summary>
internal sealed class MongoNonQueryExpression : Expression
{
    public enum OperationKind { Delete, Update }

    public enum BulkStrategy { SingleCommand, TwoPhase }

    /// <summary>One <c>SetProperty</c> of a bulk update.</summary>
    /// <param name="Property">
    /// The target: a mapped scalar property (of the root entity or, through single complex-property hops, of a complex
    /// type), or a whole complex property / complex collection.
    /// </param>
    /// <param name="ValueExpression">The value (a constant/parameter, or an expression over the entity).</param>
    /// <param name="IsSelfReferencing">Whether <paramref name="ValueExpression"/> reads the entity being updated.</param>
    /// <param name="ElementPath">
    /// The stored (dotted) path of the target, or <see langword="null"/> for a root scalar property, whose path is its
    /// element name.
    /// </param>
    public sealed record Setter(IPropertyBase Property, Expression ValueExpression, bool IsSelfReferencing, string? ElementPath = null)
    {
        /// <summary>The stored path the update writes.</summary>
        public string StoredPath => ElementPath ?? ((IReadOnlyProperty)Property).GetElementName();
    }

    public MongoNonQueryExpression(MongoQueryExpression sourceQuery, BulkStrategy strategy = BulkStrategy.SingleCommand)
    {
        SourceQuery = sourceQuery;
        Kind = OperationKind.Delete;
        Setters = [];
        Strategy = strategy;
    }

    public MongoNonQueryExpression(
        MongoQueryExpression sourceQuery,
        IReadOnlyList<Setter> setters,
        BulkStrategy strategy = BulkStrategy.SingleCommand)
    {
        SourceQuery = sourceQuery;
        Kind = OperationKind.Update;
        Setters = setters;
        Strategy = strategy;
    }

    public MongoQueryExpression SourceQuery { get; }
    public OperationKind Kind { get; }
    public IReadOnlyList<Setter> Setters { get; }
    public BulkStrategy Strategy { get; }

    private static readonly string[] BulkOperatorNames = ["ExecuteDelete", "ExecuteDeleteAsync", "ExecuteUpdate", "ExecuteUpdateAsync"];

    /// <summary>
    /// Unwraps the leading ExecuteDelete / ExecuteUpdate marker call (declared on
    /// <see cref="EntityFrameworkQueryableExtensions"/>) from a captured method chain, returning the underlying
    /// source query (e.g. <c>root.Where(...)</c>). EF captures the full chain including the bulk operator as the
    /// <see cref="MongoQueryExpression.CapturedExpression"/>; walkers that scope the operation need only the source.
    /// Only the four bulk operators are unwrapped: other methods on that type (AsNoTracking, IgnoreQueryFilters, TagWith, ...) stay in
    /// the chain so the classifier can see and reject them rather than having them silently discarded.
    /// </summary>
    public static Expression? UnwrapBulkOperator(Expression? capturedExpression)
        => capturedExpression is MethodCallExpression { Method: { DeclaringType: var declaringType, Name: var name } } call
           && declaringType == typeof(EntityFrameworkQueryableExtensions)
           && Array.IndexOf(BulkOperatorNames, name) >= 0
            ? call.Arguments[0]
            : capturedExpression;

    /// <summary>
    /// Whether a <see cref="Queryable"/> operator leaves WHICH documents a bulk operation affects unchanged, so it neither forces the
    /// two-phase strategy nor needs translating into the server-side filter. Ordering matters only together with paging, and
    /// <c>Distinct</c> over one collection's entities is a no-op because documents are already distinct by <c>_id</c>.
    /// <para>
    /// The classifier and the filter builder both consult this, so they cannot drift apart. An operator that NARROWS the affected set
    /// (<c>OfType</c>, <c>Cast</c>, <c>Skip</c>, <c>Take</c>) must never be added here: the filter builder would step over it, and
    /// a bulk delete would remove documents the caller did not select.
    /// </para>
    /// </summary>
    public static bool IsAffectedSetNeutral(string operatorName)
        => operatorName is nameof(Queryable.OrderBy)
            or nameof(Queryable.OrderByDescending)
            or nameof(Queryable.ThenBy)
            or nameof(Queryable.ThenByDescending)
            or nameof(Queryable.Distinct);

    /// <summary>Whether a <see cref="Queryable"/> operator changes the affected set in a way one server command cannot express.</summary>
    public static bool IsPaging(string operatorName)
        => operatorName is nameof(Queryable.Skip) or nameof(Queryable.Take);

    public override ExpressionType NodeType => ExpressionType.Extension;
    public override System.Type Type => typeof(int);
    public override bool CanReduce => false;

    protected override Expression VisitChildren(System.Linq.Expressions.ExpressionVisitor visitor) => this;
}
