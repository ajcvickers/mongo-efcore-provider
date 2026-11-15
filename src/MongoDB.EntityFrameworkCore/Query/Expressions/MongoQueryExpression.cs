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
/// Represents a top-level MongoDB-specific collection for querying server-side.
/// </summary>
internal sealed partial class MongoQueryExpression : Expression
{
    private Dictionary<ProjectionMember, Expression> _projectionMapping = new();
    private readonly List<ProjectionExpression> _projection = [];

    /// <summary>
    /// Create a <see cref="MongoQueryExpression"/> for the given entity type.
    /// </summary>
    /// <param name="entityType">The <see cref="IEntityType"/> this collection relates to.</param>
    public MongoQueryExpression(IEntityType entityType)
    {
        CollectionExpression = new MongoCollectionExpression(entityType);
        _projectionMapping[new ProjectionMember()] =
            new EntityProjectionExpression(entityType, new RootReferenceExpression(entityType));
    }

    /// <summary>
    /// Represents the Mongo collection this query is bound to.
    /// </summary>
    public MongoCollectionExpression CollectionExpression { get; private set; }

    /// <summary>The native-translation logical query IR for this collection. Get-only: callers mutate its members, never reassign it.</summary>
    public MongoSelectDefinition Select { get; } = new();

    /// <summary>
    /// The <see cref="Expression"/> captured from the original EF-bound LINQ query.
    /// </summary>
    public Expression? CapturedExpression { get; set; }

    /// <inheritdoc />
    public override Type Type
        => typeof(object);

    /// <inheritdoc />
    public override ExpressionType NodeType
        => ExpressionType.Extension;

    /// <summary>
    /// Clears the read-side projection list when a GroupBy composes on an already-finalized prior grouping. The prior
    /// stage's entries are dead, and left in place they collide with the second stage's identically-named aliases
    /// ("Key"), so the de-dup renames them to "Key0", a field the pipeline never emits.
    /// </summary>
    internal void ClearReadProjectionForNestedGroupBy()
        => _projection.Clear();

    public int AddToProjection(Expression expression, string? alias = null)
    {
        var existingIndex = _projection.FindIndex(pe => pe.Expression.Equals(expression));
        if (existingIndex != -1)
        {
            return existingIndex;
        }

        var baseAlias = alias ?? (expression as IAccessExpression)?.Name;

        var currentAlias = baseAlias;
        var counter = 0;
        while (_projection.Any(pe => string.Equals(pe.Alias, currentAlias, StringComparison.OrdinalIgnoreCase)))
        {
            currentAlias = $"{baseAlias}{counter++}";
        }

        _projection.Add(new ProjectionExpression(expression, currentAlias, false));

        return _projection.Count - 1;
    }

    public Expression GetMappedProjection(ProjectionMember projectionMember)
        => _projectionMapping[projectionMember];

    /// <summary>Re-points the query's ROOT <see cref="ProjectionMember"/> at a fresh root pair for <paramref name="entityType"/>.</summary>
    /// <remarks>
    /// For the bare whole-inner-element <c>SelectMany</c> (<c>MongoUnwindSource.WholeElement</c>): after
    /// <c>$unwind</c> + <c>$replaceRoot</c> the element is the root document; left mapped to the outer entity,
    /// <c>BindNavigation</c> throws. Call before <c>MongoProjectionBindingExpressionVisitor.Translate</c> runs for
    /// the trailing selector.
    /// </remarks>
    public void ReRootProjectionAt(IEntityType entityType)
        => _projectionMapping[new ProjectionMember()] =
            new EntityProjectionExpression(entityType, new RootReferenceExpression(entityType));

    public IReadOnlyList<ProjectionExpression> Projection
        => _projection;

    public void ApplyProjection()
    {
        // Not a plain "if (Projection.Any()) return;": some native routes call AddToProjection directly yet leave
        // ordinary _projectionMapping entries unflattened, and GetProjectionIndex then throws on the non-constant entry:
        //  - Route == Projection (a reference-collection-nav list leaf registers its own array shaper; native
        //    wrapped-join projections build their shaper by index, so flattening is inert).
        //  - HasClientWrappedWholeEntityShaper: a scalar sibling of the whole-entity operand still needs flattening.
        // Don't widen to other routes: under Fallback (e.g. Custom_projection_reference_navigation_PK_to_FK_
        // optimization) flattening lets a query that must fail translation silently succeed.
        if (Projection.Any()
            && (_projectionMapping.Count == 0
                || (Select.Route != NativeRoute.Projection && !Select.HasClientWrappedWholeEntityShaper)))
        {
            return;
        }

        Dictionary<ProjectionMember, Expression> result = new();
        foreach (var (projectionMember, expression) in _projectionMapping)
        {
            // Honor an alias override so the emitted $project key matches what the shaper reads (a bare selector body's
            // member has no name). Also under IsDistinct: the flatten $project re-emits the original aliases, so skipping
            // the override would null a bare body's alias and crash the shaper.
            var memberName = projectionMember.Last?.Name;
            var alias = _explicitProjectionAliases.TryGetValue(projectionMember, out var explicitAlias)
                ? explicitAlias
                : (Select.Route == NativeRoute.Projection || Select.IsDistinct)
                  && Select.TryGetProjectionAlias(memberName, out var overriddenAlias)
                    ? overriddenAlias
                    : memberName ?? TryGetNaturalMemberAlias(expression);

            result[projectionMember] = Constant(
                explicitAlias is not null ? AddExplicitlyAliasedProjection(expression, explicitAlias) : AddToProjection(expression, alias));
        }

        _projectionMapping = result;
    }

    /// <summary>
    /// Alias for a <c>_projectionMapping</c> entry with no member name (e.g. a plain member read inside a client-only
    /// conditional). A null alias means "read the whole document" and would collide with the whole-entity operand.
    /// </summary>
    private static string? TryGetNaturalMemberAlias(Expression expression)
    {
        if (expression is not MemberExpression { Member: PropertyInfo property } memberExpression)
        {
            return null;
        }

        return memberExpression.Expression?.UnwrapIncludes()
                is StructuralTypeShaperExpression { StructuralType: IReadOnlyEntityType entityType }
            ? entityType.FindProperty(property)?.GetElementName() ?? property.Name
            : property.Name;
    }

    /// <summary>
    /// Projection members under which a member-less construction's arguments were each registered (see
    /// <c>MongoProjectionBindingExpressionVisitor.VisitNew</c>), keyed to the constructed type. Read from whole
    /// documents every such argument would read as the last one registered, so the mixed reader declines them.
    /// Replaced by every projection binding.
    /// </summary>
    internal IReadOnlyDictionary<ProjectionMember, Type> AliasedConstructionMembers { get; private set; }
        = new Dictionary<ProjectionMember, Type>();

    internal void ReplaceAliasedConstructionMembers(IReadOnlyDictionary<ProjectionMember, Type> aliasedConstructionMembers)
        => AliasedConstructionMembers = new Dictionary<ProjectionMember, Type>(aliasedConstructionMembers);

    private readonly List<(Expression Leaf, string Reason)> _elementLeafDeclines = [];
    private readonly List<Expression> _elementLeafDeclineKeys = [];

    /// <summary>
    /// The element leaves <c>NativeReferencedElementMatcher.TryMatch</c> recognized and the native binder then declined, each with the reason
    /// (<c>ElementLeafDeclineReasons</c>). Diagnostic only: written by the emit side, read by the binding visitor to name the unsupported part when
    /// the leaf fails to bind, so it never changes which queries translate.
    /// </summary>
    internal IReadOnlyList<(Expression Leaf, string Reason)> ElementLeafDeclines
        => _elementLeafDeclines;

    /// <summary>
    /// Records why the native binder declined <paramref name="leaf"/>, a leaf of the selector whose parameter is <paramref name="outerParameter"/>.
    /// The first reason recorded for a leaf is kept.
    /// </summary>
    internal void RecordElementLeafDecline(Expression leaf, ParameterExpression outerParameter, string reason)
    {
        var key = ElementLeafDeclineKey.FromEmitSide(leaf, outerParameter);
        if (FindElementLeafDeclineIndex(key) >= 0)
            return;

        _elementLeafDeclines.Add((leaf, reason));
        _elementLeafDeclineKeys.Add(key);
    }

    /// <summary>
    /// The decline recorded for <paramref name="leaf"/> as the binding visitor sees it (the selector's parameter replaced by the source's
    /// shaper), or <see langword="null"/>.
    /// </summary>
    internal (Expression Leaf, string Reason)? FindElementLeafDecline(Expression leaf)
    {
        if (_elementLeafDeclines.Count == 0)
            return null;

        var index = FindElementLeafDeclineIndex(ElementLeafDeclineKey.FromReadSide(leaf));
        return index >= 0 ? _elementLeafDeclines[index] : null;
    }

    private int FindElementLeafDeclineIndex(Expression key)
        => _elementLeafDeclineKeys.FindIndex(k => ExpressionEqualityComparer.Instance.Equals(k, key));

    /// <summary>
    /// One form of an element leaf for both sides: the emit side sees the selector's parameter where the read side sees the shaper EF
    /// substituted for it, so each is replaced by a parameter of the same type and a fixed name (the structural comparer compares a free
    /// parameter by name and type). A leaf over a join scope (a transparent-identifier parameter) is not matched, and its failure keeps the
    /// plain message.
    /// </summary>
    private sealed class ElementLeafDeclineKey : ExpressionVisitor
    {
        private const string OuterName = "<element-leaf-outer>";

        private readonly ParameterExpression? _outerParameter;

        private ElementLeafDeclineKey(ParameterExpression? outerParameter)
            => _outerParameter = outerParameter;

        public static Expression FromEmitSide(Expression leaf, ParameterExpression outerParameter)
            => Normalize(leaf, outerParameter);

        public static Expression FromReadSide(Expression leaf)
            => Normalize(leaf, null);

        // An extension node that neither reduces nor visits its children throws from the base visitor: such a leaf keeps its own form, so
        // it matches only itself and its failure keeps the plain message.
        private static Expression Normalize(Expression leaf, ParameterExpression? outerParameter)
        {
            try
            {
                return new ElementLeafDeclineKey(outerParameter).Visit(leaf);
            }
            catch (ArgumentException)
            {
                return leaf;
            }
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => _outerParameter is not null && node == _outerParameter ? Parameter(node.Type, OuterName) : node;

        protected override Expression VisitExtension(Expression node)
            => _outerParameter is null && node is StructuralTypeShaperExpression
                ? Parameter(node.Type, OuterName)
                : base.VisitExtension(node);
    }

    private Dictionary<ProjectionMember, string> _explicitProjectionAliases = new();

    // An explicit alias names one staged read, which several members may share (`Id = x.Name, Name = x.Name`): reuse
    // its projection rather than let AddToProjection's de-dup rename the second registration to an element the
    // pipeline never emits. Any other outcome is an emit/read disagreement, so throw rather than read a missing field.
    private int AddExplicitlyAliasedProjection(Expression expression, string explicitAlias)
    {
        var existingIndex = _projection.FindIndex(pe => pe.Alias == explicitAlias);
        if (existingIndex != -1)
        {
            return existingIndex;
        }

        var index = AddToProjection(expression, explicitAlias);
        return _projection[index].Alias == explicitAlias
            ? index
            : throw new InvalidOperationException(
                $"Projection alias '{explicitAlias}' was renamed to '{_projection[index].Alias}'; the native pipeline emits only '{explicitAlias}'.");
    }

    /// <summary>
    /// Aliases <see cref="ApplyProjection"/> uses instead of deriving one from the member name: a client-evaluated
    /// ternary's branch reads, whose alias the emit side chose. Replaced by every projection binding.
    /// </summary>
    internal void ReplaceExplicitProjectionAliases(IReadOnlyDictionary<ProjectionMember, string> explicitAliases)
        => _explicitProjectionAliases = new Dictionary<ProjectionMember, string>(explicitAliases);

    public void ReplaceProjectionMapping(IDictionary<ProjectionMember, Expression> projectionMapping)
    {
        _projectionMapping.Clear();
        foreach (var (projectionMember, expression) in projectionMapping)
        {
            _projectionMapping[projectionMember] = expression;
        }
    }
}
