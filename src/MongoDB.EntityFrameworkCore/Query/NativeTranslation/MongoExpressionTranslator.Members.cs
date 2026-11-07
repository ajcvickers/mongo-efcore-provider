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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure; // IsEFPropertyMethod()
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;           // QueryableMethods
using MongoDB.EntityFrameworkCore.Extensions;        // GetDocumentPath(), IsEmbedded()
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Serializers;       // BsonSerializerFactory

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// <see cref="MongoExpressionTranslator"/> — member resolution: member-access / <c>EF.Property</c> chain to an
/// <see cref="IProperty"/> and document path. A <c>partial</c> because it reads the private scope state.
/// </summary>
internal sealed partial class MongoExpressionTranslator
{
    /// Resolves a member access to its <see cref="IProperty"/> and document path; composite-PK components resolve to
    /// <c>_id.&lt;element&gt;</c> (<see cref="IsCompositeKeyComponent"/>).
    private bool TryResolveMember(
        Expression node, [NotNullWhen(true)] out IProperty? property, [NotNullWhen(true)] out string? fieldPath,
        out bool isOuter)
    {
        property = null;
        fieldPath = null;
        isOuter = false;

        // Peel Nullable<T>.Value. Only a real Nullable<T>: a user type's own `Value` member (e.g. a converted
        // strongly-typed id) must not be peeled, or the converter is bypassed.
        while (node is MemberExpression { Member.Name: nameof(Nullable<int>.Value), Expression: { } nullableReceiver }
               && Nullable.GetUnderlyingType(nullableReceiver.Type) is not null)
        {
            node = nullableReceiver;
        }

        // A keyed GroupBy.Select's output aliases have no IProperty; TryResolveFlattenedAlias owns them.
        if (ProjectedAliasScope is not null)
            return false;

        // A bare-scalar Distinct's parameter (`Select(o => o.Country).Distinct().OrderBy(x => x.IndexOf(term))`)
        // resolves by identity to the sole field-backed key part; a computed sole key declines.
        if (SelfParam is not null && ReferenceEquals(node, SelfParam)
            && DistinctAliasScope is { Accumulators.Count: 0, Key: [{ FieldRef: MongoFieldExpression soleField } soleKeyPart] }
            && IsBareScalarKeyParameter(soleField, node))
        {
            property = soleField.Property;
            fieldPath = soleKeyPart.Name!;
            isOuter = false;
            return true;
        }

        // Fast path: one hop off a parameter; everything else goes to the owned dotted-path resolver.
        ParameterExpression param;
        string memberName;
        switch (node)
        {
            case MemberExpression { Expression: ParameterExpression memberParam } me:
                param = memberParam;
                memberName = me.Member.Name;
                break;

            // Single-hop EF.Property; Unwrap strips the Convert-to-object the compiler may add.
            case MethodCallExpression call
                when call.Method.IsEFPropertyMethod()
                     && call.Arguments is [var receiver, ConstantExpression { Value: string name }]
                     && Unwrap(receiver) is ParameterExpression callParam:
                param = callParam;
                memberName = name;
                break;

            // Mql.Field(p, "element", serializer) names the stored ELEMENT, not the CLR property.
            case MethodCallExpression mqlField
                when NativeProjectionBinder.IsMqlFieldCall(mqlField)
                     && mqlField.Arguments is [var mqlReceiver, ConstantExpression { Value: string mqlElementName }, _]:
                return TryResolveMqlField(mqlField, mqlReceiver, mqlElementName, out property, out fieldPath, out isOuter);

            default:
                return TryResolveOwnedFieldPath(node, out property, out fieldPath, out isOuter);
        }

        // An EF8/EF9 compiled-query argument is a plain prefixed ParameterExpression (`__o`), never a row root.
        if (IsQueryParameterRoot(param))
            return false;

        // Two-scope mode: scope by identity (ReferenceEquals), never name, so a member name shared between scopes
        // cannot be mis-routed.
        isOuter = _outerParam is not null && ReferenceEquals(param, _outerParam);

        // After a Distinct, members resolve against its flattened output, never the entity: an unmatched name must
        // decline, not fall through to a same-named entity property.
        if (!isOuter && DistinctAliasScope is { } distinctScope)
        {
            foreach (var part in distinctScope.Key)
            {
                if (part.Name == memberName && part.FieldRef is MongoFieldExpression aliasField)
                {
                    property = aliasField.Property;
                    fieldPath = part.Name;
                    return true;
                }
            }

            return false;
        }

        var scopeType = isOuter ? _outerEntityType! : ResolveScopeEntityType(param);

        var resolved = scopeType.FindProperty(memberName);
        if (resolved is null)
            return false;

        property = resolved;
        fieldPath = GetPropertyFieldPath(resolved);

        // Inner-scope fields get the unwind-scope prefix in two-scope mode.
        if (!isOuter && _innerPrefix is not null)
            fieldPath = _innerPrefix + "." + fieldPath;

        return true;
    }

    /// Resolves a primitive-collection PROPERTY (<c>string[]</c>, <c>List&lt;int&gt;</c>) stored as a BSON array of
    /// scalars to its document path. Declines a converted or non-default-represented collection, an array of
    /// arrays/documents (<c>"path.&lt;n&gt;"</c> is ambiguous with a field named <c>"&lt;n&gt;"</c>), a dictionary, a
    /// binary vector, and a string.
    private bool TryResolvePrimitiveCollectionPath(Expression source, [NotNullWhen(true)] out string? arrayPath, out bool isOuter)
    {
        arrayPath = null;
        if (!TryResolveMember(Unwrap(source), out var property, out var path, out isOuter))
            return false;

        var elementType = property.ClrType.TryGetEnumerableElementType();
        if (property.ClrType == typeof(string)
            || elementType is null
            || (elementType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(elementType)))
        {
            return false;
        }

        if (!StoredSerialization.HasDefaultKeySerialization(property) || property.GetBinaryVectorDataType() != null)
            return false;

        var serializer = BsonSerializerFactory.GetPropertySerializationInfo(property).Serializer;
        if (serializer is not MongoDB.Bson.Serialization.IBsonArraySerializer
            || serializer is MongoDB.Bson.Serialization.IBsonDictionarySerializer)
        {
            return false;
        }

        arrayPath = path;
        return true;
    }

    /// Resolves <c>Mql.Field(p, "element", serializer)</c> to the one top-level property stored under that element name.
    /// The serializer argument is ignored (it can't be inspected at compile time), which is sound only because a
    /// converter, non-default representation or CLR-type mismatch declines. Auto-include layers around the receiver
    /// are peeled: an include changes what is materialized, not which document is read.
    private bool TryResolveMqlField(
        MethodCallExpression mqlField, Expression receiver, string elementName,
        out IProperty? property, out string? fieldPath, out bool isOuter)
    {
        property = null;
        fieldPath = null;
        isOuter = false;

        var peeled = receiver.RemoveConvert();
        while (peeled is IncludeExpression include)
            peeled = include.EntityExpression.RemoveConvert();

        if (peeled is not ParameterExpression parameter)
            return false;

        isOuter = _outerParam is not null && ReferenceEquals(parameter, _outerParam);

        if (!isOuter && DistinctAliasScope is not null)
            return false;

        var scopeType = isOuter ? _outerEntityType! : ResolveScopeEntityType(parameter);
        var byElement = scopeType.GetProperties()
            .Where(p => !IsCompositeKeyComponent(p) && p.GetElementName() == elementName)
            .ToList();
        if (byElement.Count != 1)
            return false;

        var resolved = byElement[0];
        if ((Nullable.GetUnderlyingType(mqlField.Type) ?? mqlField.Type)
            != (Nullable.GetUnderlyingType(resolved.ClrType) ?? resolved.ClrType)
            || !StoredSerialization.HasDefaultKeySerialization(resolved))
        {
            return false;
        }

        property = resolved;
        fieldPath = GetPropertyFieldPath(resolved);
        if (!isOuter && _innerPrefix is not null)
            fieldPath = _innerPrefix + "." + fieldPath;

        return true;
    }

    /// The structural type a member of <paramref name="param"/> resolves against. After <c>OfType&lt;TDerived&gt;()</c> the
    /// lambda parameter is the derived type while this translator stays bound to the root type, so a derived-only member
    /// is invisible to the root's <c>FindProperty</c>. Only for <see cref="SelfParam"/>, by identity (sibling subtypes can
    /// share a CLR name with different element names). Sound because a derived parameter type only reaches here after
    /// OfType records the discriminator conjunct; <c>Cast&lt;TDerived&gt;</c> declines at the gate and
    /// <c>((Derived)e).Member</c> is not narrowed.
    private ITypeBase ResolveScopeEntityType(ParameterExpression param)
        => _outerParam is null
           && ReferenceEquals(param, SelfParam)
           && param.Type != _scopeType.ClrType
           && _scopeType is IEntityType entityScope
           && entityScope.GetDerivedTypes().FirstOrDefault(t => t.ClrType == param.Type) is { } derived
            ? derived
            : _scopeType;

    /// Resolves <paramref name="node"/> via <see cref="TryResolveMember"/> to a field reference, declining an
    /// outer-scoped member (two-scope mode).
    private bool TryResolveInnerField(Expression node, [NotNullWhen(true)] out MongoFieldExpression? field)
    {
        if (!TryResolveMember(node, out var property, out var fieldPath, out var isOuter) || isOuter)
        {
            field = null;
            return false;
        }

        field = ScopeField(property, fieldPath);
        return true;
    }

    /// <see cref="TryResolveInnerField"/> restricted to a regex-searchable string property (<see cref="IsRegexSearchableString"/>).
    private bool TryResolveInnerStringField(Expression node, [NotNullWhen(true)] out MongoFieldExpression? field)
    {
        if (TryResolveInnerField(node, out field) && IsRegexSearchableString(field.Property))
            return true;

        field = null;
        return false;
    }

    /// <summary>
    /// Whether <paramref name="parameter"/> (the bare lambda parameter after a projected <c>Distinct()</c>) is the sole
    /// key part's own scalar rather than a one-member anonymous/DTO/tuple wrapper around it
    /// (<c>Select(o =&gt; new { o.Country }).Distinct()</c> has the same key part as <c>Select(o =&gt; o.Country).Distinct()</c>).
    /// Shared by <see cref="TryResolveMember"/> and <see cref="TryResolveFlattenedAlias"/>.
    /// </summary>
    private static bool IsBareScalarKeyParameter(MongoExpression soleKeyRef, Expression parameter)
        => soleKeyRef.Type == parameter.Type;

    /// <summary>
    /// Resolves a flattened output alias that has no <see cref="IProperty"/> to a <see cref="MongoElementRefExpression"/>:
    /// under <see cref="ProjectedAliasScope"/>, any output alias of a keyed <c>GroupBy(key).Select(...)</c>; under
    /// <see cref="DistinctAliasScope"/>, a computed Distinct key part or a prior grouping's accumulator alias.
    /// </summary>
    /// <remarks>
    /// A plain-field Distinct key part declines here (<see cref="TryResolveMember"/> owns it), so the two never both
    /// claim a member.
    /// </remarks>
    private bool TryResolveFlattenedAlias(
        Expression node, [NotNullWhen(true)] out MongoElementRefExpression? fieldRef)
    {
        fieldRef = null;

        if (ProjectedAliasScope is { } projectedScope)
            return TryResolveProjectedAlias(projectedScope, node, out fieldRef);

        // The bare parameter over a computed sole Distinct key (`Select(x => x.A + x.B).Distinct().Where(v => v > 3)`)
        // is that key's alias. With accumulators in scope it would be ambiguous between key and aggregate, so it
        // declines; the type check keeps a one-member wrapper projection from binding to the scalar alias.
        if (SelfParam is not null && ReferenceEquals(node, SelfParam)
            && DistinctAliasScope is { Accumulators.Count: 0, Key: [{ FieldRef: not MongoFieldExpression } computedKeyPart] }
            && IsBareScalarKeyParameter(computedKeyPart.FieldRef, node))
        {
            // Carry ThrowsOnNull so a null-propagated non-nullable key (`e.Label.Length`) keeps its null guard and the
            // GroupBy-key / accumulator declines.
            fieldRef = new MongoElementRefExpression(computedKeyPart.Name!, node.Type, throwsOnNull: computedKeyPart.ThrowsOnNull);
            return true;
        }

        if (DistinctAliasScope is not { } scope || node is not MemberExpression { Expression: ParameterExpression } me)
            return false;

        foreach (var part in scope.Key)
        {
            if (part.Name == me.Member.Name && part.FieldRef is not MongoFieldExpression)
            {
                fieldRef = new MongoElementRefExpression(part.Name!, part.FieldRef.Type, throwsOnNull: part.ThrowsOnNull);
                return true;
            }
        }

        // An accumulator alias from a prior GroupBy(...).Select(aggregate) is a top-level field of the flatten $project.
        foreach (var acc in scope.Accumulators)
        {
            if (acc.OutputField == me.Member.Name)
            {
                fieldRef = new MongoElementRefExpression(acc.OutputField, me.Type);
                return true;
            }
        }

        return false;
    }

    // The ProjectedAliasScope half of TryResolveFlattenedAlias: root by reference, alias by the Select's member name.
    // No IProperty, so comparisons render in the aggregation dialect ($expr).
    private static bool TryResolveProjectedAlias(
        MongoProjectedAliasScope scope, Expression node, [NotNullWhen(true)] out MongoElementRefExpression? fieldRef)
    {
        fieldRef = null;

        string alias;
        if (ReferenceEquals(node, scope.Parameter))
        {
            // A bare Select(g => g.Sum(...)) projects under the reserved bare alias; a wrapped projection has no
            // single target for the bare parameter.
            if (scope.Projections is not [{ Alias: NativeProjectionBinder.SyntheticBareProjectionAlias }])
                return false;

            alias = NativeProjectionBinder.SyntheticBareProjectionAlias;
        }
        else if (node is MemberExpression { Expression: ParameterExpression memberParam } member
                 && ReferenceEquals(memberParam, scope.Parameter))
        {
            alias = member.Member.Name;
        }
        else
        {
            return false;
        }

        foreach (var projection in scope.Projections)
        {
            if (projection.Alias != alias)
                continue;

            if (projection.Expression is MongoDocumentConstructionExpression)
                return false;

            // The lambda's view must agree with the projection's read up to nullability; the nullable side is kept so
            // the null-ordering guard still fires.
            var readType = projection.Expression.Type;
            var viewType = node.Type;
            var scalarType = readType.UnwrapNullableType();
            if (scalarType != viewType.UnwrapNullableType())
                return false;

            // No IProperty: a constant/parameter compared with the alias serializes by CLR type (BsonValue.Create),
            // which throws for e.g. Guid, so admit only types it maps to their default stored form.
            if (!IsRawComparableAliasType(scalarType))
                return false;

            fieldRef = new MongoElementRefExpression(
                projection.Alias, Nullable.GetUnderlyingType(viewType) is not null ? viewType : readType);
            return true;
        }

        return false;
    }

    // Scalar CLR types BsonValue.Create maps to the form the default serializer stores. Enums are admitted because
    // a C# enum comparison arrives as a Convert to the underlying integer. Deliberately narrower than
    // NativeGroupByBinder.IsPushableScalarElementType (no typed serializer to read back); byte/short/float unverified.
    private static bool IsRawComparableAliasType(Type type)
        => type == typeof(string) || type == typeof(bool) || type == typeof(int) || type == typeof(long)
           || type == typeof(double) || type == typeof(decimal) || type == typeof(DateTime)
           || type == typeof(MongoDB.Bson.ObjectId) || type.IsEnum;

    /// <summary>
    /// Shared preamble of the owned-path resolvers: collects hop names root-first from a <see cref="ParameterExpression"/>
    /// root, resolves which scope it names, and seeds the entity type.
    /// </summary>
    /// <param name="node">The member chain to walk.</param>
    /// <param name="minimumHops">2 for a scalar leaf under a navigation (one hop is the fast path); 1 when the leaf is the navigation.</param>
    /// <param name="scopeRootFallback">In two-scope mode, a chain rooted on neither known parameter declines unless set (then it uses this translator's entity type).</param>
    /// <param name="names">The element names of each hop.</param>
    /// <param name="scopeType">The structural type of the scope the chain is rooted on: an entity type, or a complex collection's element type inside its element scope.</param>
    /// <param name="isOuter">Whether the chain is rooted on the outer parameter; callers that can't render that must decline.</param>
    /// <remarks>
    /// Scope resolves by parameter identity, never member name; getting it wrong silently resolves the wrong scope.
    /// Callers resolve the names with <see cref="StructuralPath.TryResolve"/>, which joins each hop's own element name
    /// relative to <paramref name="scopeType"/>, never the root-relative <see cref="MongoEntityTypeExtensions.GetDocumentPath"/>
    /// (double-prefix in a nested element scope), hence no <c>IsDocumentRoot</c> guard in this family. Any parameter root
    /// is accepted in single-scope mode because an enclosing-parameter chain is declined or routed earlier
    /// (<see cref="ReferencesEnclosingScope"/>, <see cref="NativeSelectManyBinder"/>).
    /// </remarks>
    private bool TryBeginOwnedHopWalk(
        Expression node,
        int minimumHops,
        bool scopeRootFallback,
        [NotNullWhen(true)] out List<string>? names,
        [NotNullWhen(true)] out ITypeBase? scopeType,
        out bool isOuter)
    {
        names = null;
        scopeType = null;
        isOuter = false;

        if (ProjectedAliasScope is not null)
            return false;

        // A chain inside a SelectMany element scope (inner prefix set) is not handled.
        if (_innerPrefix is not null)
            return false;

        var hopNames = new List<string>();
        var current = node;
        while (current.TryGetMemberOrEFProperty(out var inner, out var name))
        {
            hopNames.Add(name);
            current = inner;
        }

        if (current is not ParameterExpression rootParam || hopNames.Count < minimumHops || IsQueryParameterRoot(rootParam))
            return false;

        isOuter = _outerParam is not null && ReferenceEquals(rootParam, _outerParam);
        if (_outerParam is not null && !isOuter && !scopeRootFallback)
            return false;

        hopNames.Reverse();

        names = hopNames;
        scopeType = isOuter ? _outerEntityType! : _scopeType;
        return true;
    }

    /// <summary>
    /// Resolves a multi-hop chain to a dotted field path, e.g. <c>p.Address.City</c> → <c>"Address.City"</c>.
    /// Intermediate hops are resolved by <see cref="StructuralPath"/> (embedded single-reference navigations and
    /// single complex properties, in any mix the model allows) and the leaf must be a mapped scalar.
    /// </summary>
    /// <remarks>
    /// A hop into an owned or complex collection declines (<see cref="StructuralPathResult.CrossesCollection"/>): a
    /// leaf under an array has no single dotted path. Quantifiers over an embedded collection go through
    /// <see cref="TryResolveEmbeddedCollectionPath"/>.
    /// </remarks>
    private bool TryResolveOwnedFieldPath(
        Expression node, [NotNullWhen(true)] out IProperty? property, [NotNullWhen(true)] out string? fieldPath,
        out bool isOuter)
    {
        property = null;
        fieldPath = null;

        if (!TryBeginOwnedHopWalk(node, minimumHops: 2, scopeRootFallback: false, out var names, out var scopeType, out isOuter))
            return false;

        // A composite-PK leaf keeps the "_id.<name>" local to its declaring type after the hop prefix (e.g.
        // "Author._id.City"); StructuralPath builds the leaf segment with GetPropertyFieldPath.
        if (!StructuralPath.TryResolve(scopeType, names, names.Count - 1, out var path)
            || path.Leaf is not IProperty leaf)
        {
            return false;
        }

        property = leaf;
        fieldPath = string.Join(".", path.Segments);
        return true;
    }

    /// <summary>
    /// Resolves a whole complex value (<c>c.Address</c>, <c>c.Address.Location</c>, <c>EF.Property&lt;A&gt;(c, "Address")</c>,
    /// through owned references; single or, on EF10, a collection) to its stored element path, for a projection leaf the
    /// shaper materializes (<c>ComplexTypeMaterializationBuilder</c>). Single-scope chains rooted on the selector
    /// parameter only; a chain crossing an array, or any other leaf kind, declines.
    /// </summary>
    internal bool TryTranslateComplexValue(
        Expression node, [NotNullWhen(true)] out string? path, [NotNullWhen(true)] out IComplexProperty? complexProperty)
    {
        path = null;
        complexProperty = null;

        if (!TryBeginOwnedHopWalk(node.RemoveConvert(), minimumHops: 1, scopeRootFallback: false, out var names, out var scopeType, out var isOuter)
            || isOuter
            || !StructuralPath.TryResolve(scopeType, names, names.Count - 1, out var resolved)
            || resolved.Leaf is not IComplexProperty leaf
            || leaf.ClrType != node.Type)
        {
            return false;
        }

        path = string.Join(".", resolved.Segments);
        complexProperty = leaf;
        return true;
    }

    /// <summary>
    /// Resolves a single (non-collection) complex value used as a comparison operand (<c>c.Address</c>,
    /// <c>c.Address.Location</c>, an optional struct's <c>c.Pin.Value</c>, through owned references) to its stored
    /// element path and complex property, for <c>MongoExpressionTranslator.ComplexEquality.cs</c>.
    /// </summary>
    /// <remarks>
    /// Single-scope chains rooted on a parameter of this translator's entity type only: an outer-scoped chain (two-scope
    /// mode) or one rooted on a projected row (after a projected <c>Distinct</c>, whose parameter is not the entity)
    /// declines, so a member name shared with the row can't resolve against the entity.
    /// </remarks>
    private bool TryResolveComplexOperand(
        Expression node, [NotNullWhen(true)] out string? path, [NotNullWhen(true)] out IComplexProperty? complexProperty)
        => TryResolveComplexOperand(node, out path, out complexProperty, out _);

    /// <summary>
    /// <see cref="TryResolveComplexOperand(Expression, out string?, out IComplexProperty?)"/>, also reporting whether the
    /// value can be absent (<paramref name="mayBeAbsent"/>): the leaf itself is an optional complex property (EF10), or
    /// any hop on the way is an optional owned reference or an optional complex property. An absent ancestor reads every
    /// value below it as null.
    /// </summary>
    private bool TryResolveComplexOperand(
        Expression node, [NotNullWhen(true)] out string? path, [NotNullWhen(true)] out IComplexProperty? complexProperty,
        out bool mayBeAbsent)
    {
        path = null;
        complexProperty = null;
        // Inside a complex collection's element scope the element itself may be null (EF10 stores null elements), and an
        // absent element reads every value below it as absent: an ancestor that can be absent.
        mayBeAbsent = StructuralPath.IsComplexElementScope(_scopeType);

        node = Unwrap(node);
        while (node is MemberExpression { Member.Name: nameof(Nullable<int>.Value), Expression: { } nullableReceiver }
               && Nullable.GetUnderlyingType(nullableReceiver.Type) is not null)
        {
            node = Unwrap(nullableReceiver);
        }

        var root = node;
        while (root.TryGetMemberOrEFProperty(out var receiver, out _))
        {
            root = Unwrap(receiver);
        }

        if (root is not ParameterExpression rootParameter
            || DistinctAliasScope is not null
            || !_scopeType.ClrType.IsAssignableFrom(rootParameter.Type)
            || !TryBeginOwnedHopWalk(node, minimumHops: 1, scopeRootFallback: false, out var names, out var scopeType, out var isOuter)
            || isOuter
            || !StructuralPath.TryResolve(scopeType, names, names.Count - 1, out var resolved)
            || resolved.Leaf is not IComplexProperty { IsCollection: false } leaf
            || node.Type.UnwrapNullableType() != leaf.ComplexType.ClrType)
        {
            return false;
        }

        // Every hop, then the leaf: optional owned references and optional complex properties make the value absent-able.
        for (var count = 1; count <= names.Count; count++)
        {
            var prefixNames = names.GetRange(0, count);
            if (!StructuralPath.TryResolve(scopeType, prefixNames, count - 1, out var prefix))
                return false;

            mayBeAbsent |= prefix.Leaf switch
            {
                INavigation navigation => !navigation.ForeignKey.IsRequiredDependent,
                IComplexProperty complex => complex.IsOptional(),
                _ => false
            };
        }

        path = string.Join(".", resolved.Segments);
        complexProperty = leaf;
        return true;
    }

    /// <summary>
    /// Whether <paramref name="parameter"/> is a QUERY parameter, not a lambda's row parameter: on EF8/EF9 a compiled
    /// query's argument (<c>EF.CompileQuery((db, Customer o) =&gt; ... c.Address == o.Address)</c>) reaches the translator
    /// as a plain <see cref="ParameterExpression"/> named with <c>QueryCompilationContext.QueryParameterPrefix</c>
    /// (<c>__o</c>), typed as the entity, so a member chain rooted on it resolved against the scope and compared the field
    /// with itself (every row). Never a row root; a no-op on EF10, whose query parameters are
    /// <c>QueryParameterExpression</c> nodes.
    /// </summary>
    private static bool IsQueryParameterRoot(ParameterExpression parameter)
        => NativeQueryParameter.TryGetQueryParameterName(parameter, out _);

    /// <summary>
    /// Whether the member chain <paramref name="node"/> crosses an OPTIONAL complex property (EF10) on its way to its leaf,
    /// so the leaf is MISSING whenever that ancestor is null/absent. Owned hops are not counted.
    /// </summary>
    private bool CrossesOptionalComplexHop(Expression node)
        => CrossesOptionalHop(node, includeOwned: false);

    /// <summary>
    /// Whether the member chain <paramref name="node"/> (rooted on a scope parameter) crosses an optional complex property,
    /// or with <paramref name="includeOwned"/> an optional owned reference, before its leaf.
    /// </summary>
    private bool CrossesOptionalHop(Expression node, bool includeOwned)
    {
        node = Unwrap(node);
        while (node is MemberExpression { Member.Name: nameof(Nullable<int>.Value), Expression: { } nullableReceiver }
               && Nullable.GetUnderlyingType(nullableReceiver.Type) is not null)
        {
            node = Unwrap(nullableReceiver);
        }

        if (!TryBeginOwnedHopWalk(node, minimumHops: 2, scopeRootFallback: false, out var names, out var scopeType, out _))
            return false;

        for (var count = 1; count < names.Count; count++)
        {
            if (!StructuralPath.TryResolve(scopeType, names.GetRange(0, count), count - 1, out var prefix))
                return false;

            if (prefix.Leaf switch
                {
                    IComplexProperty complex => complex.IsOptional(),
                    INavigation navigation => includeOwned && !navigation.ForeignKey.IsRequiredDependent,
                    _ => false
                })
            {
                return true;
            }
        }

        return false;
    }

    /// True when <paramref name="property"/> is a composite primary key component; such a key nests under an <c>_id</c>
    /// local to the declaring type at any depth (<c>{ Author: { _id: { City, Country } } }</c>).
    private static bool IsCompositeKeyComponent(IProperty property)
        => property.IsPrimaryKey() && property.FindContainingPrimaryKey()!.Properties.Count > 1;

    /// The path of <paramref name="property"/> relative to its declaring type's document.
    internal static string GetPropertyFieldPath(IProperty property)
        => IsCompositeKeyComponent(property) ? "_id." + property.GetElementName() : property.GetElementName();

    /// Resolves an entity-typed comparison operand (root entity or owned single-reference chain) to a
    /// <see cref="MongoElementRefExpression"/>, for <see cref="TranslateComparison"/>'s entity-vs-null/itself arms.
    private bool TryResolveEntityTypedOperand(Expression node, [NotNullWhen(true)] out MongoElementRefExpression? elementRef)
    {
        // Over a projected/grouped output SelfParam is a row or scalar alias, not the entity (see IsSelfParamTheEntity).
        if (IsSelfParamTheEntity(node))
        {
            elementRef = new MongoElementRefExpression(MongoElementRefExpression.WholeRootDocumentPath, _scopeType.ClrType);
            return true;
        }

        if (TryResolveOwnedReferenceNavigationPath(node, out var navPath, out var targetType, out var navIsOuter))
        {
            // An outer-scoped path must decline: MongoElementRefExpression renders element-relative ("$$this.") inside
            // an element scope, so `b.Posts.Any(p => b.Address == null)` would answer true for every row.
            if (navIsOuter)
            {
                elementRef = null;
                return false;
            }

            // nullSafe: an unset owned nav is missing, and $expr's $eq doesn't treat missing as null.
            elementRef = new MongoElementRefExpression(navPath, targetType.ClrType, nullSafe: true);
            return true;
        }

        // An owned COLLECTION (`p.Children == null`) compares the STORED array (missing equals null; empty is not null),
        // not the materialized one, which normalizes missing/null to []. Owned collections only: a COMPLEX collection's null
        // check declines (the stored state is not the answer for it).
        var collectionSource = node is MaterializeCollectionNavigationExpression materialized ? materialized.Subquery : node;
        if (TryResolveEmbeddedCollectionPath(UnwrapAsQueryable(collectionSource), out var collectionPath, out var collectionElementType, out var collectionIsOuter)
            && collectionElementType is IEntityType
            && !collectionIsOuter)
        {
            elementRef = new MongoElementRefExpression(collectionPath, node.Type, nullSafe: true);
            return true;
        }

        elementRef = null;
        return false;
    }

    /// Resolves a chain whose leaf is itself an owned single-reference navigation (<c>b.Address.Recipient</c>) to its
    /// dotted document path; <paramref name="targetType"/> is the leaf navigation's target.
    private bool TryResolveOwnedReferenceNavigationPath(
        Expression node, [NotNullWhen(true)] out string? path, [NotNullWhen(true)] out IEntityType? targetType,
        out bool isOuter)
    {
        path = null;
        targetType = null;

        if (!TryBeginOwnedHopWalk(node, minimumHops: 1, scopeRootFallback: false, out var names, out var scopeType, out isOuter))
            return false;

        // The leaf must itself be an embedded single-reference navigation; StructuralPath has already required it to be
        // embedded with an element name. A complex-property leaf (`e.Address == null` over a complex Address) is not an
        // entity-typed operand and declines here: its null/missing semantics belong to the complex read path.
        if (!StructuralPath.TryResolve(scopeType, names, names.Count - 1, out var resolved)
            || resolved.Leaf is not INavigation { IsCollection: false } leafNavigation)
        {
            return false;
        }

        targetType = leafNavigation.TargetEntityType;
        path = string.Join(".", resolved.Segments);
        return true;
    }

    /// <summary>
    /// Resolves the source of an element-scoped operator (<c>Any</c>/<c>All</c>/<c>Count</c>) over an embedded collection
    /// (an owned collection <c>b.Posts</c>, <c>b.Address.Notes</c>, or a complex collection <c>c.Addresses</c>,
    /// <c>c.Profile.Addresses</c>, through any mix of owned references and single complex properties) to the dotted path
    /// of the array, relative to this translator's scope, and the structural type of its elements.
    /// </summary>
    /// <remarks>
    /// The ONE resolver for every array quantifier/count arm, owned or complex: the hops and the final-name rule are
    /// <see cref="StructuralPath.TryResolveCollection"/>'s. The path is scope-relative (see <see cref="TryBeginOwnedHopWalk"/>),
    /// which composes with <see cref="MongoFieldPrefixRewriter"/> and makes nested <c>Any</c>-within-<c>Any</c> correct,
    /// including a collection inside a complex collection's element (resolved against the element's complex type by the
    /// element-scoped translator). An outer-rooted array is returned with <c>isOuter</c> set; the quantifier/<c>Count</c>
    /// arms decline that combination.
    /// </remarks>
    private bool TryResolveEmbeddedCollectionPath(
        Expression source,
        [NotNullWhen(true)] out string? arrayPath,
        [NotNullWhen(true)] out ITypeBase? elementType,
        out bool isOuter)
    {
        arrayPath = null;
        elementType = null;

        // A correlated subquery over another collection, staged as a root-level $lookup array by the binder.
        if (ReferencedCollectionResolver?.Invoke(source) is { } referenced)
        {
            arrayPath = referenced.ArrayPath;
            elementType = referenced.ElementType;
            isOuter = false;
            return true;
        }

        // EF-446: a non-outer root is scoped to this translator's scope type (as TryResolveMember does), so a nested
        // quantifier over the element's own collection works inside a correlated one
        // (`b.Posts.Any(p => p.Comments.Any(c => c.Title == b.Title))`). The FINAL name must be an embedded (owned) or
        // complex COLLECTION (StructuralPath.TryResolveCollection): the structural protection against a scalar property
        // sharing a navigation's name.
        if (!TryBeginOwnedHopWalk(source, minimumHops: 1, scopeRootFallback: true, out var names, out var scopeType, out isOuter)
            || !StructuralPath.TryResolveCollection(scopeType, names, out var collection))
        {
            return false;
        }

        arrayPath = collection.ArrayPath;
        elementType = collection.ElementType;
        return true;
    }
}
