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
using System.Collections;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Update;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Serializers;

namespace MongoDB.EntityFrameworkCore.Storage;

/// <summary>
/// Writes the complex properties of an <see cref="IUpdateEntry"/> as embedded BSON subdocuments (and, on EF10,
/// complex collections as arrays of subdocuments), recursing through nested complex properties.
/// </summary>
/// <remarks>
/// Complex values have no update entries of their own: the top-level value comes from the owning entry
/// (<see cref="IUpdateEntry.GetCurrentValue(IPropertyBase)"/>, which accepts an <see cref="IComplexProperty"/> on
/// EF8, EF9 and EF10 alike) and every value below it is read off the CLR instance with the member's getter
/// (<c>GetGetter().GetClrValue(instance)</c>, also identical on all three versions). Struct complex values arrive
/// boxed; the getter unboxes them, so struct and class complex types share one path. Leaf values are serialized
/// with <see cref="BsonSerializerFactory.GetPropertySerializationInfo"/>, exactly as top-level scalar properties are
/// in <see cref="MongoUpdate"/>, so value converters, <c>BsonRepresentation</c> and the provider's Guid/DateTime
/// serializers apply unchanged. Nothing identity-like (no <c>_id</c>, no ordinal key) is written inside a
/// complex subdocument: complex types have no keys.
/// </remarks>
internal static class ComplexValueWriter
{
    /// <summary>
    /// Write every complex property declared on the entry's entity type.
    /// </summary>
    /// <param name="writer">The writer positioned inside the entry's document.</param>
    /// <param name="entry">The entry that owns the complex properties.</param>
    /// <param name="propertyFilter">
    /// <see langword="null"/> (insert) writes every complex property. Otherwise a complex property is written only
    /// when at least one of its leaves (at any nesting depth) passes the filter, and it is then written
    /// <b>whole</b>: a partial subdocument would be sent in a <c>$set</c> and drop the untouched leaves. The owned
    /// entity path passes <c>_ => true</c>, which therefore writes everything. Entries in the
    /// <see cref="EntityState.Added"/> state are always written whole.
    /// </param>
    internal static void WriteComplexProperties(IBsonWriter writer, IUpdateEntry entry, Func<IProperty, bool>? propertyFilter)
    {
        // An Added entry is always written whole, whatever filter the caller passed (the owned-entity path passes
        // _ => true for Added dependents too).
        var writeAll = propertyFilter == null || entry.EntityState == EntityState.Added;

        foreach (var complexProperty in entry.EntityType.GetComplexProperties())
        {
            if (!writeAll && !AnyLeafPasses(complexProperty, propertyFilter!))
            {
                continue;
            }

            WriteComplexValue(writer, complexProperty, entry.GetCurrentValue(complexProperty));
        }
    }

    private static void WriteComplexValue(IBsonWriter writer, IComplexProperty complexProperty, object? value)
    {
        writer.WriteName(complexProperty.GetElementName());

        // A required complex property can still be null in memory (EF does not reject it on SaveChanges on every
        // version); writing BSON null mirrors the owned-reference path (WriteOwnedEntities) instead of silently
        // dropping the element. On read, a null required subdocument goes through the missing-required-element
        // rules (Task 10); an optional (EF10) one materializes as null.
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        if (complexProperty.IsEmbeddedCollection())
        {
            writer.WriteStartArray();
            foreach (var element in (IEnumerable)value)
            {
                if (element == null)
                {
                    writer.WriteNull();
                }
                else
                {
                    WriteSubdocument(writer, complexProperty.ComplexType, element);
                }
            }

            writer.WriteEndArray();
            return;
        }

        WriteSubdocument(writer, complexProperty.ComplexType, value);
    }

    private static void WriteSubdocument(IBsonWriter writer, IComplexType complexType, object instance)
    {
        writer.WriteStartDocument();

        foreach (var property in complexType.GetProperties())
        {
            var serializationInfo = BsonSerializerFactory.GetPropertySerializationInfo(property);
            writer.WriteName(serializationInfo.ElementName);
            serializationInfo.Serializer.Serialize(BsonSerializationContext.CreateRoot(writer), property.GetGetter().GetClrValue(instance));
        }

        foreach (var nested in complexType.GetComplexProperties())
        {
            WriteComplexValue(writer, nested, nested.GetGetter().GetClrValue(instance));
        }

        writer.WriteEndDocument();
    }

    private static bool AnyLeafPasses(IComplexProperty complexProperty, Func<IProperty, bool> propertyFilter)
    {
        if (complexProperty.IsEmbeddedCollection())
        {
            // The leaves of a complex collection belong to per-element entries (EF10), so the owning entry's
            // per-property modified filter cannot be asked about them. Detecting a modified collection (and
            // rewriting the whole array) is scheduled with complex collection updates; until then fail loudly
            // rather than silently skip or blindly overwrite the stored array.
            throw new NotSupportedException(
                $"Updating an entity with the complex collection '{complexProperty.DeclaringType.DisplayName()}.{complexProperty.Name}' "
                + "is not yet supported by the MongoDB EF Core provider.");
        }

        var complexType = complexProperty.ComplexType;
        return complexType.GetProperties().Any(propertyFilter)
               || complexType.GetComplexProperties().Any(nested => AnyLeafPasses(nested, propertyFilter));
    }
}
