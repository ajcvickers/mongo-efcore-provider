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
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Query.Expressions;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// <see cref="MongoExpressionTranslator"/> — equality of a whole (single, non-collection) complex value: against
/// <c>null</c> (<c>c.Address == null</c>, <c>c.Pin.HasValue</c>), against a captured or constant instance
/// (<c>c.Address == other</c>), and against another stored complex value of the same CLR type
/// (<c>c.Billing == c.Shipping</c>), in the <c>==</c>, <c>!=</c>, <c>!(...)</c> and <c>.Equals(...)</c> spellings.
/// </summary>
/// <remarks>
/// <para>
/// <b>Member-wise, never whole-document.</b> Two complex values are equal when every mapped member is: a conjunction of
/// leaf equalities, recursing into nested complex properties. Matching the stored subdocument by example would depend on
/// element order, unmapped elements and the representation of absent leaves, so it is never emitted; the complex
/// serializer refuses it too (<c>ComplexTypeSerializer.Serialize</c>, ruling R1).
/// </para>
/// <para>
/// <b>Null semantics are C#'s.</b> A stored element that is BSON null and one that is MISSING both read as null, so they
/// answer alike everywhere: a complex value compared with null is <see cref="MongoElementNullCheckExpression"/>
/// (<c>{ p: null }</c> matches both); a nullable leaf equals a null member (<c>{ p.Zip: null }</c>); two stored nullable
/// leaves compare <c>$ifNull</c>-normalized (<see cref="MongoFieldExpression.NullSafe"/>). An OPTIONAL complex value
/// (EF10) is equal to an instance only if it is present; a comparand that may be null at run time (a captured
/// class instance, a nullable struct, a nested class member of one) is tested per execution with runtime-evaluated
/// boolean parameters, so the cached plan never bakes one value's null-ness in.
/// </para>
/// <para>
/// <b>Negation is exact by construction.</b> Each comparison is built as a pair, its <c>==</c> form and its exact
/// complement (<see cref="EqualityPair"/>); conjunctions and disjunctions complement by De Morgan, leaves by
/// <c>$eq</c>/<c>$ne</c> (which partition null and missing too) and null checks by flipping. <c>!=</c> and
/// <c>!(a == b)</c> take the complement directly, never the generic <c>Not</c> wrap.
/// </para>
/// <para>
/// <b>Declines</b> (the whole comparison, via the caller's <c>MarkNotNativelyRepresentable()</c>): a complex collection
/// anywhere in the compared value; a primitive-collection or <c>byte[]</c> leaf; a leaf or nested property with no
/// element name; a shadow leaf compared with an instance (it has no CLR value); a comparand that is neither null, a
/// constant, a query parameter nor a stored complex value of the same CLR type (e.g. an inline construction that reads
/// the row); two stored values whose same-named leaves are not stored alike
/// (<see cref="StoredSerialization.StoredAlike"/>) or whose types differ in shape; and a chain the scope can't address
/// (outer-scoped, after a projected <c>Distinct</c>, inside a <c>SelectMany</c> element scope).
/// </para>
/// </remarks>
internal sealed partial class MongoExpressionTranslator
{
    private static int _complexEqualityParameterSeed;

    /// <summary>
    /// The <c>==</c> form of a comparison and its exact complement, built together so negation never needs the
    /// dialect-sensitive generic <c>Not</c>.
    /// </summary>
    private readonly record struct EqualityPair(MongoExpression Equal, MongoExpression NotEqual)
    {
        public EqualityPair Negate() => new(NotEqual, Equal);

        public static EqualityPair And(EqualityPair left, EqualityPair right)
            => new(
                new MongoBinaryExpression(MongoBinaryOperator.AndAlso, left.Equal, right.Equal),
                new MongoBinaryExpression(MongoBinaryOperator.OrElse, left.NotEqual, right.NotEqual));

        public static EqualityPair Or(EqualityPair left, EqualityPair right)
            => new(
                new MongoBinaryExpression(MongoBinaryOperator.OrElse, left.Equal, right.Equal),
                new MongoBinaryExpression(MongoBinaryOperator.AndAlso, left.NotEqual, right.NotEqual));

        public static EqualityPair IsNull(string path)
            => new(new MongoElementNullCheckExpression(path, isNotNull: false), new MongoElementNullCheckExpression(path, isNotNull: true));

        public static EqualityPair Comparison(MongoExpression left, MongoExpression right)
            => new(
                new MongoBinaryExpression(MongoBinaryOperator.Equal, left, right),
                new MongoBinaryExpression(MongoBinaryOperator.NotEqual, left, right));
    }

    /// <summary>What a stored complex value is compared with.</summary>
    private abstract record ComplexComparand
    {
        /// <summary>A value known at translation time (a constant, or a member of one); may be null.</summary>
        public sealed record Known(object? Value) : ComplexComparand;

        /// <summary>
        /// A value reached from an EF query parameter by <paramref name="Getters"/> (applied in order), known only per
        /// execution. <paramref name="MayBeNull"/>: its CLR type admits null (a class, or <see cref="Nullable{T}"/>).
        /// </summary>
        public sealed record Runtime(string ParameterName, IReadOnlyList<IClrPropertyGetter> Getters, bool MayBeNull)
            : ComplexComparand;

        /// <summary>Another stored complex value of the same CLR type, at <paramref name="Path"/>.</summary>
        public sealed record Stored(string Path, IComplexProperty Property) : ComplexComparand;

        /// <summary>
        /// An inline construction (<c>new Address { City = "X", Geo = new GeoPoint { ... } }</c>): each member's bound
        /// expression, by member name. Never null.
        /// </summary>
        public sealed record Constructed(IReadOnlyDictionary<string, Expression> Members) : ComplexComparand;
    }

    /// <summary>
    /// Whether either side of an equality is a whole complex value this translator can address. When it is, the
    /// comparison is owned by <see cref="TranslateComplexEquality"/>: its decline is final (no other arm can translate a
    /// complex value), so callers return its result rather than trying further arms.
    /// </summary>
    private bool IsComplexEquality(Expression left, Expression right)
        => TryResolveComplexOperand(left, out _, out _) || TryResolveComplexOperand(right, out _, out _);

    /// <summary>
    /// The operands of a complex equality in any spelling (<c>==</c>, <c>!=</c>, <c>.Equals(...)</c>), for the
    /// <c>!(...)</c> arm, which takes the exact complement instead of wrapping.
    /// </summary>
    private bool TryGetComplexEqualityOperands(
        Expression node, [NotNullWhen(true)] out Expression? left, [NotNullWhen(true)] out Expression? right, out bool isNotEqual)
    {
        (left, right, isNotEqual) = node switch
        {
            BinaryExpression { NodeType: ExpressionType.Equal or ExpressionType.NotEqual } binary
                => (binary.Left, binary.Right, binary.NodeType == ExpressionType.NotEqual),
            MethodCallExpression { Method.Name: nameof(Equals), Object: { } receiver, Arguments.Count: 1 } call
                => (receiver, call.Arguments[0].RemoveObjectConvert(), false),
            _ => ((Expression?)null, (Expression?)null, false)
        };

        return left is not null && right is not null && IsComplexEquality(left, right);
    }

    /// <summary>
    /// Translates <c>left == right</c> (or, with <paramref name="isNotEqual"/>, its complement) where one side is a whole
    /// complex value; <see langword="null"/> to decline. See the type remarks.
    /// </summary>
    private MongoExpression? TranslateComplexEquality(Expression left, Expression right, bool isNotEqual)
    {
        Expression otherSide;
        string path;
        IComplexProperty complexProperty;
        if (TryResolveComplexOperand(left, out var leftPath, out var leftProperty))
        {
            (path, complexProperty, otherSide) = (leftPath, leftProperty, right);
        }
        else if (TryResolveComplexOperand(right, out var rightPath, out var rightProperty))
        {
            (path, complexProperty, otherSide) = (rightPath, rightProperty, left);
        }
        else
        {
            return null;
        }

        if (!TryClassifyComplexComparand(Unwrap(otherSide), complexProperty, out var comparand))
            return null;

        var pair = TryBuildComplexEquality(complexProperty, path, comparand);
        if (pair is null)
            return null;

        return isNotEqual ? pair.Value.NotEqual : pair.Value.Equal;
    }

    /// <summary>
    /// <c>c.Pin.HasValue</c> over an optional struct complex value: the same test as <c>c.Pin != null</c>.
    /// </summary>
    private bool TryTranslateComplexHasValue(Expression receiver, [NotNullWhen(true)] out MongoExpression? result)
    {
        result = null;
        if (!TryResolveComplexOperand(receiver, out var path, out _))
            return false;

        result = new MongoElementNullCheckExpression(path, isNotNull: true);
        return true;
    }

    private bool TryClassifyComplexComparand(
        Expression otherSide, IComplexProperty complexProperty, [NotNullWhen(true)] out ComplexComparand? comparand)
    {
        comparand = null;
        var clrType = complexProperty.ComplexType.ClrType;

        if (otherSide is ConstantExpression { Value: null })
        {
            comparand = new ComplexComparand.Known(null);
            return true;
        }

        if (otherSide.Type.UnwrapNullableType() != clrType)
            return false;

        switch (otherSide)
        {
            case ConstantExpression constant:
                comparand = new ComplexComparand.Known(constant.Value);
                return true;

            case var parameter when NativeQueryParameter.TryGetQueryParameterName(parameter, out var parameterName):
                comparand = new ComplexComparand.Runtime(parameterName, [], MayBeNull: otherSide.Type.IsNullableType());
                return true;

            case MemberInitExpression or NewExpression when TryGetConstructedMembers(otherSide, out var members):
                comparand = new ComplexComparand.Constructed(members);
                return true;

            default:
                if (TryResolveComplexOperand(otherSide, out var otherPath, out var otherProperty)
                    && otherProperty.ComplexType.ClrType == clrType)
                {
                    comparand = new ComplexComparand.Stored(otherPath, otherProperty);
                    return true;
                }

                return false;
        }
    }

    /// <summary>
    /// The equality of the complex value <paramref name="complexProperty"/> stored at <paramref name="path"/> with
    /// <paramref name="comparand"/>, or <see langword="null"/> to decline.
    /// </summary>
    private static EqualityPair? TryBuildComplexEquality(IComplexProperty complexProperty, string path, ComplexComparand comparand)
    {
        var isOptional = complexProperty.IsOptional();

        switch (comparand)
        {
            // `c.Address == null`: null and missing alike (and for a required property, the malformed rows driver-LINQ's
            // { Address: null } also returns).
            case ComplexComparand.Known { Value: null }:
                return EqualityPair.IsNull(path);

            case ComplexComparand.Known or ComplexComparand.Runtime { MayBeNull: false } or ComplexComparand.Constructed:
            {
                var members = TryBuildMemberEquality(complexProperty.ComplexType, path, comparand);
                if (members is null)
                    return null;

                // An optional value equals an instance only when present: `{}` holds an instance, null/missing don't.
                return isOptional ? EqualityPair.And(EqualityPair.IsNull(path).Negate(), members.Value) : members;
            }

            case ComplexComparand.Runtime runtime:
            {
                var members = TryBuildMemberEquality(complexProperty.ComplexType, path, runtime);
                if (members is null)
                    return null;

                var whenPresent = isOptional ? EqualityPair.And(EqualityPair.IsNull(path).Negate(), members.Value) : members.Value;

                // Which branch applies is known only per execution: (value is null AND stored is null/missing) OR
                // (value is not null AND the members match).
                var valueIsNull = RuntimeIsNull(runtime);
                return EqualityPair.Or(
                    EqualityPair.And(valueIsNull, EqualityPair.IsNull(path)),
                    EqualityPair.And(valueIsNull.Negate(), whenPresent));
            }

            case ComplexComparand.Stored stored:
            {
                var members = TryBuildMemberEquality(complexProperty.ComplexType, path, stored);
                if (members is null)
                    return null;

                if (!isOptional && !stored.Property.IsOptional())
                    return members;

                // Both null/missing, or both present with equal members.
                return EqualityPair.Or(
                    EqualityPair.And(EqualityPair.IsNull(path), EqualityPair.IsNull(stored.Path)),
                    EqualityPair.And(
                        EqualityPair.And(EqualityPair.IsNull(path).Negate(), EqualityPair.IsNull(stored.Path).Negate()),
                        members.Value));
            }

            default:
                return null;
        }
    }

    /// <summary>
    /// The conjunction of member equalities of <paramref name="complexType"/> (stored at <paramref name="path"/>) with
    /// the corresponding members of <paramref name="comparand"/>, recursing into nested complex properties.
    /// </summary>
    private static EqualityPair? TryBuildMemberEquality(IComplexType complexType, string path, ComplexComparand comparand)
    {
        EqualityPair? result = null;

        foreach (var leaf in complexType.GetProperties())
        {
            var leafPair = TryBuildLeafEquality(leaf, path, comparand);
            if (leafPair is null)
                return null;

            result = result is null ? leafPair : EqualityPair.And(result.Value, leafPair.Value);
        }

        foreach (var nested in complexType.GetComplexProperties())
        {
            if (nested.IsCollection || nested.GetElementName() is not { Length: > 0 } nestedElement)
                return null;

            var nestedPath = path + "." + nestedElement;
            ComplexComparand? nestedComparand = comparand switch
            {
                ComplexComparand.Known known
                    => new ComplexComparand.Known(nested.GetGetter().GetClrValue(known.Value!)),
                ComplexComparand.Runtime runtime
                    => new ComplexComparand.Runtime(
                        runtime.ParameterName, [.. runtime.Getters, nested.GetGetter()], MayBeNull: nested.ClrType.IsNullableType()),
                ComplexComparand.Constructed constructed
                    => TryClassifyConstructedMember(constructed, nested.Name, nested.ClrType),
                ComplexComparand.Stored stored
                    when stored.Property.ComplexType.FindComplexProperty(nested.Name) is { IsCollection: false } otherNested
                         && otherNested.ClrType == nested.ClrType
                         && otherNested.GetElementName() is { Length: > 0 } otherElement
                    => new ComplexComparand.Stored(stored.Path + "." + otherElement, otherNested),
                _ => null
            };

            if (nestedComparand is null)
                return null;

            var nestedPair = TryBuildComplexEquality(nested, nestedPath, nestedComparand);
            if (nestedPair is null)
                return null;

            result = result is null ? nestedPair : EqualityPair.And(result.Value, nestedPair.Value);
        }

        // A complex type with nothing to compare has no member-wise answer.
        return result;
    }

    private static EqualityPair? TryBuildLeafEquality(IProperty leaf, string path, ComplexComparand comparand)
    {
        // A primitive collection or byte[] leaf: `{ f: [..] }` also matches an array CONTAINING the value, and C# compares
        // such members by reference; neither is member-wise value equality.
        if (leaf.GetElementName() is not { Length: > 0 } element
            || (leaf.ClrType != typeof(string) && leaf.ClrType.TryGetEnumerableElementType() is not null))
        {
            return null;
        }

        var leafPath = path + "." + element;
        var field = new MongoFieldExpression(leaf, leafPath);

        switch (comparand)
        {
            case ComplexComparand.Known known:
            {
                if (leaf.IsShadowProperty())
                    return null;

                // A null member serializes as BSON null (BsonValueSerializer.SerializeNullAware); `{ f: null }` and the
                // aggregation dialect's $ifNull against null both treat null and missing alike.
                return EqualityPair.Comparison(field, new MongoConstantExpression(leaf.GetGetter().GetClrValue(known.Value!), leaf));
            }

            case ComplexComparand.Runtime runtime:
            {
                if (leaf.IsShadowProperty())
                    return null;

                // The leaf's value is read per execution from the parameter; a null serializes as BSON null, and
                // `{ f: null }` / `{ f: { $ne: null } }` treat null and missing alike.
                var getters = new List<IClrPropertyGetter>(runtime.Getters) { leaf.GetGetter() };
                var value = new MongoParameterExpression(
                    NextComplexParameterName(), leaf, valueType: leaf.ClrType,
                    runtimeEvaluator: values => ReadThrough(values[runtime.ParameterName], getters));
                return EqualityPair.Comparison(field, value);
            }

            case ComplexComparand.Constructed constructed:
            {
                // Every mapped leaf must be bound: an unbound member holds whatever its initializer sets, unknown here.
                if (!constructed.Members.TryGetValue(leaf.Name, out var bound))
                    return null;

                return TranslateValue(Unwrap(bound), leaf) is { } value ? EqualityPair.Comparison(field, value) : null;
            }

            case ComplexComparand.Stored stored:
            {
                if (stored.Property.ComplexType.FindProperty(leaf.Name) is not { } otherLeaf
                    || otherLeaf.GetElementName() is not { Length: > 0 } otherElement
                    || otherLeaf.ClrType != leaf.ClrType
                    || !StoredSerialization.StoredAlike(leaf, otherLeaf))
                {
                    return null;
                }

                // Field to field is $expr: $ifNull both sides so a missing and a null member compare equal, as in C#.
                return EqualityPair.Comparison(
                    new MongoFieldExpression(leaf, leafPath, nullSafe: true),
                    new MongoFieldExpression(otherLeaf, stored.Path + "." + otherElement, nullSafe: true));
            }

            default:
                return null;
        }
    }

    /// <summary>
    /// The member bindings of an inline construction with a parameterless constructor (<c>new A { X = ... }</c>, or a
    /// struct's <c>new G { ... }</c>), by member name. A constructor with arguments declines: which member each argument
    /// sets is not known.
    /// </summary>
    private static bool TryGetConstructedMembers(Expression node, [NotNullWhen(true)] out IReadOnlyDictionary<string, Expression>? members)
    {
        members = null;
        var (newExpression, bindings) = node switch
        {
            MemberInitExpression memberInit => (memberInit.NewExpression, memberInit.Bindings),
            NewExpression bare => (bare, (IReadOnlyList<MemberBinding>)[]),
            _ => ((NewExpression?)null, (IReadOnlyList<MemberBinding>)[])
        };

        if (newExpression is null || newExpression.Arguments.Count != 0)
            return false;

        var result = new Dictionary<string, Expression>();
        foreach (var binding in bindings)
        {
            if (binding is not MemberAssignment assignment)
                return false;

            result[assignment.Member.Name] = assignment.Expression;
        }

        members = result;
        return true;
    }

    // The comparand for a nested complex member of an inline construction: a nested construction, a constant or a query
    // parameter. An unbound or computed member declines.
    private static ComplexComparand? TryClassifyConstructedMember(ComplexComparand.Constructed constructed, string name, Type clrType)
    {
        if (!constructed.Members.TryGetValue(name, out var bound))
            return null;

        bound = Unwrap(bound);
        return bound switch
        {
            ConstantExpression constant => new ComplexComparand.Known(constant.Value),
            MemberInitExpression or NewExpression when TryGetConstructedMembers(bound, out var members)
                => new ComplexComparand.Constructed(members),
            _ when NativeQueryParameter.TryGetQueryParameterName(bound, out var parameterName)
                => new ComplexComparand.Runtime(parameterName, [], MayBeNull: clrType.IsNullableType()),
            _ => null
        };
    }

    // "The comparand is null" and its complement, each a boolean evaluated once per execution from the parameter.
    private static EqualityPair RuntimeIsNull(ComplexComparand.Runtime runtime)
    {
        var getters = runtime.Getters;
        var name = runtime.ParameterName;
        return new EqualityPair(
            new MongoParameterExpression(
                NextComplexParameterName(), forSerialization: null, valueType: typeof(bool),
                runtimeEvaluator: values => ReadThrough(values[name], getters) is null),
            new MongoParameterExpression(
                NextComplexParameterName(), forSerialization: null, valueType: typeof(bool),
                runtimeEvaluator: values => ReadThrough(values[name], getters) is not null));
    }

    // Applies the getters in order; a null anywhere on the way reads null (the member of a null value is absent).
    private static object? ReadThrough(object? value, IReadOnlyList<IClrPropertyGetter> getters)
    {
        foreach (var getter in getters)
        {
            if (value is null)
                return null;

            value = getter.GetClrValue(value);
        }

        return value;
    }

    private static string NextComplexParameterName()
        => "__mongoef_complex_" + Interlocked.Increment(ref _complexEqualityParameterSeed);
}
