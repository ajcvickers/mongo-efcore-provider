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

    /// <summary>The <c>Skip</c> count applied after the ordering (a constant or a query parameter), or <see langword="null"/>.</summary>
    public Expression? Skip { get; init; }

    /// <summary>The <c>Take</c> count applied after the skip (a constant or a query parameter; a <c>ToList</c> only), or <see langword="null"/>.</summary>
    public Expression? Take { get; init; }
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
            Skip = skip,
            Take = take
        };
        return true;
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
