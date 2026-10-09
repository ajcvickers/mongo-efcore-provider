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

#if !EF8 && !EF9
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// Ruling R20: a query the native path declines must not be served by the driver-LINQ fallback when, inside a complex
/// collection's element scope, it holds a shape the driver evaluates wrongly over a NULL element (EF10 writes a null
/// element as BSON null; in a <c>$map</c>/<c>$filter</c> scope its members are MISSING, which the driver's aggregation
/// operators order below every value and never equate with null, where ruling R17 reads every member as null). Such a
/// query is refused at the compile-time gate under <see cref="MongoQueryMode.Native"/> instead of silently returning
/// wrong rows; explicit <see cref="MongoQueryMode.DriverLinq"/> is the user's opt-in and runs the driver.
/// </summary>
/// <remarks>
/// <para>
/// Structural, compile time: the element scope is recognized by resolving a lambda's source through
/// <see cref="StructuralPath.TryResolveCollection"/> (by parameter identity, never member name) to an
/// <see cref="IComplexType"/> element; the node kinds are those whose native rendering needs a null guard or a
/// missing-safe equality (relational comparison, <c>==</c>/<c>!=</c> null, membership of an element value in a local
/// collection, equality on a non-nullable <c>bool</c> member: R17/R18/R19). The data isn't consulted: the shape is refused
/// even over a collection that holds no null element today.
/// </para>
/// <para>
/// Only reached once the gate has committed to the fallback, so a shape the native path serves is never refused. Owned
/// collections (an <see cref="IEntityType"/> element) are untouched: EF never stores a null owned element.
/// </para>
/// </remarks>
internal static class ComplexElementNullGuardRefusal
{
    /// <summary>
    /// Throws <see cref="NativeTranslationNotSupportedException"/> when <paramref name="captured"/>, about to run on
    /// driver-LINQ, holds a null-guard-requiring shape in a complex collection's element scope. A no-op under
    /// <see cref="MongoQueryMode.DriverLinq"/>.
    /// </summary>
    internal static void ThrowIfDriverLinqMisreadsNullElements(Expression? captured, IModel model, MongoQueryMode mode)
    {
        if (mode == MongoQueryMode.DriverLinq)
        {
            return;
        }

        if (Find(captured, model) is { } found)
        {
            throw new NativeTranslationNotSupportedException(
                $"The query is not natively translatable and would run on driver-LINQ, which evaluates {found.Shape} over an "
                + $"element of the complex collection '{found.Collection}' incorrectly when the element is null (the members of "
                + "a null element are missing, which the server orders below every value and does not equate with null), so "
                + "the fallback would return wrong rows. Rewrite the element predicate to a natively translatable form, "
                + "project the values you need and evaluate the condition on the client, or use MongoQueryMode.DriverLinq to "
                + "opt in to the driver-LINQ execution of this query.");
        }
    }

    /// <summary>
    /// The first null-guard-requiring shape in a complex element scope of <paramref name="captured"/>, or
    /// <see langword="null"/>. Exposed for unit tests.
    /// </summary>
    internal static (string Shape, string Collection)? Find(Expression? captured, IModel model)
    {
        if (captured is null)
        {
            return null;
        }

        var finder = new Finder(model);
        finder.Visit(captured);
        return finder.Found;
    }

    private sealed class Finder(IModel model) : ExpressionVisitor
    {
        // A lambda parameter -> the structural type(s) it ranges over (an entity for a query root or a join side, an
        // element type for an embedded collection's element). More than one when a CLR type maps to several entity types.
        private readonly Dictionary<ParameterExpression, IReadOnlyList<ITypeBase>> _scopes = new();

        // A complex element parameter -> the display name of the collection it is an element of.
        private readonly Dictionary<ParameterExpression, string> _complexElementCollections = new();

        public (string Shape, string Collection)? Found { get; private set; }

        public override Expression? Visit(Expression? node)
            => Found is null ? base.Visit(node) : node;

        // EF query roots, query parameters and navigation nodes: no lambda over a complex element lives inside them.
        protected override Expression VisitExtension(Expression node) => node;

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            foreach (var parameter in node.Parameters)
            {
                if (!_scopes.ContainsKey(parameter) && model.FindEntityTypes(parameter.Type).ToList() is { Count: > 0 } entityTypes)
                {
                    _scopes[parameter] = entityTypes;
                }
            }

            return base.VisitLambda(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Bind each single-parameter lambda of a sequence operator to the elements of its source.
            if ((node.Method.DeclaringType == typeof(Queryable) || node.Method.DeclaringType == typeof(Enumerable))
                && node.Arguments.Count >= 2)
            {
                var elementScopes = ResolveElementScopes(node.Arguments[0], out var collectionName);
                if (elementScopes.Count > 0)
                {
                    for (var i = 1; i < node.Arguments.Count; i++)
                    {
                        if (node.Arguments[i].UnwrapQuote() is LambdaExpression { Parameters: [var parameter] })
                        {
                            _scopes[parameter] = elementScopes;
                            if (collectionName is not null && elementScopes.All(s => s is IComplexType))
                            {
                                _complexElementCollections[parameter] = collectionName;
                            }
                        }
                    }
                }
            }

            // `list.Contains(s.Zip)` (Enumerable/Queryable, a List<T> instance method, or the C# 14 span lowering
            // MemoryExtensions.Contains(span, value[, comparer])): the needle is a MEMBER of the element. The element itself
            // as the needle (`list.Contains(s)`) is Task 12's member-wise equality, owned elsewhere.
            if (_complexElementCollections.Count > 0 && node.Method.Name == nameof(Enumerable.Contains)
                && TryGetContainsOperands(node, out var sequence, out var value)
                && value.RemoveConvert() is not ParameterExpression
                && FindComplexElementRead(value) is { } containsElement
                && FindComplexElementRead(sequence) is null)
            {
                Found = ("a membership test of a member value in a local collection", _complexElementCollections[containsElement]);
                return node;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (_complexElementCollections.Count > 0)
            {
                switch (node.NodeType)
                {
                    case ExpressionType.LessThan or ExpressionType.LessThanOrEqual
                        or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual:
                        if ((FindComplexElementRead(node.Left) ?? FindComplexElementRead(node.Right)) is { } relational)
                        {
                            Found = ("a relational comparison", _complexElementCollections[relational]);
                            return node;
                        }

                        break;

                    case ExpressionType.Equal or ExpressionType.NotEqual:
                        if (IsNullConstant(node.Left) || IsNullConstant(node.Right))
                        {
                            if ((FindComplexElementRead(node.Left) ?? FindComplexElementRead(node.Right)) is { } nullCheck)
                            {
                                Found = ("a comparison with null", _complexElementCollections[nullCheck]);
                                return node;
                            }
                        }
                        else if ((FindBoolMemberRoot(node.Left) ?? FindBoolMemberRoot(node.Right)) is { } boolMember)
                        {
                            Found = ("an equality on a non-nullable bool member", _complexElementCollections[boolMember]);
                            return node;
                        }

                        break;
                }
            }

            return base.VisitBinary(node);
        }

        private static bool IsNullConstant(Expression expression)
            => expression.RemoveConvert()! is ConstantExpression { Value: null };

        // `x.Flag == v`: a non-nullable bool member read rooted on a complex element parameter (R19).
        private ParameterExpression? FindBoolMemberRoot(Expression operand)
        {
            var stripped = operand.RemoveConvert()!;
            return stripped.Type == typeof(bool) && stripped.TryGetMemberOrEFProperty(out var receiver, out _)
                ? FindComplexElementRead(receiver)
                : null;
        }

        // The complex element parameter `expression` reads (by identity), or null.
        private ParameterExpression? FindComplexElementRead(Expression expression)
        {
            var finder = new ParameterReadFinder(_complexElementCollections);
            finder.Visit(expression);
            return finder.Found;
        }

        private static bool TryGetContainsOperands(MethodCallExpression call, out Expression sequence, out Expression value)
        {
            switch (call)
            {
                case { Object: null, Arguments: [var staticSequence, var staticValue, ..] }
                    when staticSequence.Type.TryGetItemType() is not null || IsSpanLike(staticSequence.Type):
                    sequence = staticSequence;
                    value = staticValue;
                    return true;

                case { Object: { } instanceSequence, Arguments: [var instanceValue] }
                    when instanceSequence.Type.TryGetItemType() is not null:
                    sequence = instanceSequence;
                    value = instanceValue;
                    return true;

                default:
                    sequence = value = null!;
                    return false;
            }
        }

        private static bool IsSpanLike(Type type)
            => type.IsGenericType
               && (type.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>) || type.GetGenericTypeDefinition() == typeof(Span<>));

        // The element structural type(s) of a sequence operator's source: a member chain (owned / complex hops, then
        // an embedded collection) rooted on a bound parameter, under pass-through operators. Empty when unknown.
        private IReadOnlyList<ITypeBase> ResolveElementScopes(Expression source, out string? collectionName)
        {
            collectionName = null;
            source = source.RemoveConvert()!;
            while (source is MethodCallExpression
                   {
                       Method.Name: nameof(Queryable.AsQueryable) or nameof(Queryable.Where) or nameof(Queryable.OrderBy)
                       or nameof(Queryable.OrderByDescending) or nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending)
                       or nameof(Queryable.Skip) or nameof(Queryable.Take) or nameof(Queryable.Distinct)
                       or nameof(Queryable.DefaultIfEmpty) or nameof(Enumerable.ToList) or nameof(Enumerable.ToArray)
                   } passThrough
                   && (passThrough.Method.DeclaringType == typeof(Queryable) || passThrough.Method.DeclaringType == typeof(Enumerable))
                   && passThrough.Arguments.Count >= 1)
            {
                source = passThrough.Arguments[0].RemoveConvert()!;
            }

            var names = new List<string>();
            var current = source;
            while (current.TryGetMemberOrEFProperty(out var receiver, out var name))
            {
                names.Insert(0, name);
                current = receiver.RemoveConvert()!;
            }

            if (names.Count == 0 || current is not ParameterExpression root || !_scopes.TryGetValue(root, out var rootScopes))
            {
                return [];
            }

            var elementScopes = new List<ITypeBase>();
            foreach (var scope in rootScopes)
            {
                if (StructuralPath.TryResolveCollection(scope, names, out var collection))
                {
                    elementScopes.Add(collection.ElementType);
                    collectionName ??= $"{collection.Collection.DeclaringType.DisplayName()}.{collection.Collection.Name}";
                }
            }

            return elementScopes;
        }

        private sealed class ParameterReadFinder(Dictionary<ParameterExpression, string> complexElements) : ExpressionVisitor
        {
            public ParameterExpression? Found { get; private set; }

            public override Expression? Visit(Expression? node)
                => Found is null ? base.Visit(node) : node;

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (complexElements.ContainsKey(node))
                {
                    Found = node;
                }

                return node;
            }

            protected override Expression VisitExtension(Expression node) => node;
        }
    }
}
#endif
