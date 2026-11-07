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

using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.Extensions;

/// <summary>
/// Internal helpers for complex properties, which share the element-name annotation with scalar properties
/// but have no public <c>GetElementName</c> overload.
/// </summary>
internal static class MongoStructuralMemberExtensions
{
    /// <summary>
    /// The element name a complex property occupies, read from the same <c>Mongo:ElementName</c> annotation as
    /// <see cref="Microsoft.EntityFrameworkCore.MongoPropertyExtensions.GetElementName(Microsoft.EntityFrameworkCore.Metadata.IReadOnlyProperty)"/>,
    /// falling back to the CLR name.
    /// </summary>
    internal static string GetElementName(this IReadOnlyComplexProperty property)
        => (string?)property[MongoAnnotationNames.ElementName] ?? property.Name;

    /// <summary>
    /// Whether the complex property is a complex collection (stored as an array of subdocuments).
    /// </summary>
    internal static bool IsEmbeddedCollection(this IReadOnlyComplexProperty property)
        => property.IsCollection;

    /// <summary>
    /// Whether the complex property is optional (may be null). Optional complex properties exist from EF10;
    /// on EF8/EF9 they are rejected by the model, so this is always false.
    /// </summary>
    internal static bool IsOptional(this IReadOnlyComplexProperty property)
#if !EF8 && !EF9
        => property.IsNullable;
#else
        => false;
#endif
}
