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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// The outcome of <see cref="StructuralPath.TryResolve"/>.
/// </summary>
/// <param name="Segments">
/// The stored path, one entry per resolved name, relative to the starting scope. A composite-key leaf contributes
/// one <c>_id.&lt;element&gt;</c> entry, so join with <c>"."</c> rather than counting entries as names.
/// </param>
/// <param name="Leaf">
/// The member the leaf name resolved to (an <see cref="IProperty"/>, <see cref="IComplexProperty"/> or embedded
/// <see cref="INavigation"/>), or <see langword="null"/> when every name was a hop.
/// </param>
/// <param name="LeafOwner">
/// The structural type the leaf was looked up on: the scope reached after the last hop. When every name was a
/// hop it is the last hop's target (an <see cref="IEntityType"/> for a navigation, an <see cref="IComplexType"/>
/// for a complex property).
/// </param>
/// <param name="CrossesCollection">
/// Whether resolution stopped because a hop walks into an embedded array (an owned collection or a complex
/// collection). Only ever set together with a <see langword="false"/> result.
/// </param>
internal readonly record struct StructuralPathResult(
    IReadOnlyList<string> Segments,
    IReadOnlyPropertyBase? Leaf,
    ITypeBase LeafOwner,
    bool CrossesCollection);

/// <summary>
/// The outcome of <see cref="StructuralPath.TryResolveCollection"/>: an embedded array and the scope of its elements.
/// </summary>
/// <param name="ArrayPath">The dotted stored path of the array, relative to the starting scope.</param>
/// <param name="Collection">
/// The collection member: an embedded (owned) collection <see cref="INavigation"/> or a collection
/// <see cref="IComplexProperty"/>.
/// </param>
/// <param name="ElementType">
/// The structural type of one element, which an element-scoped predicate resolves its members against: the navigation's
/// target <see cref="IEntityType"/> or the complex collection's <see cref="IComplexType"/>.
/// </param>
internal readonly record struct StructuralCollectionPath(
    string ArrayPath,
    IReadOnlyPropertyBase Collection,
    ITypeBase ElementType);

/// <summary>
/// Resolves a root-first chain of member names across a mixed chain of owned (embedded) navigations and complex
/// properties to its stored document path. The one place that knows both kinds of hop; callers decide which leaf
/// kinds they accept.
/// </summary>
/// <remarks>
/// <para>
/// <b>Scope-relative.</b> Segments are each hop's own element name (<see cref="MongoEntityTypeExtensions.GetContainingElementName"/>
/// for a navigation's target, <see cref="MongoStructuralMemberExtensions.GetElementName(IReadOnlyComplexProperty)"/>
/// for a complex property), never the root-relative <see cref="MongoEntityTypeExtensions.GetDocumentPath"/>, so a
/// caller that prefixes the result (a nested element scope) doesn't double-prefix.
/// </para>
/// <para>
/// <b>Hop order.</b> A hop is an embedded navigation first, then a complex property. Member names are unique per
/// type, so the order only matters for which decline wins; complex types have no navigations, so after a complex
/// hop only complex hops follow.
/// </para>
/// <para>
/// <b>Arrays.</b> A hop into an owned or complex collection has no single dotted path: it declines with
/// <see cref="StructuralPathResult.CrossesCollection"/> set. A collection as the <i>leaf</i> is not a crossing (the
/// array itself has a dotted path). Element-scoped (quantifier, count) callers resolve the array with
/// <see cref="TryResolveCollection"/>, the collection variant, and bind their own element scope over its
/// <see cref="StructuralCollectionPath.ElementType"/>; <see cref="TryResolve"/> itself never crosses an array, which
/// dotted-path callers rely on to decline.
/// </para>
/// </remarks>
internal static class StructuralPath
{
    /// <summary>
    /// Resolves <paramref name="names"/> (root-first; every name but the last a hop, the last the collection) against
    /// <paramref name="scope"/> to an embedded array: an owned collection navigation or a complex collection, reached
    /// through any mix of embedded single references and single complex properties.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when any hop declines (<see cref="TryResolve"/>'s rules: an earlier hop into an array has no
    /// dotted path, so a collection inside a collection element is resolved by a nested element scope, never here) or
    /// the last name is not an embedded collection (a scalar, including a primitive collection; a reference; a
    /// cross-document navigation).
    /// </returns>
    /// <remarks>
    /// The final-name rule is the structural protection against a mapped scalar sharing a collection's name: a scalar is
    /// never an embedded collection, so a member called <c>Count</c> or a same-named property on another scope can't match.
    /// </remarks>
    internal static bool TryResolveCollection(
        ITypeBase scope, IReadOnlyList<string> names, out StructuralCollectionPath result)
    {
        result = default;

        if (names.Count == 0 || !TryResolve(scope, names, names.Count - 1, out var resolved))
            return false;

        ITypeBase? elementType = resolved.Leaf switch
        {
            INavigation { IsCollection: true } navigation => navigation.TargetEntityType,
            IComplexProperty { IsCollection: true } complexCollection => complexCollection.ComplexType,
            _ => null
        };

        if (elementType is null)
            return false;

        result = new StructuralCollectionPath(string.Join(".", resolved.Segments), resolved.Leaf!, elementType);
        return true;
    }

    /// <summary>
    /// Resolves a single-parameter selector that reads one mapped property off its parameter, directly or through owned /
    /// complex hops (<c>o =&gt; o.Ship.City</c>, <c>EF.Property</c> spellings), to that property and its dotted stored path
    /// relative to <paramref name="entityType"/>. Callers that address a field by a selector (a join key, a sort key) must
    /// resolve the WHOLE chain: resolving the leaf's simple name answers the root's own same-named property.
    /// </summary>
    internal static bool TryResolveSelectorLeaf(
        LambdaExpression selector, IEntityType entityType,
        [NotNullWhen(true)] out IProperty? property, [NotNullWhen(true)] out string? path)
    {
        property = null;
        path = null;

        var names = new List<string>();
        var current = selector.Body.RemoveConvert();
        while (current.TryGetMemberOrEFProperty(out var receiver, out var name))
        {
            names.Insert(0, name);
            current = receiver.RemoveConvert();
        }

        if (names.Count == 0
            || selector.Parameters.Count != 1
            || !ReferenceEquals(current, selector.Parameters[0])
            || !TryResolve(entityType, names, names.Count - 1, out var resolved)
            || resolved.Leaf is not IProperty leaf)
        {
            return false;
        }

        property = leaf;
        path = string.Join(".", resolved.Segments);
        return true;
    }

    /// <summary>
    /// Resolves <paramref name="names"/> against <paramref name="scope"/>: the first <paramref name="hopCount"/> names
    /// are hops; the name after them, if any, is the leaf.
    /// </summary>
    /// <param name="scope">The structural type the first name is looked up on.</param>
    /// <param name="names">Root-first member names.</param>
    /// <param name="hopCount">
    /// How many leading names are hops: <c>names.Count - 1</c> (a leaf follows) or <c>names.Count</c> (no leaf).
    /// </param>
    /// <param name="result">The resolved path; on failure, the segments resolved so far.</param>
    /// <returns>
    /// <see langword="false"/> for an unknown name, a hop that is neither an embedded single-reference navigation nor
    /// a complex property, a hop into a collection, a non-embedded navigation leaf, or an empty element name.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hopCount"/> is negative, exceeds <c>names.Count</c>, or leaves more than one leaf name.
    /// </exception>
    internal static bool TryResolve(
        ITypeBase scope, IReadOnlyList<string> names, int hopCount, out StructuralPathResult result)
    {
        if (hopCount < 0 || hopCount > names.Count || names.Count - hopCount > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(hopCount), hopCount,
                $"Expected {names.Count - 1} or {names.Count} hops for {names.Count} names.");
        }

        var segments = new List<string>(names.Count);
        var current = scope;

        for (var i = 0; i < hopCount; i++)
        {
            if (!TryResolveHop(current, names[i], out var next, out var segment, out var crossesCollection))
            {
                result = new StructuralPathResult(segments, null, current, crossesCollection);
                return false;
            }

            segments.Add(segment);
            current = next;
        }

        if (hopCount == names.Count)
        {
            result = new StructuralPathResult(segments, null, current, CrossesCollection: false);
            return true;
        }

        if (!TryResolveLeaf(current, names[hopCount], out var leaf, out var leafSegment))
        {
            result = new StructuralPathResult(segments, null, current, CrossesCollection: false);
            return false;
        }

        segments.Add(leafSegment);
        result = new StructuralPathResult(segments, leaf, current, CrossesCollection: false);
        return true;
    }

    // A hop: an embedded single-reference navigation (entity scopes only) or a single complex property. A collection
    // of either kind declines with crossesCollection; anything else (unknown, scalar, cross-document navigation)
    // declines without it.
    private static bool TryResolveHop(
        ITypeBase scope,
        string name,
        out ITypeBase next,
        out string segment,
        out bool crossesCollection)
    {
        next = scope;
        segment = "";
        crossesCollection = false;

        if (scope is IEntityType entityType && entityType.FindNavigation(name) is { } navigation)
        {
            if (!navigation.IsEmbedded())
                return false;

            if (navigation.IsCollection)
            {
                crossesCollection = true;
                return false;
            }

            // The same source the shapers and pipeline use, so the path matches stored layout (HasElementName
            // overrides, shared types).
            if (navigation.TargetEntityType.GetContainingElementName() is not { Length: > 0 } elementName)
                return false;

            next = navigation.TargetEntityType;
            segment = elementName;
            return true;
        }

        if (scope.FindComplexProperty(name) is { } complexProperty)
        {
            if (complexProperty.IsCollection)
            {
                crossesCollection = true;
                return false;
            }

            if (complexProperty.GetElementName() is not { Length: > 0 } elementName)
                return false;

            next = complexProperty.ComplexType;
            segment = elementName;
            return true;
        }

        return false;
    }

    // The leaf: a mapped property (composite-key components keep their "_id." prefix via GetPropertyFieldPath), a
    // complex property (single or collection), or an embedded navigation (reference or collection). Callers check the
    // kind they accept.
    private static bool TryResolveLeaf(
        ITypeBase scope, string name, out IReadOnlyPropertyBase? leaf, out string segment)
    {
        leaf = null;
        segment = "";

        if (scope.FindProperty(name) is { } property)
        {
            leaf = property;
            segment = MongoExpressionTranslator.GetPropertyFieldPath(property);
            return true;
        }

        if (scope.FindComplexProperty(name) is { } complexProperty)
        {
            if (complexProperty.GetElementName() is not { Length: > 0 } complexElementName)
                return false;

            leaf = complexProperty;
            segment = complexElementName;
            return true;
        }

        if (scope is IEntityType entityType
            && entityType.FindNavigation(name) is { } navigation
            && navigation.IsEmbedded()
            && navigation.TargetEntityType.GetContainingElementName() is { Length: > 0 } navigationElementName)
        {
            leaf = navigation;
            segment = navigationElementName;
            return true;
        }

        return false;
    }
}
