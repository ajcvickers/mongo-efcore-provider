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
/// Tests whether the CURRENT ELEMENT of an aggregation element scope (<c>$$e</c> of a quantifier's <c>$map</c> or a
/// filtered count's <c>$filter</c>) is absent (<see cref="IsNotNull"/> false: BSON null) or present (true), as C# sees an
/// element of a complex collection: EF10 stores a null element as BSON null.
/// </summary>
/// <remarks>
/// <para>
/// The sealed sibling of <see cref="MongoElementNullCheckExpression"/> (which tests an element at a PATH below the
/// scope) for the one value that has no path: the element itself, as compared by <c>c.Lines.Contains(line)</c> /
/// <c>c.Lines.Any(l =&gt; l == line)</c> (member-wise equality with the element as the compared value). A separate type
/// because it is legal only inside an aggregation element scope, so every dispatcher treats it differently from the path
/// form: the query dialect has no form for it (an element is never addressable from <c>$elemMatch</c>'s body), the
/// field-prefix rewriter must not prefix it, and the aggregation renderer renders the scope variable itself.
/// </para>
/// <para>
/// Renders <c>$eq</c>/<c>$ne: [{ $ifNull: ["$$e", null] }, null]</c>. Only ever built inside an element predicate; at the
/// document root it would test <c>$$CURRENT</c> (the document, never null), so every dispatcher has a defined answer. The
/// field-prefix rewriter declines it (it is not a path), so a SelectMany scope never re-targets it. Negation flips
/// <see cref="IsNotNull"/>: the two forms partition every element.
/// </para>
/// </remarks>
internal sealed class MongoCurrentElementNullCheckExpression(bool isNotNull) : MongoExpression
{
    /// <summary><see langword="true"/> for "the element is present", <see langword="false"/> for "the element is null".</summary>
    public bool IsNotNull { get; } = isNotNull;

    /// <summary>The exact complement.</summary>
    public MongoCurrentElementNullCheckExpression Negate()
        => new(!IsNotNull);

    /// <inheritdoc />
    public override Type Type => typeof(bool);
}
