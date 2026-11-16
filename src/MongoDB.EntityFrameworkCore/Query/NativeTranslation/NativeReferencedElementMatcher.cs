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
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;  // IsEFPropertyMethod()
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.EntityFrameworkCore.Extensions;        // IsEmbedded()

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

    /// <summary>
    /// The navigation of the element to another collection that <see cref="Selector"/> reads (<c>m.Animal.Name</c>), which nav-expansion joins in
    /// below the selector (<c>Join(.., DbSet&lt;Animal&gt;(), ..).Select(ti =&gt; ti.Inner.Name)</c>), or <see langword="null"/>. Recognized only so that
    /// the binder can refuse it naming the navigation: such a leaf is never translated.
    /// </summary>
    public INavigation? JoinedNavigation { get; init; }

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

    public const string OuterEntityInSelector
        = "The element selector reads the outer entity; a selector over the elements of a projected collection can only read the element.";

    public const string SelectorUsesEFProperty
        = "The element selector uses EF.Property, which cannot run over the materialized elements of a projected collection.";

    /// <summary>
    /// The selector reads a navigation of the element to another collection (<c>m.Animal.Name</c>), which nav-expansion turns into a join that
    /// the lookup of a projected collection cannot carry.
    /// </summary>
    public static string SelectorReadsNavigation(string elementTypeName, string navigationName)
        => $"The element selector reads the navigation '{elementTypeName}.{navigationName}'; a navigation of an element of a projected collection is not supported.";

    /// <summary>The neutral reason: the selector contains something that cannot run over the materialized elements, which the binder did not identify further.</summary>
    public const string SelectorNotSelfContained
        = "The element selector contains a part that cannot run over the materialized elements of a projected collection.";

    public const string Correlation
        = "The collection is not correlated with the outer entity by an equality of stored-alike key properties.";

    public const string OuterCondition
        = "A condition on the outer entity in the correlation does not compare a default-stored property with a constant or query parameter.";

    public const string SortKey
        = "An ordering key is not a scalar property of the element, stored in its default form, whose stored order is its CLR order.";

    /// <summary>Defensive; believed unreachable because <see cref="NativeReferencedElementMatcher.TryMatch"/> already requires each count to be a constant or a query parameter.</summary>
    public const string Paging
        = "The Skip, Take or ElementAt count is not a constant or a query parameter.";

    /// <summary>Defensive; believed unreachable because <c>DerivedEntityRootNarrower</c> rewrites <c>Set&lt;Derived&gt;()</c> to <c>Set&lt;Base&gt;().OfType&lt;Derived&gt;()</c> before nav-expansion, which TryMatch does not recognize.</summary>
    public const string DerivedElementType
        = "The element type is a derived type of a hierarchy, which the lookup of a projected collection cannot narrow by discriminator.";

    public const string ElementFilterReadsOuterEntity
        = "An element filter reads the outer entity, a second correlation.";

    public const string ElementFilter
        = "An element filter could not be translated.";

    public const string LookupConflict
        = "The lookup of the collection conflicts with another lookup of the query.";

    /// <summary>
    /// An <c>OrDefault</c> terminal (<c>FirstOrDefault().Rank + 1</c>, a cast over one, a whole <c>FirstOrDefault()</c> under <c>??</c> or
    /// <c>?:</c>) inside an operator, cast or conditional: EF null-propagates a missing element where C# would throw, so the shaper cannot
    /// evaluate the enclosing expression in C#.
    /// </summary>
    public const string OrDefaultMemberUnderOperator
        = "An OrDefault terminal (FirstOrDefault, SingleOrDefault, LastOrDefault, ElementAtOrDefault) inside an operator, cast or conditional is not supported; its missing element would propagate as null where C# would throw.";

    /// <summary>
    /// The leaf is an argument of a construction that names no members (a positional constructor or a container), whose read side binds the
    /// argument by position through the result type's serializer and so cannot run the element selector.
    /// </summary>
    public const string ConstructorArgument
        = "An element of a projected collection cannot be read as an argument of a constructor call that names no members; project it as a member of an anonymous or named type instead.";
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

        // A navigation of the element read in the selector: nav-expansion joins its target in directly below the selector. Recognized only to be
        // refused by name (see ReferencedElementShape.JoinedNavigation); a join anywhere else in the chain is not an element leaf.
        MethodCallExpression? join = null;
        if (selector is not null && TryPeelNavigationJoin(ref current, out var navigationJoin))
            join = navigationJoin;

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

        var root = (EntityQueryRootExpression)current;
        INavigation? joinedNavigation = null;
        if (join is not null)
        {
            if (FindJoinedNavigation(join, root.EntityType) is not { } found)
                return false;

            joinedNavigation = found;
        }
        else if (selector is not null)
        {
            // The owned navigations of the element are in its own document, and the element shaper includes them
            // (CreateElementShaperWithOwnedIncludes): nav-expansion's owned Include and EF.Property spelling of them are read as the element's members.
            selector = NormalizeOwnedNavigationReads(selector, root.EntityType);
        }

        shape = new ReferencedElementShape
        {
            Terminal = terminal,
            ElementIndex = elementIndex,
            Root = root,
            Correlation = wheres[0],
            Filters = wheres.Skip(1).ToList(),
            Sorts = sorts,
            Selector = selector,
            SelectorKind = joinedNavigation is not null ? ElementSelectorKind.Other : ClassifySelector(selector, root.EntityType),
            JoinedNavigation = joinedNavigation,
            Skip = skip,
            Take = take
        };
        return true;
    }

    /// <summary>
    /// Rewrites the reads of the element's owned (embedded) navigations in <paramref name="selector"/> to the plain member reads the user wrote:
    /// nav-expansion spells <c>m.Detail.Label</c> as <c>EF.Property&lt;Detail&gt;(m, "Detail").Label</c> (and a nested owned navigation
    /// the same way off it), and wraps an element it returns in an <see cref="IncludeExpression"/> per owned navigation
    /// (<c>m =&gt; Include(Include(m, Detail), Pieces)</c>). Their data is in the element's own document and the element shaper includes them, so
    /// the include is dropped, and a selector left as the bare element is no selector. Returns <see langword="null"/> for that case. Any other
    /// <c>EF.Property</c> is left as it is (the binder declines it).
    /// </summary>
    private static LambdaExpression? NormalizeOwnedNavigationReads(LambdaExpression selector, IEntityType elementType)
    {
        var body = new OwnedNavigationReadRewriter(selector.Parameters[0], elementType).Visit(selector.Body);
        if (body == selector.Parameters[0])
            return null;

        return body == selector.Body ? selector : Expression.Lambda(body, selector.Parameters);
    }

    private sealed class OwnedNavigationReadRewriter(ParameterExpression element, IEntityType elementType) : ExpressionVisitor
    {
        // The entity type of each owned-navigation read this rewriter produced, so a nested one is resolved against its owner.
        private readonly Dictionary<Expression, IEntityType> _ownedReads = new(ReferenceEqualityComparer.Instance);

        protected override Expression VisitExtension(Expression node)
        {
            // An Include of only embedded navigations around the element: the element shaper includes them already.
            if (node is IncludeExpression include && IsElementUnderEmbeddedIncludes(include))
                return element;

            return base.VisitExtension(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsEFPropertyMethod()
                && node.Arguments is [var receiverArgument, ConstantExpression { Value: string name }])
            {
                var receiver = Visit(receiverArgument).RemoveConvert();
                var receiverType = receiver == element ? elementType : _ownedReads.GetValueOrDefault(receiver);
                if (receiverType?.FindNavigation(name) is { } navigation
                    && navigation.IsEmbedded()
                    && (navigation.PropertyInfo ?? (MemberInfo?)navigation.FieldInfo) is { } member
                    && member.DeclaringType!.IsAssignableFrom(receiver.Type))
                {
                    Expression read = Expression.MakeMemberAccess(receiver, member);
                    _ownedReads[read] = navigation.TargetEntityType;
                    return read.Type == node.Type ? read : Expression.Convert(read, node.Type);
                }
            }

            // A Queryable operator chain over an owned collection read (top-down, so an intermediate operator never has to stand alone).
            if (TryRewriteToEnumerable(node, out var enumerableCall) && node.Type.IsAssignableFrom(enumerableCall.Type))
                return enumerableCall;

            return base.VisitMethodCall(node);
        }

        // Nav-expansion reads an owned collection as `Queryable.X(AsQueryable(<owned read>), ..)`. The shaper evaluates the selector per element
        // and row, and a Queryable operator over an EnumerableQuery compiles an expression tree on every call, so an operator chain over an owned
        // read this rewriter produces becomes the Enumerable overloads with the same name and shape, their quoted lambdas unquoted. The caller keeps
        // the result only when it can stand where the call stood (a terminal such as Count, not an IQueryable the parent expects); an operator
        // with no matching Enumerable overload leaves the chain as it is.
        private bool TryRewriteToEnumerable(MethodCallExpression call, [NotNullWhen(true)] out Expression? enumerableCall)
        {
            enumerableCall = null;
            if (call.Method.DeclaringType != typeof(Queryable)
                || call.Method.Name == nameof(Queryable.AsQueryable)
                || call.Arguments.Count == 0)
            {
                return false;
            }

            Expression source;
            switch (call.Arguments[0])
            {
                case MethodCallExpression { Method.Name: nameof(Queryable.AsQueryable), Arguments: [var asQueryableSource] } asQueryable
                    when asQueryable.Method.DeclaringType == typeof(Queryable):
                    source = Visit(asQueryableSource);
                    if (!_ownedReads.ContainsKey(source.RemoveConvert()))
                        return false;

                    break;

                case MethodCallExpression inner when TryRewriteToEnumerable(inner, out var innerEnumerable):
                    source = innerEnumerable;
                    break;

                default:
                    return false;
            }

            var queryableParameters = call.Method.GetParameters();
            var genericArguments = call.Method.IsGenericMethod ? call.Method.GetGenericArguments() : [];
            foreach (var candidate in typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (candidate.Name != call.Method.Name
                    || candidate.IsGenericMethodDefinition != call.Method.IsGenericMethod
                    || (candidate.IsGenericMethodDefinition && candidate.GetGenericArguments().Length != genericArguments.Length)
                    || candidate.GetParameters().Length != queryableParameters.Length)
                {
                    continue;
                }

                MethodInfo method;
                try
                {
                    method = candidate.IsGenericMethodDefinition ? candidate.MakeGenericMethod(genericArguments) : candidate;
                }
                catch (System.ArgumentException)
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (!parameters[0].ParameterType.IsAssignableFrom(source.Type))
                    continue;

                var matches = true;
                for (var i = 1; matches && i < parameters.Length; i++)
                {
                    var queryableType = queryableParameters[i].ParameterType;
                    matches = parameters[i].ParameterType
                              == (queryableType.IsGenericType && queryableType.GetGenericTypeDefinition() == typeof(Expression<>)
                                  ? queryableType.GetGenericArguments()[0]
                                  : queryableType);
                }

                if (!matches)
                    continue;

                var arguments = new Expression[parameters.Length];
                arguments[0] = source;
                for (var i = 1; i < parameters.Length; i++)
                {
                    var argument = call.Arguments[i] is UnaryExpression { NodeType: ExpressionType.Quote } quoted ? quoted.Operand : call.Arguments[i];
                    arguments[i] = Visit(argument);
                }

                enumerableCall = Expression.Call(method, arguments);
                return true;
            }

            return false;
        }
        private bool IsElementUnderEmbeddedIncludes(IncludeExpression include)
        {
            Expression current = include;
            while (current is IncludeExpression nested)
            {
                if (nested.Navigation is not INavigation navigation || !navigation.IsEmbedded())
                    return false;

                current = nested.EntityExpression;
            }

            return current == element;
        }
    }

    /// <summary>
    /// Peels nav-expansion's join of a navigation the selector reads (an inner <c>Join</c>, or EF10's <c>LeftJoin</c>, onto a query root with a
    /// <c>TransparentIdentifier&lt;TOuter, TInner&gt;</c> result) from the top of the chain.
    /// </summary>
    private static bool TryPeelNavigationJoin(ref Expression source, [NotNullWhen(true)] out MethodCallExpression? join)
    {
        join = null;
        if (source is MethodCallExpression
            {
                Method: { Name: nameof(Queryable.Join) or "LeftJoin", DeclaringType: var declaring },
                Arguments: [var outer, EntityQueryRootExpression, var outerKey, _, var resultSelector]
            } call
            && declaring == typeof(Queryable)
            && outerKey.UnwrapLambdaFromQuote() is { Parameters.Count: 1 }
            && resultSelector.UnwrapLambdaFromQuote() is { Body: NewExpression resultBody }
            && resultBody.Type.IsTransparentIdentifierType())
        {
            join = call;
            source = outer;
            return true;
        }

        return false;
    }

    /// <summary>The single reference navigation of <paramref name="elementType"/> to the joined root whose element-side key is the join's outer key.</summary>
    private static INavigation? FindJoinedNavigation(MethodCallExpression join, IEntityType elementType)
    {
        var inner = ((EntityQueryRootExpression)join.Arguments[1]).EntityType;
        var outerKeyNames = new HashSet<string>(
            new EFPropertyNameCollector().Collect(join.Arguments[2].UnwrapLambdaFromQuote().Body));

        var candidates = elementType.GetNavigations()
            .Where(n => !n.IsCollection
                        && !n.IsEmbedded()
                        && n.TargetEntityType == inner
                        && (n.IsOnDependent ? n.ForeignKey.Properties : n.ForeignKey.PrincipalKey.Properties)
                        .Select(p => p.Name).ToHashSet().SetEquals(outerKeyNames))
            .ToList();

        return candidates is [var navigation] ? navigation : null;
    }

    private sealed class EFPropertyNameCollector : ExpressionVisitor
    {
        private readonly List<string> _names = [];

        public IReadOnlyList<string> Collect(Expression body)
        {
            Visit(body);
            return _names;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsEFPropertyMethod() && node.Arguments is [_, ConstantExpression { Value: string name }])
                _names.Add(name);

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ParameterExpression)
                _names.Add(node.Member.Name);

            return base.VisitMember(node);
        }
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
