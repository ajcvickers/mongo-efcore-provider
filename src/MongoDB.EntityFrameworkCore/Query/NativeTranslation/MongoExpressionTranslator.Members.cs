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

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// <see cref="MongoExpressionTranslator"/> — member resolution: resolves a member-access / <c>EF.Property</c> chain
/// to an <see cref="IProperty"/> and its document path, for predicates, sort keys and projection leaves.
/// </summary>
/// <remarks>
/// A <c>partial</c> rather than an extracted type because these members read the private scope state
/// (<c>_scopeType</c>/<c>_outerParam</c>/<c>_outerEntityType</c>/<c>_innerPrefix</c>).
/// </remarks>
internal sealed partial class MongoExpressionTranslator
{
    /// Resolves a member access to its <see cref="IProperty"/> and document path, or returns
    /// <see langword="false"/> when it can't be natively addressed. Composite-PK components resolve to
    /// <c>_id.&lt;element&gt;</c>; see <see cref="IsCompositeKeyComponent"/>.
    private bool TryResolveMember(
        Expression node, [NotNullWhen(true)] out IProperty? property, [NotNullWhen(true)] out string? fieldPath,
        out bool isOuter)
    {
        property = null;
        fieldPath = null;
        isOuter = false;

        // Peel Nullable<T>.Value so `x.A.Value` takes the fast path (`.Value` changes the CLR type, not the stored
        // element). Only for a real Nullable<T>: a user type's own `Value` member (e.g. a converted strongly-typed
        // id) must not be peeled, or the converter is bypassed.
        while (node is MemberExpression { Member.Name: nameof(Nullable<int>.Value), Expression: { } nullableReceiver }
               && Nullable.GetUnderlyingType(nullableReceiver.Type) is not null)
        {
            node = nullableReceiver;
        }

        // A keyed GroupBy.Select's output aliases have no IProperty and must never resolve against the entity or a
        // key-part name; TryResolveFlattenedAlias owns them. See ProjectedAliasScope.
        if (ProjectedAliasScope is not null)
            return false;

        // A bare-scalar Distinct's parameter (`Select(o => o.Country).Distinct().OrderBy(x => x.IndexOf(term))`)
        // resolves by identity to the sole field-backed key part; a computed sole key declines. The Accumulators.Count == 0
        // clause is defensive parity with TryResolveMember's guard: this path requires IsDistinct && !IsGroupBy, so
        // accumulators are always empty here.
        if (SelfParam is not null && ReferenceEquals(node, SelfParam)
            && DistinctAliasScope is { Accumulators.Count: 0, Key: [{ FieldRef: MongoFieldExpression soleField } soleKeyPart] }
            && IsBareScalarKeyParameter(soleField, node))
        {
            property = soleField.Property;
            fieldPath = soleKeyPart.Name!;
            isOuter = false;
            return true;
        }

        // Fast path: one hop off a parameter, as p.Foo or EF.Property<T>(p, "Foo"). Everything else goes to the
        // owned dotted-path resolver, which declines any shape that isn't a valid owned chain.
        ParameterExpression param;
        string memberName;
        switch (node)
        {
            case MemberExpression { Expression: ParameterExpression memberParam } me:
                param = memberParam;
                memberName = me.Member.Name;
                break;

            // Single-hop EF.Property. Unwrap strips the Convert-to-object the compiler may add for EF.Property's
            // `object` parameter (nav-expansion emits a bare parameter); anything else is a multi-hop chain.
            case MethodCallExpression call
                when call.Method.IsEFPropertyMethod()
                     && call.Arguments is [var receiver, ConstantExpression { Value: string name }]
                     && Unwrap(receiver) is ParameterExpression callParam:
                param = callParam;
                memberName = name;
                break;

            default:
                return TryResolveOwnedFieldPath(node, out property, out fieldPath, out isOuter);
        }

        // An EF8/EF9 compiled-query argument is a plain prefixed ParameterExpression (`__o`), never a row root.
        if (IsQueryParameterRoot(param))
            return false;

        // Two-scope mode: a member rooted on the outer param resolves against the outer entity type at document
        // root; every other member is inner-scoped. Identity (ReferenceEquals), never name — so a member name
        // shared between the two scopes cannot be mis-routed.
        isOuter = _outerParam is not null && ReferenceEquals(param, _outerParam);

        // After a Distinct, members resolve against its flattened output (DistinctAliasScope), never the entity.
        // An unmatched name declines rather than falling through, which would silently resolve a same-named
        // entity property.
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

        var scopeType = isOuter ? _outerEntityType! : _scopeType;

        var resolved = scopeType.FindProperty(memberName);
        if (resolved is null)
            return false;

        property = resolved;
        fieldPath = GetPropertyFieldPath(resolved);

        // Inner-scope fields are prefixed with the unwind scope in two-scope mode; outer-scope fields (and every
        // field in single-scope mode, where _innerPrefix is null) stay at their resolved element name.
        if (!isOuter && _innerPrefix is not null)
            fieldPath = _innerPrefix + "." + fieldPath;

        return true;
    }

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

    /// <see cref="TryResolveInnerField"/> restricted to a regex-searchable string property (<see cref="IsRegexSearchableString"/>: string-typed and stored
    /// as a BSON string; the receiver/term shape of the regex-backed string operators).
    private bool TryResolveInnerStringField(Expression node, [NotNullWhen(true)] out MongoFieldExpression? field)
    {
        if (TryResolveInnerField(node, out field) && IsRegexSearchableString(field.Property))
            return true;

        field = null;
        return false;
    }

    /// <summary>
    /// Whether <paramref name="parameter"/> (the bare lambda parameter after a projected <c>Distinct()</c>) is the sole
    /// key part's own scalar value rather than a one-member anonymous/DTO/tuple wrapper around it
    /// (<c>Select(o =&gt; new { o.Country }).Distinct()</c> has the same single key part as
    /// <c>Select(o =&gt; o.Country).Distinct()</c>, but its parameter is the wrapper). A bare scalar key's type is the
    /// parameter's type; a wrapper never is. Shared by the field-backed arm of <see cref="TryResolveMember"/> and the
    /// computed arm of <see cref="TryResolveFlattenedAlias"/>.
    /// </summary>
    private static bool IsBareScalarKeyParameter(MongoExpression soleKeyRef, Expression parameter)
        => soleKeyRef.Type == parameter.Type;

    /// <summary>
    /// Resolves a reference to a flattened output alias that has no <see cref="IProperty"/>, and so can't be
    /// expressed by <see cref="TryResolveMember"/>, to a top-level <see cref="MongoElementRefExpression"/>:
    /// <list type="bullet">
    /// <item>under <see cref="ProjectedAliasScope"/>, any output alias of a keyed <c>GroupBy(key).Select(...)</c>
    /// (a single-hop member on the scope's parameter, or the bare parameter over a bare <c>Select</c>);</item>
    /// <item>under <see cref="DistinctAliasScope"/>, a Distinct alias whose key part is computed (e.g. <c>A</c> in
    /// <c>Select(c =&gt; new { A = c.CustomerID + c.City }).Distinct()</c>), or a prior grouping's accumulator
    /// alias.</item>
    /// </list>
    /// Used by <see cref="TranslateOperand"/> (so every operator bottoming out there accepts it) and the
    /// StartsWith/EndsWith/Contains regex arm of <see cref="TryTranslate"/>.
    /// </summary>
    /// <remarks>
    /// Under <see cref="DistinctAliasScope"/> it declines for a plain-field key part, which
    /// <see cref="TryResolveMember"/> handles, so the two never both claim a member. Under
    /// <see cref="ProjectedAliasScope"/> <see cref="TryResolveMember"/> always declines, so this is the only resolver.
    /// </remarks>
    private bool TryResolveFlattenedAlias(
        Expression node, [NotNullWhen(true)] out MongoElementRefExpression? fieldRef)
    {
        fieldRef = null;

        if (ProjectedAliasScope is { } projectedScope)
            return TryResolveProjectedAlias(projectedScope, node, out fieldRef);

        // The bare parameter over a computed sole Distinct key (`Select(x => x.A + x.B).Distinct().Where(v => v > 3)`)
        // is that key's alias; a field-backed sole key is TryResolveMember's (same shape, same guards). With
        // accumulators in scope (a prior GroupBy(key).Select(aggregate)) a bare parameter would be ambiguous between
        // key and aggregate, so it falls through and declines.
        // The type check keeps a one-member anonymous/DTO projection (`Select(o => new { S = o.Year * 2 }).Distinct()`,
        // same single computed key part) from binding its non-scalar parameter to the scalar alias: only a parameter
        // whose type is the key's own (scalar) type is the bare key.
        if (SelfParam is not null && ReferenceEquals(node, SelfParam)
            && DistinctAliasScope is { Accumulators.Count: 0, Key: [{ FieldRef: not MongoFieldExpression } computedKeyPart] }
            && IsBareScalarKeyParameter(computedKeyPart.FieldRef, node))
        {
            // Carry the key part's ThrowsOnNull mark, as the member arm below does: a null-propagated non-nullable key
            // (`Select(e => e.Label.Length).Distinct()`) must keep the comparison null guard and the GroupBy-key /
            // accumulator declines.
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

        // An accumulator alias from a prior GroupBy(...).Select(aggregate) under a further GroupBy was written as a
        // top-level field by the flatten $project. Accumulators have no IProperty, so no overlap with
        // TryResolveMember. Empty for a plain Distinct.
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

    // The ProjectedAliasScope half of TryResolveFlattenedAlias. The root must be the scope's own parameter (by
    // reference); an alias is looked up by the Select's member name only. The result has no IProperty, so every
    // comparison renders in the aggregation dialect ($expr): a post-group $match runs over $group output, where no
    // index applies.
    private static bool TryResolveProjectedAlias(
        MongoProjectedAliasScope scope, Expression node, [NotNullWhen(true)] out MongoElementRefExpression? fieldRef)
    {
        fieldRef = null;

        string alias;
        if (ReferenceEquals(node, scope.Parameter))
        {
            // A bare Select(g => g.Sum(...)) projects its sole output under the reserved bare alias; a wrapped
            // projection has no single implied target for the bare parameter.
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

            // A nested construction is a sub-document, not a comparable or reducible scalar.
            if (projection.Expression is MongoDocumentConstructionExpression)
                return false;

            // Typed from the projection's read, which the lambda's view of the alias must agree with up to
            // nullability; the nullable side is kept so the aggregation renderer's null-ordering guard still fires.
            var readType = projection.Expression.Type;
            var viewType = node.Type;
            var scalarType = readType.UnwrapNullableType();
            if (scalarType != viewType.UnwrapNullableType())
                return false;

            // The alias has no IProperty, so a constant or parameter compared with it serializes by CLR type
            // (BsonValue.Create), which throws for e.g. Guid rather than declining. Admit only types it maps to their
            // default stored form.
            if (!IsRawComparableAliasType(scalarType))
                return false;

            fieldRef = new MongoElementRefExpression(
                projection.Alias, Nullable.GetUnderlyingType(viewType) is not null ? viewType : readType);
            return true;
        }

        return false;
    }

    // Scalar CLR types BsonValue.Create maps to the same BSON form the default serializer stores. Enums are admitted
    // because a C# enum comparison arrives as a Convert to the underlying integer on both sides.
    //
    // Narrower than NativeGroupByBinder.IsPushableScalarElementType on purpose: a pushed list is read back through a
    // typed serializer, whereas a value compared with an alias is serialized by BsonValue.Create with no type to go
    // on. byte/short/float are left out (unverified constant/parameter mapping; C# widens byte/short to int anyway).
    private static bool IsRawComparableAliasType(Type type)
        => type == typeof(string) || type == typeof(bool) || type == typeof(int) || type == typeof(long)
           || type == typeof(double) || type == typeof(decimal) || type == typeof(DateTime)
           || type == typeof(MongoDB.Bson.ObjectId) || type.IsEnum;

    /// <summary>
    /// Shared preamble of the owned-path resolvers: rejects an inner-prefixed scope, collects hop names root-first,
    /// requires a <see cref="ParameterExpression"/> root, resolves which scope it names, and seeds the entity type.
    /// </summary>
    /// <param name="node">The member-access / <c>EF.Property</c> chain to walk.</param>
    /// <param name="minimumHops">
    /// 2 when the leaf is a scalar under a navigation (one hop is <see cref="TryResolveMember"/>'s fast path);
    /// 1 when the leaf is itself the navigation.
    /// </param>
    /// <param name="scopeRootFallback">
    /// In two-scope mode, a chain rooted on neither known parameter declines unless this is set, in which case it is
    /// scoped to this translator's own entity type (as <see cref="TryResolveMember"/> does).
    /// </param>
    /// <param name="names">The hop names, root-first, on success.</param>
    /// <param name="scopeType">
    /// The structural type the first hop resolves against, on success: an entity type, or a complex collection's element
    /// type inside its element scope.
    /// </param>
    /// <param name="isOuter">
    /// Whether the chain is rooted on the outer parameter. Callers that can't render an outer-scoped result must
    /// decline on it.
    /// </param>
    /// <remarks>
    /// <para>
    /// The <c>isOuter</c> line enforces "scope resolves by parameter identity, never member name"; getting it wrong
    /// silently resolves against the wrong scope.
    /// </para>
    /// <para>
    /// <b>Scope-relative.</b> Callers resolve the names with <see cref="StructuralPath.TryResolve"/>, which joins each
    /// hop's own element name relative to <paramref name="scopeType"/>, never
    /// <see cref="MongoEntityTypeExtensions.GetDocumentPath"/>, which is root-relative and would double-prefix in a
    /// nested element scope whose caller prefixes the result. Hence no <c>IsDocumentRoot</c> guard anywhere in this
    /// family.
    /// </para>
    /// <para>
    /// <b>Any parameter root is accepted in single-scope mode</b> because a chain rooted on an enclosing parameter
    /// can't reach here: the quantifier arm's <see cref="ReferencesEnclosingScope"/> guard and
    /// <see cref="NativeSelectManyBinder"/>'s identity-routed construction decline or route it first.
    /// </para>
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

        // A keyed GroupBy.Select's flattened output has no entity to walk; see ProjectedAliasScope.
        if (ProjectedAliasScope is not null)
            return false;

        // A chain inside a SelectMany element scope (inner prefix set) is out of scope; only the outer-param
        // two-scope case is handled.
        if (_innerPrefix is not null)
            return false;

        // Collect hop names from the outer (leaf) hop inward; the root must be a parameter.
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
            return false; // two-scope mode, rooted on neither known parameter — decline rather than guess

        hopNames.Reverse(); // now root-first: [firstNav, ..., leaf]

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
        // absent element reads every value below it as absent: an ancestor that can be absent (ruling R14).
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
    /// so the leaf is MISSING whenever that ancestor is null/absent (ruling R14). Owned hops are not counted.
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

    /// True when <paramref name="property"/> is a component of a composite primary key. The serializer nests such a
    /// key under an <c>_id</c> local to the declaring type — <c>{ _id: { Key1, Key2 } }</c> at the root, or
    /// <c>{ Author: { _id: { City, Country } } }</c> for an owned type with its own <c>HasKey</c> — at any depth.
    private static bool IsCompositeKeyComponent(IProperty property)
        => property.IsPrimaryKey() && property.FindContainingPrimaryKey()!.Properties.Count > 1;

    /// The path of <paramref name="property"/> relative to its declaring type's document: its element name, or
    /// <c>_id.&lt;element&gt;</c> for a composite-PK component (see <see cref="IsCompositeKeyComponent"/>).
    internal static string GetPropertyFieldPath(IProperty property)
        => IsCompositeKeyComponent(property) ? "_id." + property.GetElementName() : property.GetElementName();

    /// Resolves an entity-typed comparison operand — the root entity (<c>c == null</c>) or an owned single-reference
    /// navigation chain (<c>b.Address == null</c>) — to a <see cref="MongoElementRefExpression"/>. Only for
    /// <see cref="TranslateComparison"/>'s entity-vs-null and entity-vs-itself arms.
    private bool TryResolveEntityTypedOperand(Expression node, [NotNullWhen(true)] out MongoElementRefExpression? elementRef)
    {
        // Over a projected/grouped output SelfParam is a row or a bare scalar alias, not the entity: `v == null` over a
        // nullable bare aggregate must compare the alias, not "$$ROOT". See IsSelfParamTheEntity.
        if (IsSelfParamTheEntity(node))
        {
            elementRef = new MongoElementRefExpression(MongoElementRefExpression.WholeRootDocumentPath, _scopeType.ClrType);
            return true;
        }

        if (TryResolveOwnedReferenceNavigationPath(node, out var navPath, out var targetType, out var navIsOuter))
        {
            // An outer-scoped path must decline: MongoElementRefExpression renders element-relative ("$$this.")
            // inside an element scope, so `b.Posts.Any(p => b.Address == null)` would read a missing field on the
            // element and answer true for every row. MongoOuterFieldExpression is root-anchored but needs an
            // IProperty.
            if (navIsOuter)
            {
                elementRef = null;
                return false;
            }

            // nullSafe: an unset owned nav is missing, and $expr's $eq doesn't treat missing as null. See
            // MongoElementRefExpression.NullSafe.
            elementRef = new MongoElementRefExpression(navPath, targetType.ClrType, nullSafe: true);
            return true;
        }

        elementRef = null;
        return false;
    }

    /// Resolves a chain whose leaf is itself an owned single-reference navigation (<c>b.Address</c>,
    /// <c>b.Address.Recipient</c>) to its dotted document path. Every hop, including the leaf, must be an embedded
    /// single reference; <paramref name="targetType"/> is the leaf navigation's target entity type.
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

        // EF-446: a non-outer root is scoped to this translator's own scope type rather than declined, mirroring
        // TryResolveMember. A plain nested quantifier over the element's own collection can then sit inside an
        // already-correlated quantifier (`b.Posts.Any(p => p.Comments.Any(c => c.Title == b.Title))`); an array
        // reached THROUGH the outer scope is still declined by the callers' isOuter check. Every hop but the last is a
        // StructuralPath hop (never a collection); the FINAL name must be an embedded (owned) or complex COLLECTION
        // (StructuralPath.TryResolveCollection): a primitive collection property or a reference navigation declines, so a
        // mapped scalar property sharing a navigation's name is never taken for an array.
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
