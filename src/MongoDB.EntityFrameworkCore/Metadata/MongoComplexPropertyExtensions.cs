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

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.Metadata;

/// <summary>
/// Extensions for <see cref="IReadOnlyComplexProperty"/> on the MongoDB provider's model.
/// </summary>
public static class MongoComplexPropertyExtensions
{
    /// <summary>
    /// Returns the BSON element name to use for this complex property: the
    /// <see cref="MongoAnnotationNames.ElementName"/> annotation when set, otherwise the CLR property name.
    /// </summary>
    public static string GetElementName(this IReadOnlyComplexProperty complexProperty)
        => (string?)complexProperty[MongoAnnotationNames.ElementName] ?? complexProperty.Name;

    /// <summary>
    /// Returns the root-relative BSON document path of a property declared on a complex type — one segment per
    /// complex property from the entity root, then the leaf's own element name (e.g. <c>["Contact", "Address",
    /// "City"]</c>). A complex type is unique per complex property (duplicates are rejected at model validation),
    /// so the walk from the root to the leaf's declaring type is unambiguous.
    /// </summary>
    public static string[] GetDocumentPath(this IReadOnlyProperty property)
        => BuildComplexPath(property).path;

    /// <summary>
    /// Whether any complex property on the path from the entity root down to this leaf property's declaring type
    /// is nullable. Such a leaf reads an explicit BSON <c>null</c> as <c>default</c> — EF threads the complex
    /// property's nullability into its leaves' materialization rather than storing it on each leaf.
    /// </summary>
    public static bool IsUnderNullableComplex(this IReadOnlyProperty property)
        => BuildComplexPath(property).anyNullable;

    // One walk serves both consumers above: the root-relative element path (leaf-to-root, reversed at the end)
    // and whether any complex property on that path is nullable.
    private static (string[] path, bool anyNullable) BuildComplexPath(IReadOnlyProperty property)
    {
        var reversed = new List<string> { property.GetElementName() };
        var anyNullable = false;
        if (property.DeclaringType is IReadOnlyComplexType complexType
            && complexType.ContainingEntityType is IReadOnlyEntityType root
            && TryBuildPathFrom(target: complexType, root, reversed, ref anyNullable))
        {
            reversed.Reverse();
        }

        return (reversed.ToArray(), anyNullable);
    }

    // Appends, leaf-to-root, the element name of each complex property on the path from `current` down to
    // `target`; the caller reverses to root-relative order.
    private static bool TryBuildPathFrom(IReadOnlyComplexType target, IReadOnlyTypeBase current, List<string> reversed, ref bool anyNullable)
    {
        foreach (var complexProperty in current.GetComplexProperties())
        {
            if (complexProperty.ComplexType == target)
            {
                reversed.Add(complexProperty.GetElementName());
                anyNullable |= complexProperty.IsNullable;
                return true;
            }

            if (TryBuildPathFrom(target, complexProperty.ComplexType, reversed, ref anyNullable))
            {
                reversed.Add(complexProperty.GetElementName());
                anyNullable |= complexProperty.IsNullable;
                return true;
            }
        }

        return false;
    }
}
