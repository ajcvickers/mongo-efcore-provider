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
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

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
    /// <param name="property">
    /// The target: a mapped scalar property (of the root entity or, through single complex-property hops, of a complex
    /// type), or a whole complex property / complex collection.
    /// </param>
    /// <param name="valueExpression">The value (a constant/parameter, or an expression over the entity).</param>
    /// <param name="isSelfReferencing">Whether <paramref name="valueExpression"/> reads the entity being updated.</param>
    /// <param name="elementPath">
    /// The stored (dotted) path of the target, or <see langword="null"/> for a root scalar property, whose path is its
    /// element name.
    /// </param>
    public sealed class Setter(IPropertyBase property, Expression valueExpression, bool isSelfReferencing, string? elementPath = null)
    {
        /// <summary>The target property (see the type documentation).</summary>
        public IPropertyBase Property { get; } = property;

        /// <summary>The value (a constant/parameter, or an expression over the entity).</summary>
        public Expression ValueExpression { get; } = valueExpression;

        /// <summary>Whether <see cref="ValueExpression"/> reads the entity being updated.</summary>
        public bool IsSelfReferencing { get; } = isSelfReferencing;

        /// <summary>The stored (dotted) path of the target, or <see langword="null"/> for a root scalar property.</summary>
        public string? ElementPath { get; } = elementPath;

        /// <summary>The stored path the update writes.</summary>
        public string StoredPath => ElementPath ?? ((IReadOnlyProperty)Property).GetElementName();

        // Lazily populated by the compile-and-evaluate fallback in the execution path. This node is baked into the compiled query as a
        // constant, so the same instance serves every execution and the delegate need only be built once. What is cached is the DELEGATE,
        // never a value: it is a parameterless Func over an immutable tree that reads any external state when invoked, so reusing it is
        // behaviour-identical to compiling afresh. This is a class rather than a record because a field declared in a record body takes
        // part in synthesized equality. Volatile so a racing thread can never observe the reference before the delegate is fully built
        // (a duplicate build is harmless; a half-published delegate is not).
        internal volatile Func<object?>? CompiledValue;
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
    /// <para>
    /// An outermost auto-include wrapper is unwrapped too (<see cref="StripIncludeWrapper"/>), so every consumer of the source chain
    /// (classification, filter, two-phase id query, null-element scan) sees the same chain.
    /// </para>
    /// </summary>
    public static Expression? UnwrapBulkOperator(Expression? capturedExpression)
        => StripIncludeWrapper(
            capturedExpression is MethodCallExpression { Method: { DeclaringType: var declaringType, Name: var name } } call
            && declaringType == typeof(EntityFrameworkQueryableExtensions)
            && Array.IndexOf(BulkOperatorNames, name) >= 0
                ? call.Arguments[0]
                : capturedExpression);

    /// <summary>
    /// Removes the auto-include wrapper that navigation expansion leaves as the OUTERMOST operator when the entity type has an included
    /// navigation: always for ExecuteDelete, and for ExecuteUpdate on EF9 (#69). It only loads related data into the result, so it does
    /// not change which documents match, and a bulk operation returns none of it. Two shapes, matched narrowly (anything else stays in the
    /// chain for the classifier to reject):
    /// <list type="bullet">
    /// <item>
    /// <c>Select(e => Include(e, ...))</c>, an embedded (owned) navigation: a <c>Select</c> whose lambda body is nothing but
    /// <see cref="IncludeExpression"/>s over the lambda's own parameter.
    /// </item>
    /// <item>
    /// <c>LeftJoin(source, DbSet&lt;Related&gt;, ...).Select(ti => Include(ti.Outer, nav, ti.Inner))</c>, a REFERENCE navigation to another
    /// collection: a left-outer join (so a document whose reference is missing is still in the set, and one match at most, since it is
    /// a reference) that only feeds the include. Stripping both leaves <c>source</c>. An inner <c>Join</c>, which a required navigation may
    /// produce, is NOT accepted: it can drop documents whose reference dangles, which would change the affected set.
    /// </item>
    /// </list>
    /// Several included reference navigations arrive as ONE <c>Select</c> of nested includes over nested <c>LeftJoin</c>s, handled the same
    /// way. Repeated while the chain still starts with one wrapper.
    /// </summary>
    private static Expression? StripIncludeWrapper(Expression? expression)
    {
        while (TryStripIncludeWrapper(expression, out var source))
        {
            expression = source;
        }

        return expression;
    }

    private static bool TryStripIncludeWrapper(Expression? expression, out Expression? source)
    {
        source = null;
        if (expression is not MethodCallExpression { Method: { IsGenericMethod: true } method, Arguments: [var select, var selector] }
            || method.GetGenericMethodDefinition() != QueryableMethods.Select
            || selector.UnwrapLambdaFromQuote() is not { Parameters: [var parameter], Body: IncludeExpression outermost })
        {
            return false;
        }

        // The nested includes, outermost first, down to the entity they all include onto.
        var includes = new List<IncludeExpression>();
        Expression entity = outermost;
        while (entity is IncludeExpression nested)
        {
            includes.Add(nested);
            entity = nested.EntityExpression;
        }

        if (entity == parameter)
        {
            source = select;
            return true;
        }

        // n included reference navigations: the entity is p.Outer (n times) and include j reads p.Outer (j times).Inner, over n nested
        // LeftJoins that only feed those includes.
        var count = includes.Count;
        if (!parameter.Type.IsTransparentIdentifierType() || !IsOuterPath(entity, parameter, count))
        {
            return false;
        }

        for (var j = 0; j < count; j++)
        {
            if (includes[j].Navigation.IsCollection
                || includes[j].NavigationExpression is not MemberExpression { Member.Name: "Inner", Expression: { } owner }
                || !IsOuterPath(owner, parameter, j))
            {
                return false;
            }
        }

        var current = select;
        for (var j = 0; j < count; j++)
        {
            if (current is not MethodCallExpression
                {
                    Method: { Name: "LeftJoin", IsGenericMethod: true } join,
                    Arguments: [var joinSource, QueryRootExpression, _, _, var resultSelector]
                }
                || !(join.DeclaringType == typeof(Queryable) || join.DeclaringType?.Assembly == typeof(DbContext).Assembly)
                || !IsTransparentIdentifierOfParameters(resultSelector.UnwrapLambdaFromQuote()))
            {
                return false;
            }

            current = joinSource;
        }

        source = current;
        return true;
    }

    // parameter.Outer.Outer... (count times); count 0 is the parameter itself.
    private static bool IsOuterPath(Expression expression, ParameterExpression parameter, int count)
    {
        while (count > 0)
        {
            if (expression is not MemberExpression { Member.Name: "Outer" } member || !member.IsTransparentIdentifierOuterOrInnerAccess())
            {
                return false;
            }

            expression = member.Expression!;
            count--;
        }

        return expression == parameter;
    }

    // (o, i) => new TransparentIdentifier<TOuter, TInner>(o, i): the plain pairing EF writes for a join feeding an include.
    private static bool IsTransparentIdentifierOfParameters(LambdaExpression selector)
        => selector is { Parameters: [var outer, var inner], Body: NewExpression { Arguments: [var first, var second] } body }
           && body.Type.IsTransparentIdentifierType()
           && first == outer
           && second == inner;

    // The canonical single-key / parameterless overloads. Matched by method definition, not by name (Query/AGENTS.md: reference-equality on
    // MethodInfo), so overloads with different semantics are not admitted: OrderBy(keySelector, IComparer<T>) and
    // Distinct(IEqualityComparer<T>) change what is selected or how it is compared, and an indexed Where would leave its index parameter
    // unbound in the filter. EF rejects those before provider code runs today; this keeps that true if it ever loosens.
    private static readonly IReadOnlySet<MethodInfo> AffectedSetNeutralDefinitions = new HashSet<MethodInfo>
    {
        QueryableMethods.OrderBy,
        QueryableMethods.OrderByDescending,
        QueryableMethods.ThenBy,
        QueryableMethods.ThenByDescending,
        QueryableMethods.Distinct
    };

    private static MethodInfo Definition(MethodInfo method)
        => method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;

    /// <summary>Whether this is the canonical <c>Queryable.Where(source, predicate)</c>, which scopes a bulk operation.</summary>
    public static bool IsWhere(MethodInfo method)
        => Definition(method) == QueryableMethods.Where;

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
    public static bool IsAffectedSetNeutral(MethodInfo method)
        => AffectedSetNeutralDefinitions.Contains(Definition(method));

    /// <summary>
    /// Whether this is the canonical <c>Queryable.OfType</c>. It NARROWS the affected set (to one type of a hierarchy), so it is never
    /// affected-set-neutral. Unlike <c>Cast</c> it is a structural type swap the read path translates into a discriminator filter, which
    /// is why a bulk operation over it runs two-phase: phase 1 is that read-path query.
    /// </summary>
    public static bool IsOfType(MethodInfo method)
        => Definition(method) == QueryableMethods.OfType;

    /// <summary>Whether this is the canonical <c>Skip</c> or <c>Take</c>, which change the affected set in a way one server command cannot express.</summary>
    public static bool IsPaging(MethodInfo method)
    {
        var definition = Definition(method);
        return definition == QueryableMethods.Skip || definition == QueryableMethods.Take;
    }

    public override ExpressionType NodeType => ExpressionType.Extension;
    public override System.Type Type => typeof(int);
    public override bool CanReduce => false;

    /// <summary>
    /// This node is an opaque marker: its source query and setters are consumed directly by
    /// <see cref="Visitors.MongoShapedQueryCompilingExpressionVisitor"/> and must not be rewritten by generic visitors,
    /// so no children are visited.
    /// </summary>
    protected override Expression VisitChildren(System.Linq.Expressions.ExpressionVisitor visitor) => this;
}
