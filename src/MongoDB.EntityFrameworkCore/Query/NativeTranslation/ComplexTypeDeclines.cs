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

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// Single source for the text of every complex-type decline message. Several gate sites can decline a complex
/// shape (whole-value comparisons, DOM-route materialization, <c>OfType</c> over a complex shaper); the wording
/// lives here so the messages stay consistent and the member naming stays uniform.
/// </summary>
/// <remarks>
/// <para>
/// Exception TYPES are deliberately not unified. A whole-complex comparison throws
/// <see cref="InvalidOperationException"/>: the compile-time gate catches
/// <see cref="NativeTranslationNotSupportedException"/> and falls back to driver LINQ under Native mode, and
/// driver LINQ has no complex-type oracle for comparisons — the fallback would surface a bare
/// <c>ExpressionNotSupportedException</c> instead of a clear message. DOM-route materialization declines throw
/// <see cref="NativeTranslationNotSupportedException"/> because nothing catches it there: driver LINQ has no
/// whole-entity complex-collection materialization oracle (materialization always runs through the provider's
/// shaper in every mode — driver LINQ supplies MQL only), so the fallback's shaper rethrows the identical
/// decline and the query fails loudly in every mode.
/// </para>
/// <para>
/// None of these messages include rendered MQL or connection details — only model metadata (type and member
/// names) and the shape's reason.
/// </para>
/// </remarks>
internal static class ComplexTypeDeclines
{
    /// <summary>
    /// A whole complex value has no scalar representation to compare; name the member and the scalar-member
    /// alternative.
    /// </summary>
    public static string WholeValueComparison([NotNull] IReadOnlyComplexProperty complexProperty)
        => $"The complex property '{complexProperty.DeclaringType.ClrType.Name}.{complexProperty.Name}' "
           + "cannot be compared as a whole. Compare one of its scalar members instead, e.g. "
           + $"'x.{complexProperty.Name}.Member == value'.";

    /// <summary>
    /// The DOM shaper has no complex-collection read (EF's materializer block never reads the member and would
    /// silently assign null); name the entity type and the streaming alternative.
    /// </summary>
    public static string DomComplexCollection(IEntityType rootEntityType)
        => $"Materialization of entity type '{rootEntityType.DisplayName()}' is not supported: "
           + "complex collection properties are not supported by the DOM materializer. "
           + "The streaming materializer supports a root-level complex collection; this query's shape "
           + "(TPH, skip navigations, a nested complex collection, or single-result cardinality) requires "
           + "the DOM one.";

    /// <summary>
    /// <c>OfType</c> and similar hierarchy operators need a discriminator; complex types have none.
    /// </summary>
    public static string OfTypeOverComplex(IReadOnlyTypeBase structuralType)
        => $"Complex type '{structuralType.DisplayName()}' not supported in MongoDB.";
}
