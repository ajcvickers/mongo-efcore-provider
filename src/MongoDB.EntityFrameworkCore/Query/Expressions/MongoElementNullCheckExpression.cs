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

namespace MongoDB.EntityFrameworkCore.Query.Expressions;

/// <summary>
/// Tests whether a stored element at <see cref="Path"/> is absent (<see cref="IsNotNull"/> false: BSON null or
/// MISSING) or present (true: neither), as C# sees a value read from it: a null and a missing element both read null.
/// </summary>
/// <remarks>
/// <para>
/// Produced by <c>MongoExpressionTranslator.ComplexEquality.cs</c> for a complex value compared with <c>null</c>
/// (<c>c.Address == null</c>, <c>c.Pin.HasValue</c>) and for a nullable leaf inside a member-wise complex equality. It has
/// no <see cref="Microsoft.EntityFrameworkCore.Metadata.IProperty"/> (a complex value is not a property), so it is not a
/// <see cref="MongoFieldExpression"/>.
/// </para>
/// <para>
/// Renders <c>{ path: null }</c> / <c>{ path: { $ne: null } }</c> in the query dialect (index-usable; both match null AND
/// missing alike) and <c>$eq</c>/<c>$ne: [{ $ifNull: ["$path", null] }, null]</c> in the aggregation dialect, where a
/// bare <c>$eq</c> would answer false for a missing element. The two forms partition every document, so negation flips
/// <see cref="IsNotNull"/>: an exact complement in both dialects.
/// </para>
/// </remarks>
internal sealed class MongoElementNullCheckExpression(string path, bool isNotNull) : MongoExpression
{
    /// <summary>The (possibly dotted) element path, relative to the current scope.</summary>
    public string Path { get; } = path;

    /// <summary><see langword="true"/> for "present and not null", <see langword="false"/> for "null or missing".</summary>
    public bool IsNotNull { get; } = isNotNull;

    /// <summary>The exact complement: the same element, the other answer.</summary>
    public MongoElementNullCheckExpression Negate()
        => new(Path, !IsNotNull);

    /// <inheritdoc />
    public override Type Type => typeof(bool);
}
