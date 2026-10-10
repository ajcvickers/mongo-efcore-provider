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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Extensions;
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

        var finder = new Finder(model, strict: false, parameterValue: null);
        finder.Visit(captured);
        return finder.Found;
    }

    /// <summary>
    /// Bulk <c>ExecuteUpdate</c>/<c>ExecuteDelete</c> (task 14): the filter and every <c>SetProperty</c> value always run
    /// on the driver-LINQ bridge, so a complex-element read the driver evaluates differently from ruling R17 would update
    /// or DELETE rows the equivalent query excludes. Throws <see cref="NativeTranslationNotSupportedException"/> unless every
    /// read of a complex collection's element in <paramref name="expressions"/> is one the driver provably answers like R17
    /// (the strict allow-list, <see cref="Finder"/>); a no-op under explicit <see cref="MongoQueryMode.DriverLinq"/>, where
    /// the matching query runs on the driver too.
    /// </summary>
    /// <param name="expressions">The bulk source chain and each setter value.</param>
    /// <param name="model">The model.</param>
    /// <param name="mode">The context's query mode.</param>
    /// <param name="parameterValue">
    /// The value of a query parameter for this execution (by name), so a comparand known non-null at run time is admitted.
    /// </param>
    internal static void ThrowIfBulkMisreadsNullElements(
        IEnumerable<Expression?> expressions, IModel model, MongoQueryMode mode, Func<string, object?> parameterValue)
    {
        if (mode == MongoQueryMode.DriverLinq || FindForBulk(expressions, model, parameterValue) is not { } found)
        {
            return;
        }

        throw new NativeTranslationNotSupportedException(
            $"ExecuteUpdate and ExecuteDelete run their filter and SetProperty values on driver-LINQ, which evaluates {found.Shape} "
            + $"over an element of the complex collection '{found.Collection}' differently from a query when the element is null "
            + "(the members of a null element are missing, which the server orders below every value and does not equate with "
            + "null) or when the collection is null, so the operation could update or delete rows the same query does not "
            + "return. No document was modified. Inside a complex collection's element predicate a bulk operation supports "
            + "'==' and '!=' between a member and a non-null value, a bool member and its negation, a non-nullable local "
            + "list's Contains of a member, and '&&', '||' and '!' over those; select the keys with a query and update or "
            + "delete by key, or use MongoQueryMode.DriverLinq to opt in to the driver-LINQ evaluation.");
    }

    /// <summary>The strict (bulk) scan of <see cref="ThrowIfBulkMisreadsNullElements"/>. Exposed for unit tests.</summary>
    internal static (string Shape, string Collection)? FindForBulk(
        IEnumerable<Expression?> expressions, IModel model, Func<string, object?>? parameterValue = null)
    {
        // A model with no complex collection has nothing to refuse: one cached lookup, no walk (the strict no-op for every
        // non-complex-collection bulk operation).
        if (ModelComplexTypes.For(model).Collections.Count == 0)
        {
            return null;
        }

        var finder = new Finder(model, strict: true, parameterValue);
        foreach (var expression in expressions)
        {
            if (expression is not null && finder.Found is null)
            {
                // A SetProperty value is a bare body over the setter's (free) entity parameter: bind it like a lambda's.
                finder.BindFreeEntityParameters(expression);
                finder.Visit(expression);
            }
        }

        return finder.Found;
    }

    // Every complex type and complex collection of a (read-only, finalized) model, computed once per model.
    private sealed class ModelComplexTypes
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IModel, ModelComplexTypes> Cache = new();

        private ModelComplexTypes(IModel model)
        {
            var types = new List<IReadOnlyComplexType>();
            var collections = new List<IReadOnlyComplexProperty>();
            var pending = new Stack<IReadOnlyTypeBase>(model.GetEntityTypes());
            while (pending.Count > 0)
            {
                foreach (var complexProperty in pending.Pop().GetComplexProperties())
                {
                    types.Add(complexProperty.ComplexType);
                    if (complexProperty.IsCollection)
                    {
                        collections.Add(complexProperty);
                    }

                    pending.Push(complexProperty.ComplexType);
                }
            }

            Types = types;
            Collections = collections;
        }

        public IReadOnlyList<IReadOnlyComplexType> Types { get; }

        public IReadOnlyList<IReadOnlyComplexProperty> Collections { get; }

        public static ModelComplexTypes For(IModel model) => Cache.GetValue(model, m => new ModelComplexTypes(m));
    }

    /// <summary>
    /// <para>
    /// Non-strict (queries, R20): finds the deny-listed null-guard-requiring node kinds in keyed element scopes.
    /// </para>
    /// <para>
    /// Strict (bulk): additionally, every read of a keyed complex element parameter must sit in an ALLOWED ATOM, a node the
    /// driver evaluates over a null element exactly as R17 does: <c>member ==/!= v</c> with <c>v</c> non-null and element-free
    /// (not over a bool member: R19 reads a null element's bool as false, the driver's <c>$eq</c> misses it); a bare
    /// non-nullable bool member (a lambda body, an <c>&amp;&amp;</c>/<c>||</c> operand, a <c>!</c> operand); and
    /// <c>list.Contains(member)</c> over a local list whose item type can't hold null. Each atom gives the same truth value
    /// on both paths, so every <c>&amp;&amp;</c>/<c>||</c>/<c>!</c> combination of them does too. Any other read (a
    /// relational comparison, arithmetic, a string method, a nested collection, a <c>Select</c>/<c>OrderBy</c> body, a
    /// comparison with another member), an element-typed lambda parameter the keying can't bind (e.g. through a
    /// navigation), and an operator over an OPTIONAL complex collection (which the bridge doesn't normalize, so the server
    /// errors mid-operation on a null one) are refused. A positive list, so a shape nobody classified is refused rather
    /// than served: the R21 limits of the query net don't apply to bulk.
    /// </para>
    /// </summary>
    private sealed class Finder(IModel model, bool strict, Func<string, object?>? parameterValue) : ExpressionVisitor
    {
        private HashSet<Type>? _complexElementClrTypes;

        // A lambda parameter -> the structural type(s) it ranges over (an entity for a query root or a join side, an
        // element type for an embedded collection's element). More than one when a CLR type maps to several entity types.
        private readonly Dictionary<ParameterExpression, IReadOnlyList<ITypeBase>> _scopes = new();

        // A complex element parameter -> the display name of the collection it is an element of.
        private readonly Dictionary<ParameterExpression, string> _complexElementCollections = new();

        public (string Shape, string Collection)? Found { get; private set; }

        public override Expression? Visit(Expression? node)
            => Found is null ? base.Visit(node) : node;

        // EF query roots and query parameters hold no lambda (measured: the only extension nodes in a bulk plan's captured
        // chain). Strict: any OTHER extension node might hide a lambda over complex elements this visitor can't see into, so
        // it is refused rather than skipped (only when the model has complex collections: FindForBulk exits earlier).
        protected override Expression VisitExtension(Expression node)
        {
            if (strict && node is not (Microsoft.EntityFrameworkCore.Query.QueryRootExpression or Microsoft.EntityFrameworkCore.Query.QueryParameterExpression))
            {
                Found = ($"an expression node of type '{node.GetType().Name}' the bulk check cannot inspect", "(any)");
            }

            return node;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            foreach (var parameter in node.Parameters)
            {
                // Strict: a lambda over complex collection elements the keying couldn't bind (reached through a navigation,
                // a join, an operator nobody classified) can't be checked, so it is refused. Checked before the entity-type
                // binding below, so a CLR type that is also an (owned) entity type isn't taken for a root; an owned
                // element's lambda was already bound by its operator.
                if (strict && !_scopes.ContainsKey(parameter) && ComplexElementClrTypes.Contains(parameter.Type))
                {
                    Found = ($"a lambda over complex collection elements ('{ExpressionShapePrinter.Print(node)}') that the bulk check cannot bind to its collection",
                        parameter.Type.Name);
                    return node;
                }

                if (!_scopes.ContainsKey(parameter) && model.FindEntityTypes(parameter.Type).ToList() is { Count: > 0 } entityTypes)
                {
                    _scopes[parameter] = entityTypes;
                }
            }

            return base.VisitLambda(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (strict)
            {
                // `c.Lines.Count`: a required collection reads empty for a null/missing array, as the bridge's coalesce to
                // `[]` does; an optional one's `$size` over null is a server error, possibly after earlier writes.
                if (node is { Member.Name: nameof(List<int>.Count), Expression: { } countReceiver }
                    && ResolveCollectionReads(countReceiver.RemoveConvert()!) is { Count: > 0 } counted
                    && counted.All(c => !c.IsOptional()))
                {
                    _approvedCollectionReads.Add(countReceiver.RemoveConvert()!);
                }

                if (IsComplexCollectionRead(node) && !_approvedCollectionReads.Contains(node))
                {
                    Found = ("a read of the collection other than as the source of Any, All, Count, LongCount, Where, Skip or Take",
                        DescribeCollectionRead(node));
                    return node;
                }
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Bind each single-parameter lambda of a sequence operator to the elements of its source.
            if ((node.Method.DeclaringType == typeof(Queryable) || node.Method.DeclaringType == typeof(Enumerable))
                && node.Arguments.Count >= (strict ? 1 : 2))
            {
                var elementScopes = ResolveElementScopes(node.Arguments[0], out var collectionName, out var collectionRead, out var collections);
                var isComplexElementScope = collectionName is not null && elementScopes.Count > 0 && elementScopes.All(StructuralPath.IsComplexElementScope);
                if (strict && isComplexElementScope && !IsBulkSafeOperator(node, collectionRead!, collections, collectionName!))
                {
                    return node;
                }

                if (elementScopes.Count > 0)
                {
                    for (var i = 1; i < node.Arguments.Count; i++)
                    {
                        if (node.Arguments[i].UnwrapQuote() is LambdaExpression { Parameters: [var parameter] } lambda)
                        {
                            _scopes[parameter] = elementScopes;
                            if (isComplexElementScope)
                            {
                                _complexElementCollections[parameter] = collectionName!;
                                if (strict && !IsBulkSafePredicate(lambda.Body))
                                {
                                    Found = ($"the element predicate '{ExpressionShapePrinter.Print(lambda)}'", collectionName!);
                                    return node;
                                }
                            }
                        }
                    }
                }
            }

            if (strict && IsComplexCollectionRead(node) && !_approvedCollectionReads.Contains(node))
            {
                Found = ("a read of the collection other than as the source of Any, All, Count, LongCount, Where, Skip or Take",
                    DescribeCollectionRead(node));
                return node;
            }

            // `list.Contains(s.Zip)` (Enumerable/Queryable, a List<T> instance method, or the C# 14 span lowering
            // MemoryExtensions.Contains(span, value[, comparer])): the needle is a MEMBER of the element. The element itself
            // as the needle (`list.Contains(s)`) is Task 12's member-wise equality, owned elsewhere.
            // (Strict: the positive list in IsBulkSafePredicate decides instead, so a deny-listed node kind it admits, e.g.
            // a non-nullable list's Contains, isn't refused here.)
            if (!strict && _complexElementCollections.Count > 0 && node.Method.Name == nameof(Enumerable.Contains)
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

        // KEEP IN STEP with the renderer's structural MayBeNull arms (R18) and ScopeField/ScopeValue (R17/R19): a node kind
        // that gets the native null guard in a complex element scope must be refused here on the fallback.
        // ComplexElementNullGuardRefusalTests pairs every discovered structural arm with an example this method refuses.
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (!strict && _complexElementCollections.Count > 0)
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
            => ResolveElementScopes(source, out collectionName, out _, out _);

        // As above; also the collection read the chain starts from (`r.Stops`, under pass-through operators) and the
        // collection member(s) it resolves to.
        private IReadOnlyList<ITypeBase> ResolveElementScopes(
            Expression source, out string? collectionName, out Expression? collectionRead, out IReadOnlyList<IReadOnlyComplexProperty> collections)
        {
            collectionName = null;
            collectionRead = null;
            var resolvedCollections = new List<IReadOnlyComplexProperty>();
            collections = resolvedCollections;
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

            collectionRead = source;
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
                    if (collection.Collection is IReadOnlyComplexProperty complexCollection)
                    {
                        resolvedCollections.Add(complexCollection);
                    }
                }
            }

            return elementScopes;
        }

        // ---- Strict (bulk) allow-list ----

        internal void BindFreeEntityParameters(Expression expression)
        {
            foreach (var parameter in ParameterCollector.Collect(expression))
            {
                if (!_scopes.ContainsKey(parameter) && model.FindEntityTypes(parameter.Type).ToList() is { Count: > 0 } entityTypes)
                {
                    _scopes[parameter] = entityTypes;
                }
            }
        }

        // The FREE parameters of an expression (a setter value's entity parameter), never one a lambda inside it declares.
        private sealed class ParameterCollector : ExpressionVisitor
        {
            private readonly HashSet<ParameterExpression> _parameters = [];
            private readonly HashSet<ParameterExpression> _declared = [];

            public static IReadOnlyCollection<ParameterExpression> Collect(Expression expression)
            {
                var collector = new ParameterCollector();
                collector.Visit(expression);
                return [.. collector._parameters.Except(collector._declared)];
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                _declared.UnionWith(node.Parameters);
                return base.VisitLambda(node);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                _parameters.Add(node);
                return node;
            }

            protected override Expression VisitExtension(Expression node) => node;
        }

        // Collection reads a consumer has approved (the source of an allowed operator, a required collection's `.Count`).
        // Approval happens when the consumer is visited, before its children. An optional collection's null check
        // (`r.Opt == null`) is NOT approved: the driver renders `{Opt: null}`, which also matches an array that CONTAINS a
        // null element (measured: [null] matched), so the operation would select a non-null collection.
        private readonly HashSet<Expression> _approvedCollectionReads = new(ReferenceEqualityComparer.Instance);

        // The CLR types of every complex collection's element in the model (for the unbound-lambda check). Deliberately
        // NOT StructuralPath.IsComplexElementScope: that judges a structural scope the keying has BOUND; this asks a
        // different question, whether a lambda parameter the keying could NOT bind (no scope to judge) might range over
        // complex elements, which only its CLR type can answer (an over-approximation, refused in strict mode).
        private HashSet<Type> ComplexElementClrTypes
            => _complexElementClrTypes ??= [.. ModelComplexTypes.For(model).Collections.Select(c => c.ComplexType.ClrType)];

        // The operators over complex collection elements a bulk filter/setter may use: quantifiers and counts (whose
        // predicates are checked by IsBulkSafePredicate) and the pass-throughs that don't read an element. An operator over
        // an OPTIONAL collection is refused: the bridge does not normalize a null optional array, so the server errors
        // (deleteMany/updateMany are not atomic: documents before the failing one are already written).
        private bool IsBulkSafeOperator(
            MethodCallExpression node, Expression collectionRead, IReadOnlyList<IReadOnlyComplexProperty> collections, string collectionName)
        {
            if (collections.Count == 0 || collections.Any(c => c.IsOptional()))
            {
                Found = ($"'{node.Method.Name}' over an optional complex collection", collectionName);
                return false;
            }

            // The bridge coalesces a null/missing REQUIRED collection to [] only when a List<T> fits the property's CLR type
            // (the same predicate, IsNormalizableToEmptyList). Otherwise (T[], ObservableCollection<T>, ...) the server's
            // `$anyElementTrue`/`$size` over a null array errors MID-OPERATION, after earlier documents were written.
            if (collections.Any(c => !c.ClrType.IsNormalizableToEmptyList(out _)))
            {
                Found = ($"'{node.Method.Name}' over a complex collection whose CLR type cannot hold a List<T> (declare it as List<T> "
                    + "or IList<T> so a null or missing array reads as empty)", collectionName);
                return false;
            }

            if (node.Method.Name is not (nameof(Enumerable.Any) or nameof(Enumerable.All) or nameof(Enumerable.Count)
                or nameof(Enumerable.LongCount) or nameof(Enumerable.Where) or nameof(Enumerable.Skip) or nameof(Enumerable.Take)
                or nameof(Queryable.AsQueryable) or nameof(Enumerable.ToList) or nameof(Enumerable.ToArray)))
            {
                Found = ($"the operator '{node.Method.Name}'", collectionName);
                return false;
            }

            _approvedCollectionReads.Add(collectionRead);
            return true;
        }

        // Whether `expression` (a lambda body over complex elements) is built only from atoms the driver evaluates over a
        // null element (and a missing member) exactly as R17 does. Parts that read no complex element are left to the
        // outer visit (which checks nested lambdas over other collections when it reaches them).
        private bool IsBulkSafePredicate(Expression expression)
        {
            if (!ReadsComplexElement(expression))
            {
                return true;
            }

            switch (expression)
            {
                case BinaryExpression { NodeType: ExpressionType.AndAlso or ExpressionType.OrElse } logical:
                    return IsBulkSafePredicate(logical.Left) && IsBulkSafePredicate(logical.Right);

                case UnaryExpression { NodeType: ExpressionType.Not, Type: var notType } not when notType == typeof(bool):
                    return IsBulkSafePredicate(not.Operand);

                // `member == v` / `!= v`, v a non-null value: a null element's member is MISSING, `$eq` false and `$ne` true
                // in every dialect the driver renders, which is R17's `null == v` / `null != v`. Not over a non-nullable bool
                // (R19 reads it false, so `== false` is true there) unless v is true.
                case BinaryExpression { NodeType: ExpressionType.Equal or ExpressionType.NotEqual } equality:
                    return (IsElementLeafRead(equality.Left, out var leftLeaf) && TryGetKnownValue(equality.Right, out var rightValue)
                               && IsSafeEqualityValue(leftLeaf, rightValue))
                           || (IsElementLeafRead(equality.Right, out var rightLeaf) && TryGetKnownValue(equality.Left, out var leftValue)
                               && IsSafeEqualityValue(rightLeaf, leftValue));

                // `list.Contains(member)`: `$in` of a MISSING value is false, R17's `list.Contains(null)` for a list that
                // can't hold null (a non-nullable item type, or a runtime value without null). Not over a bool member (R19).
                case MethodCallExpression { Method.Name: nameof(Enumerable.Contains) } contains
                    when TryGetContainsOperands(contains, out var sequence, out var value):
                    return IsElementLeafRead(value, out var needle)
                           && needle.ClrType != typeof(bool)
                           && TryGetKnownValue(sequence, out var list)
                           && list is System.Collections.IEnumerable items
                           && (sequence.Type.TryGetItemType() is { IsValueType: true } itemType && Nullable.GetUnderlyingType(itemType) is null
                               || !items.Cast<object?>().Any(i => i is null));

                // A bare non-nullable, default-serialized bool member: the driver's truthiness of MISSING is false, R19's
                // read of a null element's bool.
                default:
                    return expression.Type == typeof(bool)
                           && IsElementLeafRead(expression, out var flag)
                           && flag.ClrType == typeof(bool)
                           && HasDefaultSerialization(flag);
            }
        }

        private static bool IsSafeEqualityValue(IReadOnlyProperty leaf, object? value)
            => value is not null && (leaf.ClrType != typeof(bool) || value is true);

        private static bool HasDefaultSerialization(IReadOnlyProperty property)
            => property.GetValueConverter() is null
               && (property as IProperty)?.FindTypeMapping()?.Converter is null
               && property.GetBsonRepresentation() is null;

        // A mapped scalar (not a collection) read off a keyed complex element parameter, directly or through single complex
        // hops of the element (`s.City`, `s.Location.Lat`, EF.Property spellings), under converts. Resolved structurally
        // against the element's complex type(s).
        private bool IsElementLeafRead(Expression expression, out IReadOnlyProperty leaf)
        {
            leaf = null!;
            var names = new List<string>();
            var current = expression.RemoveConvert()!;
            while (current.TryGetMemberOrEFProperty(out var receiver, out var name))
            {
                names.Insert(0, name);
                current = receiver.RemoveConvert()!;
            }

            if (names.Count == 0
                || current is not ParameterExpression parameter
                || !_complexElementCollections.ContainsKey(parameter)
                || !_scopes.TryGetValue(parameter, out var scopes))
            {
                return false;
            }

            IReadOnlyProperty? resolvedLeaf = null;
            foreach (var scope in scopes)
            {
                if (!StructuralPath.TryResolve(scope, names, names.Count - 1, out var resolved)
                    || resolved.Leaf is not IReadOnlyProperty property
                    || (property.ClrType != typeof(string) && property.ClrType.TryGetItemType() is not null))
                {
                    return false;
                }

                resolvedLeaf ??= property;
            }

            leaf = resolvedLeaf!;
            return resolvedLeaf is not null;
        }

        // A value known when the operation runs: a constant or a query parameter (under converts and the span lowering's
        // implicit conversion). Never a read of the row.
        private bool TryGetKnownValue(Expression expression, out object? value)
        {
            value = null;
            var current = expression.RemoveConvert()!;
            if (current is MethodCallExpression { Method.Name: "op_Implicit", Object: null, Arguments: [var converted] })
            {
                current = converted.RemoveConvert()!;
            }

            switch (current)
            {
                case ConstantExpression constant:
                    value = constant.Value;
                    return true;

                // An inline array of constants (EF parameterizes it in a real query; kept for completeness).
                case NewArrayExpression { NodeType: ExpressionType.NewArrayInit } array
                    when array.Expressions.All(e => e.RemoveConvert() is ConstantExpression):
                    var items = Array.CreateInstance(array.Type.GetElementType()!, array.Expressions.Count);
                    for (var i = 0; i < array.Expressions.Count; i++)
                    {
                        items.SetValue(((ConstantExpression)array.Expressions[i].RemoveConvert()!).Value, i);
                    }

                    value = items;
                    return true;

                case Microsoft.EntityFrameworkCore.Query.QueryParameterExpression parameter when parameterValue is not null:
                    value = parameterValue(parameter.Name);
                    return true;

                default:
                    return false;
            }
        }

        // Whether `expression` reads a complex element (a keyed element parameter, or any parameter of a complex element's
        // CLR type) that is not declared inside it.
        private bool ReadsComplexElement(Expression expression)
        {
            var finder = new FreeElementReadFinder(this);
            finder.Visit(expression);
            return finder.Found;
        }

        // Keyed to a complex element scope, or unbound and of a complex element's CLR type (a parameter bound to another
        // scope, e.g. an owned element sharing the CLR type, is not a complex element).
        private bool IsComplexElementParameter(ParameterExpression parameter)
            => _complexElementCollections.ContainsKey(parameter)
               || (!_scopes.ContainsKey(parameter) && ComplexElementClrTypes.Contains(parameter.Type));

        // A member read naming a complex COLLECTION on its receiver's type(s), resolved by CLR type (an over-approximation,
        // so it catches reads the structural keying can't bind: navigation and join roots).
        private bool IsComplexCollectionRead(Expression expression)
            => ResolveCollectionReads(expression).Count > 0;

        private IReadOnlyList<IReadOnlyComplexProperty> ResolveCollectionReads(Expression expression)
        {
            if (!expression.TryGetMemberOrEFProperty(out var receiver, out var name))
            {
                return [];
            }

            var receiverType = receiver.RemoveConvert()!.Type;
            IEnumerable<IReadOnlyTypeBase> owners = model.FindEntityTypes(receiverType);
            owners = owners.Concat(ModelComplexTypes.For(model).Types.Where(t => t.ClrType == receiverType));
            return [.. owners.Select(t => t.FindComplexProperty(name)).OfType<IReadOnlyComplexProperty>().Where(p => p.IsCollection)];
        }

        private string DescribeCollectionRead(Expression expression)
            => ResolveCollectionReads(expression) is [var first, ..]
                ? $"{first.DeclaringType.DisplayName()}.{first.Name}"
                : expression.ToString();

        private sealed class FreeElementReadFinder(Finder owner) : ExpressionVisitor
        {
            private readonly HashSet<ParameterExpression> _declared = [];

            public bool Found { get; private set; }

            public override Expression? Visit(Expression? node)
                => Found ? node : base.Visit(node);

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                _declared.UnionWith(node.Parameters);
                return base.VisitLambda(node);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (!_declared.Contains(node) && owner.IsComplexElementParameter(node))
                {
                    Found = true;
                }

                return node;
            }

            protected override Expression VisitExtension(Expression node) => node;
        }

        // Whether an operand reads a keyed complex element parameter DIRECTLY (not from inside a nested lambda): a
        // relational `r.Stops.Count(s2 => s2.City == s.City) >= 1` compares a count, never the element, so it needs no
        // guard; the nested lambda's own body is still visited by the outer Finder, which finds any guard-requiring
        // shape inside it. (The cost: an element read inside a nested lambda whose RESULT feeds the relational, e.g.
        // `r.Stops.Select(s2 => s.Floor).First() < 1`, is not found; a documented limit, pinned.)
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

            protected override Expression VisitLambda<T>(Expression<T> node) => node;

            protected override Expression VisitExtension(Expression node) => node;
        }
    }
}
#endif
