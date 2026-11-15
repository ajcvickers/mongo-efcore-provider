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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// The structure of a projected element leaf over a cross-document collection, as nav-expansion lowers it:
/// <c>ToList(Where(DbSet&lt;T&gt;, correlation) [.Where(f)] [.OrderBy(k).ThenBy(k)])</c> or the same chain under
/// <c>FirstOrDefault</c> (a predicate argument is hoisted into a <c>Where</c> by nav-expansion).
/// </summary>
internal sealed class ReferencedElementShape
{
    /// <summary>The operator the leaf ends in.</summary>
    public required ElementTerminal Terminal { get; init; }

    /// <summary>Whether the leaf is a list (<c>ToList</c>) rather than one element.</summary>
    public bool IsList => Terminal == ElementTerminal.ToList;

    /// <summary>Whether the leaf is <c>Last</c>/<c>LastOrDefault</c>, the first element of the reversed ordering.</summary>
    public bool ReversesSort => Terminal is ElementTerminal.Last or ElementTerminal.LastOrDefault;

    /// <summary>The <c>ElementAt</c> index (a constant or a query parameter), or <see langword="null"/>.</summary>
    public Expression? ElementIndex { get; init; }

    /// <summary>The query root of the subquery's entity type.</summary>
    public required EntityQueryRootExpression Root { get; init; }

    /// <summary>The <c>Where</c> directly over <see cref="Root"/>: the correlation with the outer entity.</summary>
    public required LambdaExpression Correlation { get; init; }

    /// <summary>The uncorrelated element filters, innermost first. Moving one ahead of the ordering does not change the result.</summary>
    public required IReadOnlyList<LambdaExpression> Filters { get; init; }

    /// <summary>The ordering keys, primary first.</summary>
    public required IReadOnlyList<(LambdaExpression Key, bool Ascending)> Sorts { get; init; }

    /// <summary>
    /// The element selector of <c>Select(o =&gt; o.Total).ToList()</c>, or <see langword="null"/> for a list of the entities themselves.
    /// A single-element terminal carries one too (<c>Select(o =&gt; o.Total).First()</c>), as nav-expansion lowers <c>First().Total</c>.
    /// </summary>
    public LambdaExpression? Selector { get; init; }

    /// <summary>
    /// What <see cref="Selector"/> does with the element it runs over. A selector that only reads mapped scalar properties of it can run over an element
    /// that is not the tracked instance, so the leaf reads the stored row (see <see cref="ElementSelectorKind.ScalarMembersOnly"/>).
    /// </summary>
    public ElementSelectorKind SelectorKind { get; init; }

    /// <summary>The <c>Skip</c> count applied after the ordering (a constant or a query parameter), or <see langword="null"/>.</summary>
    public Expression? Skip { get; init; }

    /// <summary>The <c>Take</c> count applied after the skip (a constant or a query parameter; a <c>ToList</c> only), or <see langword="null"/>.</summary>
    public Expression? Take { get; init; }
}

/// <summary>What the selector of an element leaf does with the element it runs over.</summary>
internal enum ElementSelectorKind
{
    /// <summary>There is no selector: the leaf is the element itself.</summary>
    None,

    /// <summary>
    /// Every use of the element is the read of one of its mapped scalar properties (<c>m.Method</c>, <c>m.Rank + 1</c>, <c>new { m.Method, m.Rank }</c>),
    /// so the selector's result depends only on the stored values it reads. It may run over an element materialized outside the change tracker: a
    /// relational provider reads such values from the columns it fetched, never from an instance the context already tracks.
    /// </summary>
    ScalarMembersOnly,

    /// <summary>
    /// The selector uses the element in any other way (passes it to a method, keeps it in the result, reads a navigation, an owned or complex member,
    /// or a shadow property). It runs over the element as a tracking query materializes it: resolved against the change tracker, so on a tracked row
    /// it is the tracked instance with its unsaved changes, as for any entity a tracking query returns (EF Core's identity resolution never overwrites
    /// a tracked instance with what the query read). The scalars such a selector reads from the element are read from that instance too, where a
    /// relational provider would read each of them as a column: a divergence from EF Core, not a rule to rely on.
    /// </summary>
    Other
}

/// <summary>The operator an element leaf ends in.</summary>
internal enum ElementTerminal
{
    ToList,
    First,
    FirstOrDefault,
    Single,
    SingleOrDefault,
    Last,
    LastOrDefault,
    ElementAt,
    ElementAtOrDefault
}

/// <summary>
/// Why the native binder declined an element leaf that <see cref="NativeReferencedElementMatcher.TryMatch"/> recognized: the detail of
/// <c>CoreStrings.TranslationFailedWithDetails</c>, recorded on <see cref="Expressions.MongoQueryExpression.ElementLeafDeclines"/> and thrown by the
/// read side when it reaches the leaf. Element leaves have no driver-LINQ fallback, so the same reason is reported in every query mode.
/// </summary>
internal static class ElementLeafDeclineReasons
{
    public const string EagerLoadedNavigation
        = "The element type has an eagerly loaded navigation to another collection, which is not loaded for the elements of a projected collection.";

    public const string OwnedNavigations
        = "The element type has owned navigations, which are not supported for the elements of a projected collection.";

    public const string CapturedValueInSelector
        = "The element selector reads a captured value; a selector over the elements of a projected collection can only read the element.";

    public const string OuterEntityInSelector
        = "The element selector reads the outer entity; a selector over the elements of a projected collection can only read the element.";

    public const string SelectorNotSelfContained
        = "The element selector reads an owned member of the element or uses EF.Property, which a selector over the elements of a projected collection cannot.";

    public const string Correlation
        = "The collection is not correlated with the outer entity by an equality of stored-alike key properties.";

    public const string OuterCondition
        = "A condition on the outer entity in the correlation does not compare a default-stored property with a constant or query parameter.";

    public const string SortKey
        = "An ordering key is not a scalar property of the element, stored in its default form, whose stored order is its CLR order.";

    public const string Paging
        = "The Skip, Take or ElementAt count is not a constant or a query parameter.";

    public const string DerivedElementType
        = "The element type is a derived type of a hierarchy, which the lookup of a projected collection cannot narrow by discriminator.";

    public const string ElementFilterReadsOuterEntity
        = "An element filter reads the outer entity, a second correlation.";

    public const string ElementFilter
        = "An element filter could not be translated.";

    public const string LookupConflict
        = "The lookup of the collection conflicts with another lookup of the query.";
}

/// <summary>
/// Recognizes <see cref="ReferencedElementShape"/>. Purely structural and side-effect free, so the emit side
/// (<see cref="NativeProjectionBinder"/>) and the read side (<c>MongoProjectionBindingExpressionVisitor</c>) can both call it; the
/// emit side alone decides admissibility and records the leaf on the lookup it staged.
/// </summary>
internal static class NativeReferencedElementMatcher
{
    internal static bool TryMatch(Expression leaf, [NotNullWhen(true)] out ReferencedElementShape? shape)
    {
        shape = null;

        ElementTerminal terminal;
        Expression? elementIndex = null;
        Expression source;
        switch (leaf)
        {
            case MethodCallExpression
            {
                Method: { Name: nameof(Enumerable.ToList), DeclaringType: var toListDeclaring },
                Arguments: [var toListSource]
            } when toListDeclaring == typeof(Enumerable):
                terminal = ElementTerminal.ToList;
                source = toListSource;
                break;

            case MethodCallExpression
            {
                Method: { DeclaringType: var terminalDeclaring } terminalMethod,
                Arguments: [var terminalSource]
            } when terminalDeclaring == typeof(Queryable) && TryGetTerminal(terminalMethod.Name, out terminal):
                source = terminalSource;
                break;

            // ElementAt(index): the index is a constant or a query parameter, like a Skip count.
            case MethodCallExpression
            {
                Method: { Name: nameof(Queryable.ElementAt) or nameof(Queryable.ElementAtOrDefault), DeclaringType: var elementAtDeclaring } elementAtMethod,
                Arguments: [var elementAtSource, var indexArgument]
            } when elementAtDeclaring == typeof(Queryable)
                   && (indexArgument is ConstantExpression { Value: int } || NativeQueryParameter.TryGetQueryParameterName(indexArgument, out _)):
                terminal = elementAtMethod.Name == nameof(Queryable.ElementAt) ? ElementTerminal.ElementAt : ElementTerminal.ElementAtOrDefault;
                elementIndex = indexArgument;
                source = elementAtSource;
                break;

            default:
                return false;
        }

        var isList = terminal == ElementTerminal.ToList;

        // `Select(o => o.Total).ToList()` or `.First()`: the selector is applied to the materialized entities. An identity selector (what a
        // ThenInclude leaves behind) is none.
        LambdaExpression? selector = null;
        if (source is MethodCallExpression
            {
                Method: { Name: nameof(Queryable.Select), DeclaringType: var selectDeclaring },
                Arguments: [var selectSource, var selectorArgument]
            }
            && selectDeclaring == typeof(Queryable)
            && selectorArgument is UnaryExpression { NodeType: ExpressionType.Quote } or LambdaExpression
            && selectorArgument.UnwrapLambdaFromQuote() is { Parameters.Count: 1 } selectLambda)
        {
            source = selectSource;
            if (selectLambda.Body != selectLambda.Parameters[0])
                selector = selectLambda;
        }

        // Paging, outermost: `OrderBy(..).Skip(1).Take(2).ToList()`, each count a constant or a query parameter. A Where above it would filter
        // the page, not the group, and is declined by the loop below (it meets Skip/Take where it expects a filter or an ordering).
        Expression? take = null;
        Expression? skip = null;
        if (isList && TryPeelPaging(ref source, nameof(Queryable.Take), out var takeCount))
            take = takeCount;

        if (TryPeelPaging(ref source, nameof(Queryable.Skip), out var skipCount))
            skip = skipCount;

        // A constant Take keeps a positive count and a constant Skip is not negative; a parameter's value is checked per execution, where
        // zero is an empty list and a negative count throws, as in LINQ.
        if (take is ConstantExpression { Value: int constantTake and <= 0 }
            || skip is ConstantExpression { Value: int constantSkip and < 0 }
            || elementIndex is ConstantExpression { Value: int constantIndex and < 0 })
        {
            return false;
        }

        // The index is the skip, so a Skip already in the chain (the index counts from its result) is not expressible as one count.
        if (elementIndex is not null && skip is not null)
            return false;

        var wheres = new List<LambdaExpression>();
        var sorts = new List<(LambdaExpression Key, bool Ascending)>();
        var primarySorts = 0;
        var previousWasWhere = false;
        var current = source;

        while (current is not EntityQueryRootExpression)
        {
            // Only the operators below take a lambda second argument; Concat/Union/Except/... take a query, which must not be unwrapped as one.
            if (current is not MethodCallExpression
                {
                    Method: { DeclaringType: var declaring, Name: nameof(Queryable.Where) or nameof(Queryable.OrderBy)
                        or nameof(Queryable.OrderByDescending) or nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending) } method
                } call
                || declaring != typeof(Queryable)
                || call.Arguments is not [var next, var lambdaArgument]
                || lambdaArgument is not (UnaryExpression { NodeType: ExpressionType.Quote } or LambdaExpression)
                || lambdaArgument.UnwrapLambdaFromQuote() is not { Parameters.Count: 1 } lambda)
            {
                return false;
            }

            switch (method.Name)
            {
                case nameof(Queryable.Where):
                    wheres.Add(lambda);
                    previousWasWhere = true;
                    break;
                case nameof(Queryable.OrderBy):
                    primarySorts++;
                    sorts.Add((lambda, true));
                    previousWasWhere = false;
                    break;
                case nameof(Queryable.OrderByDescending):
                    primarySorts++;
                    sorts.Add((lambda, false));
                    previousWasWhere = false;
                    break;
                case nameof(Queryable.ThenBy):
                    sorts.Add((lambda, true));
                    previousWasWhere = false;
                    break;
                case nameof(Queryable.ThenByDescending):
                    sorts.Add((lambda, false));
                    previousWasWhere = false;
                    break;
                default:
                    // Select, Skip, Take, Distinct, ...: not an element leaf.
                    return false;
            }

            current = next;
        }

        // The correlation is the Where the subquery starts with; a second OrderBy would re-sort from scratch.
        if (!previousWasWhere || wheres.Count == 0 || primarySorts > 1)
            return false;

        // Last is the first element of the reversed ordering, which needs an ordering to reverse and no paging ahead of it.
        if (terminal is ElementTerminal.Last or ElementTerminal.LastOrDefault && (sorts.Count == 0 || skip is not null))
            return false;

        // Collected outermost first.
        sorts.Reverse();
        wheres.Reverse();

        shape = new ReferencedElementShape
        {
            Terminal = terminal,
            ElementIndex = elementIndex,
            Root = (EntityQueryRootExpression)current,
            Correlation = wheres[0],
            Filters = wheres.Skip(1).ToList(),
            Sorts = sorts,
            Selector = selector,
            SelectorKind = ClassifySelector(selector, ((EntityQueryRootExpression)current).EntityType),
            Skip = skip,
            Take = take
        };
        return true;
    }

    /// <summary>
    /// Classifies an element selector, of both leaf kinds: a referenced element leaf's (whose elements the read side materializes outside the change
    /// tracker for a <see cref="ElementSelectorKind.ScalarMembersOnly"/> selector) and an owned collection's element <c>Select</c>
    /// (<see cref="NativeProjectionBinder.TryMatchOwnedArraySelectLeaf"/>, evaluated over the stored element documents with no owned entity built,
    /// <see cref="Expressions.OwnedElementStoredSelectExpression"/>). Either way the leaf returns the stored values it reads, as a relational provider
    /// returns the columns it fetched. It decides only how the elements are read, never whether the query translates, so a selector of the other kind
    /// is not refused: it sees the tracked instance.
    /// </summary>
    internal static ElementSelectorKind ClassifySelector(LambdaExpression? selector, IEntityType elementType)
    {
        if (selector is null)
            return ElementSelectorKind.None;

        var checker = new ScalarMemberReadChecker(selector.Parameters[0], elementType);
        checker.Visit(selector.Body);
        return checker.OnlyScalarMembers ? ElementSelectorKind.ScalarMembersOnly : ElementSelectorKind.Other;
    }

    private sealed class ScalarMemberReadChecker(ParameterExpression element, IEntityType elementType) : ExpressionVisitor
    {
        public bool OnlyScalarMembers { get; private set; } = true;

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression == element)
            {
                // A navigation and an owned or complex member are not mapped scalar CLR properties of the element. (A shadow property has no CLR
                // member: it is read through EF.Property, which passes the element as an argument and so is not a member read.)
                if (elementType.FindProperty(node.Member) is not { } property || IsOwnedKeyOrOwnerKey(property))
                    OnlyScalarMembers = false;

                return node;
            }

            return base.VisitMember(node);
        }

        // An owned element's primary key and ownership foreign key are synthesized by the entity path (IsOwnedTypeKey: element name ""), not
        // stored, so a stored-value read would find no element: such a read builds the element (Other). A referenced element's key is stored.
        private bool IsOwnedKeyOrOwnerKey(IProperty property)
            => elementType.FindOwnership() is { } ownership
               && (property.IsPrimaryKey() || ownership.Properties.Contains(property));

        // Any use of the element other than as the receiver of a member read above.
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == element)
                OnlyScalarMembers = false;

            return node;
        }
    }

    private static bool TryGetTerminal(string methodName, out ElementTerminal terminal)
    {
        terminal = methodName switch
        {
            nameof(Queryable.First) => ElementTerminal.First,
            nameof(Queryable.FirstOrDefault) => ElementTerminal.FirstOrDefault,
            nameof(Queryable.Single) => ElementTerminal.Single,
            nameof(Queryable.SingleOrDefault) => ElementTerminal.SingleOrDefault,
            nameof(Queryable.Last) => ElementTerminal.Last,
            nameof(Queryable.LastOrDefault) => ElementTerminal.LastOrDefault,
            _ => ElementTerminal.ToList
        };

        return terminal != ElementTerminal.ToList;
    }

    private static bool TryPeelPaging(ref Expression source, string methodName, [NotNullWhen(true)] out Expression? count)
    {
        count = null;

        if (source is MethodCallExpression
            {
                Method: { DeclaringType: var declaring } method,
                Arguments: [var inner, var argument]
            }
            && declaring == typeof(Queryable)
            && method.Name == methodName
            && (argument is ConstantExpression { Value: int } || NativeQueryParameter.TryGetQueryParameterName(argument, out _)))
        {
            source = inner;
            count = argument;
            return true;
        }

        return false;
    }
}
