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
    /// <param name="onlyModified">
    /// <see langword="false"/> writes every complex property: inserts, and owned dependents, whose whole
    /// subdocument is rewritten as part of their owner (<c>WriteEntity(_ => true)</c>). <see langword="true"/> (the
    /// entry's own <see cref="EntityState.Modified"/> pass, which goes out as a <c>$set</c>) writes a complex
    /// property only when EF reports a change anywhere inside it (see <see cref="IsChanged"/>), and then writes it
    /// <b>whole</b>: a partial subdocument in a <c>$set</c> would replace the stored one and drop the untouched
    /// leaves. A complex property with no change is skipped.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>Cost of the whole rewrite.</b> The unit of update is the top-level complex property (subdocument or
    /// array), not the leaf: if another writer changed a sibling leaf (or another element) of the same complex
    /// property since this context read it, that change is overwritten. Last writer wins per complex property, the
    /// same as for owned entities. Dotted per-leaf <c>$set</c> paths would narrow this and are a possible later
    /// optimization, not a correctness requirement.
    /// </para>
    /// <para>
    /// <b>How EF reports non-collection complex changes (measured on EF 8.0.30, 9.0.19 and 10.0.11; identical on
    /// all three).</b> A non-collection complex property has no entry of its own: its leaves are properties of the
    /// owning entry, snapshotted and compared by <c>DetectChanges</c> like any scalar.
    /// </para>
    /// <list type="bullet">
    /// <item>Changing a leaf (<c>Address.City</c>) marks exactly that leaf modified (<c>IUpdateEntry.IsModified(IProperty)</c>
    /// and <c>Entry(e).ComplexProperty(...).Property(...).IsModified</c> agree), leaves its siblings unmodified, and
    /// moves the owning entry to <see cref="EntityState.Modified"/>, so <c>MongoUpdate.ConvertModified</c> runs.</item>
    /// <item>A nested struct leaf changed by copy, mutate, assign back (<c>Location.Lat</c>) marks only that nested
    /// leaf. Replacing the whole complex instance marks only the leaves whose values differ from the snapshot; an
    /// equal-valued replacement is no change at all (the entry stays <see cref="EntityState.Unchanged"/>).</item>
    /// <item>After <c>SaveChanges</c> the leaves are accepted, so a second <c>SaveChanges</c> sends no command.</item>
    /// </list>
    /// <para>
    /// <b>Optional complex properties and complex collections (EF10 only; EF8/EF9 reject optional complex
    /// properties and have no <c>ComplexCollection</c>). Measured on EF 10.0.11.</b>
    /// </para>
    /// <list type="bullet">
    /// <item>Optional value to <see langword="null"/> and <see langword="null"/> to value mark the leaves modified
    /// (including a value whose leaves are all CLR defaults) and the root <see cref="EntityState.Modified"/>; the
    /// property is written whole, or as BSON <c>null</c> (an element present with a null value, matching a null
    /// owned reference in <c>WriteOwnedEntities</c>). <see langword="null"/> to <see langword="null"/> is no change.
    /// A null <b>required</b> complex property (EF8, EF9 and EF10) or collection (EF10) is rejected by EF itself on
    /// <c>SaveChanges</c> (<see cref="InvalidOperationException"/> "configured as required (non-nullable) but has a
    /// null value"), so the provider never writes one.</item>
    /// <item>Disconnected updates (<c>DbSet.Update(detached)</c>, or <c>Attach</c> then <c>State = Modified</c>) mark
    /// every leaf modified on all three versions, and on EF10 also mark every complex collection modified
    /// (<c>IsModified(IComplexProperty)</c> true), so every complex property, optional and collection included, is
    /// written whole.</item>
    /// <item>Complex collection elements get their own (complex) entries, so their leaves are <b>not</b> properties
    /// of the owning entry: <c>IUpdateEntry.IsModified(IProperty)</c> with an element leaf throws ("belongs to the
    /// type '...Addresses#ComplexAddress', but is being used with an instance of type '...'"). The collection is
    /// asked as a whole through <c>IUpdateEntry.IsModified(IComplexProperty)</c> (EF10 API), which agrees with
    /// <c>Entry(e).ComplexCollection(...).IsModified</c>.</item>
    /// <item>Every kind of collection change (an element leaf edited in place, a nested struct leaf in an element,
    /// an element added, removed or set to null, the list reordered or cleared, the list instance replaced by a
    /// different one) makes the collection modified <b>and moves the root entry to</b>
    /// <see cref="EntityState.Modified"/>: a change only inside a collection is not dropped. Replacing the list with
    /// an equal-valued one is no change. A scalar-only change leaves the collection unmodified (it is skipped).</item>
    /// <item>A collection nested inside a non-collection complex property reports through the parent too, and
    /// inside a collection element through the outer collection. In an owned dependent, a change only inside the
    /// dependent's collection marks the dependent Modified and leaves the root Unchanged; the root is promoted by
    /// <c>MongoDatabaseWrapper.GetAllChangedRootEntries</c> and the owned subdocument is rewritten whole.</item>
    /// <item>An empty collection is stored as <c>[]</c>, a null element as BSON <c>null</c>, and a null optional
    /// collection (<c>IsRequired(false)</c>) as BSON <c>null</c>.</item>
    /// </list>
    /// <para>
    /// <b>Shadow leaves.</b> EF10 rejects shadow properties on complex types at model building. EF8/EF9 accept them;
    /// they have no CLR member, so their value is read from the owning entry instead of the instance getter.
    /// </para>
    /// </remarks>
    internal static void WriteComplexProperties(IBsonWriter writer, IUpdateEntry entry, bool onlyModified)
    {
        foreach (var complexProperty in entry.EntityType.GetComplexProperties())
        {
            if (onlyModified && !IsChanged(entry, complexProperty))
            {
                continue;
            }

            WriteComplexValue(writer, entry, complexProperty, entry.GetCurrentValue(complexProperty));
        }
    }

    private static void WriteComplexValue(IBsonWriter writer, IUpdateEntry entry, IComplexProperty complexProperty, object? value)
    {
        writer.WriteName(complexProperty.GetElementName());

        // Reached only for an optional complex property or collection (EF10). A null *required* one is rejected by
        // EF itself at SaveChanges before the provider runs (measured on EF8, EF9 and EF10). BSON null mirrors the
        // owned-reference path (WriteOwnedEntities) rather than dropping the element; on read an optional one
        // materializes as null.
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
                    WriteSubdocument(writer, entry, complexProperty.ComplexType, element);
                }
            }

            writer.WriteEndArray();
            return;
        }

        WriteSubdocument(writer, entry, complexProperty.ComplexType, value);
    }

    private static void WriteSubdocument(IBsonWriter writer, IUpdateEntry entry, IComplexType complexType, object instance)
    {
        writer.WriteStartDocument();

        // An empty element name means "not stored", exactly as for top-level scalars in
        // MongoUpdate.WriteNonKeyProperties. Model validation skips (does not reject) empty names, so the writer
        // has to honor it rather than write an element named "".
        foreach (var property in complexType.GetProperties().Where(p => p.GetElementName() != ""))
        {
            var serializationInfo = BsonSerializerFactory.GetPropertySerializationInfo(property);
            writer.WriteName(serializationInfo.ElementPath?.Last() ?? serializationInfo.ElementName);
            serializationInfo.Serializer.Serialize(BsonSerializationContext.CreateRoot(writer), GetValue(entry, property, instance));
        }

        foreach (var nested in complexType.GetComplexProperties())
        {
            WriteComplexValue(writer, entry, nested, GetValue(entry, nested, instance));
        }

        writer.WriteEndDocument();
    }

    /// <summary>
    /// The value of a member of a complex instance. Shadow members (possible on EF8/EF9 only; EF10 rejects them on
    /// complex types) have no CLR getter, so they come from the owning entry, which tracks the leaves of a
    /// non-collection complex property. Complex collection elements (EF10) never reach the shadow branch.
    /// </summary>
    private static object? GetValue(IUpdateEntry entry, IPropertyBase member, object instance)
        => member.IsShadowProperty()
            ? entry.GetCurrentValue(member)
            : member.GetGetter().GetClrValue(instance);

    /// <summary>
    /// Whether EF reports a change anywhere inside <paramref name="complexProperty"/> on <paramref name="entry"/>:
    /// a modified leaf at any depth of the non-collection part, or (EF10) a modified complex collection at any depth
    /// of the non-collection part. A collection's own elements are not walked: their leaves belong to per-element
    /// entries, and the collection's modified flag already covers every change inside it, including changes in
    /// collections nested in its elements.
    /// </summary>
    private static bool IsChanged(IUpdateEntry entry, IComplexProperty complexProperty)
    {
#if !EF8 && !EF9
        if (complexProperty.IsEmbeddedCollection())
        {
            return entry.IsModified(complexProperty);
        }
#endif

        var complexType = complexProperty.ComplexType;
        return complexType.GetProperties().Any(entry.IsModified)
               || complexType.GetComplexProperties().Any(nested => IsChanged(entry, nested));
    }
}
