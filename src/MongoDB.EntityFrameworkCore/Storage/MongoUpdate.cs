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
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Update;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Serializers;

namespace MongoDB.EntityFrameworkCore.Storage;

internal class MongoUpdate(IUpdateEntry entry, WriteModel<BsonDocument> model, IUpdateEntry? replaced = null)
{
    /// <summary>
    /// The <see cref="WriteModel{BsonDocument}"/> that contains both the document
    /// being modified and indication of the type of update being performed.
    /// </summary>
    public WriteModel<BsonDocument> Model
    {
        get => model;
    }

    /// <summary>
    /// The <see cref="IUpdateEntry"/> that is the source of this update.
    /// </summary>
    public IUpdateEntry Entry
    {
        get => entry;
    }

    /// <summary>
    /// For a replacement of a document (an entity removed and another added with the same primary key in one save), the
    /// <see cref="EntityState.Deleted"/> entry of the entity removed; <see cref="Entry"/> is the one added. Otherwise
    /// <see langword="null"/>.
    /// </summary>
    public IUpdateEntry? Replaced
    {
        get => replaced;
    }

    /// <summary>
    /// <see langword="true"/> if this changes an existing document (an <c>UpdateOne</c> or a <c>ReplaceOne</c>), which is
    /// matched on its filter, as opposed to inserting or deleting one.
    /// </summary>
    public bool IsUpdate
    {
        get => model.ModelType is WriteModelType.UpdateOne or WriteModelType.ReplaceOne;
    }

    /// <summary>
    /// Create an enumeration of <see cref="MongoUpdate"/> from an enumeration of EF Core-supplied
    /// <see cref="IUpdateEntry"/>.
    /// </summary>
    /// <param name="entries">The EF Core-supplied <see cref="IUpdateEntry"/> to process.</param>
    /// <returns>An enumeration of <see cref="MongoUpdate"/> that corresponds to these updates.</returns>
    public static IEnumerable<MongoUpdate> CreateAll(IEnumerable<IUpdateEntry> entries)
        => entries.Select(Create).OfType<MongoUpdate>();

    private static MongoUpdate? Create(IUpdateEntry entry)
        => entry.EntityState switch
        {
            EntityState.Added => ConvertAdded(entry),
            EntityState.Deleted => ConvertDeleted(entry),
            EntityState.Modified => ConvertModified(entry),
            EntityState.Detached => null,
            EntityState.Unchanged => null,
            _ => throw new NotSupportedException($"Unexpected entity state: {entry.EntityState}.")
        };

    private static MongoUpdate ConvertAdded(IUpdateEntry entry)
    {
        // An entity removed and another added with the same primary key: EF Core pairs the two entries (the added entry's
        // SharedIdentityEntry is the deleted one) and leaves the deleted one out of the entries to save, as the relational
        // providers then send one UPDATE. Inserting would collide with the document still there, and sending a delete as well
        // would leave a moment with no document, so the document is replaced in one write (#109).
        if (entry.SharedIdentityEntry is { EntityState: EntityState.Deleted } removed)
        {
            return ConvertReplaced(entry, removed);
        }

        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);

        SetStoreGeneratedValues(entry);
        WriteEntity(writer, entry);

        return new MongoUpdate(entry, new InsertOneModel<BsonDocument>(document));
    }

    private static MongoUpdate ConvertDeleted(IUpdateEntry entry)
    {
        return new MongoUpdate(entry, new DeleteOneModel<BsonDocument>(CreateWhereFilter(entry)));
    }

    private static MongoUpdate ConvertModified(IUpdateEntry entry)
    {
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);

        var whereFilter = CreateWhereFilter(entry); // Before row version incrementation
        SetStoreGeneratedValues(entry);
        WriteEntity(writer, entry);

        var updateDefinition = new BsonDocumentUpdateDefinition<BsonDocument>(new BsonDocument("$set", document));
        return new MongoUpdate(entry, new UpdateOneModel<BsonDocument>(whereFilter, updateDefinition));
    }

    /// <summary>
    /// Replaces the document of <paramref name="removed"/> with the whole document of <paramref name="entry"/>, which has the
    /// same primary key. Like the relational providers' one UPDATE for the pair, it matches the document by its key and the
    /// concurrency token values <paramref name="removed"/> was loaded with (so a document changed or deleted since is a
    /// conflict, not an upsert), and everything the new entity maps is written, as for an insert.
    /// </summary>
    private static MongoUpdate ConvertReplaced(IUpdateEntry entry, IUpdateEntry removed)
    {
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);

        var whereFilter = CreateWhereFilter(entry, removed); // Before row version incrementation
        ContinueRowVersions(entry, removed);
        SetStoreGeneratedValues(entry);
        WriteEntity(writer, entry);

        return new MongoUpdate(entry, new ReplaceOneModel<BsonDocument>(whereFilter, document), removed);
    }

    // The new entity has not been stored, so its row version would start again and could equal the one other contexts loaded
    // with the document it replaces, letting their stale updates through. Carry on from the version that was replaced.
    private static void ContinueRowVersions(IUpdateEntry entry, IUpdateEntry removed)
    {
        foreach (var property in entry.EntityType.GetProperties())
        {
            if (property.IsRowVersion()
                && removed.EntityType.FindProperty(property.Name) is { } removedProperty
                && removedProperty.ClrType == property.ClrType
                && removedProperty.IsRowVersion())
            {
                entry.SetStoreGeneratedValue(property, removed.GetOriginalValue(removedProperty));
            }
        }
    }

    /// <summary>
    /// Creates the filter that finds the document of <paramref name="entry"/>: its key and the original values of its
    /// concurrency tokens. For a replacement the key comes from the entity added, but the tokens from
    /// <paramref name="tokenEntry"/>, the entity removed, which is the one the stored document was loaded as.
    /// </summary>
    private static FilterDefinition<BsonDocument> CreateWhereFilter(IUpdateEntry entry, IUpdateEntry? tokenEntry = null)
    {
        _ = entry.EntityType.FindPrimaryKey() ??
            throw new InvalidOperationException($"Cannot find the primary key for the entity: {entry.EntityType.Name}");

        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);

        writer.WriteStartDocument();
        WriteKeyProperties(writer, entry);
        WriteConcurrencyTokens(writer, tokenEntry ?? entry);
        writer.WriteEndDocument();

        return writer.Document.Elements
            .Select(element => Builders<BsonDocument>.Filter.Eq(element.Name, element.Value))
            .Aggregate<FilterDefinition<BsonDocument>?, FilterDefinition<BsonDocument>?>(null, (current, nextFilter)
                => current == null
                    ? nextFilter
                    : Builders<BsonDocument>.Filter.And(current, nextFilter))!;
    }

    private static void WriteConcurrencyTokens(IBsonWriter writer, IUpdateEntry entry)
    {
        var concurrencyTokens = entry.EntityType.GetProperties().Where(p => p.IsConcurrencyToken).ToArray();
        foreach (var property in concurrencyTokens)
        {
            WriteProperty(writer, entry.GetOriginalValue(property), property);
        }
    }

    private static void WriteEntity(IBsonWriter writer, IUpdateEntry entry, Func<IProperty, bool>? propertyFilter = null)
    {
        // Only the entry's own Modified pass writes just what changed. Inserts (no filter) and owned dependents
        // (_ => true, because their whole subdocument is rewritten as part of the owner) write everything.
        var onlyModified = propertyFilter == null && entry.EntityState == EntityState.Modified;
        if (onlyModified)
        {
            propertyFilter = entry.IsModified;
        }

        writer.WriteStartDocument();
        WriteKeyProperties(writer, entry);
        WriteNonKeyProperties(writer, entry, propertyFilter);
        WriteOwnedEntities(writer, entry);
        ComplexValueWriter.WriteComplexProperties(writer, entry, onlyModified);
        writer.WriteEndDocument();
    }

    private static IProperty? FindOrdinalKeyProperty(IEntityType entityType)
        => entityType.FindPrimaryKey()!.Properties.FirstOrDefault(p => p.GetElementName().Length == 0 && p.IsOwnedTypeOrdinalKey());

    private static void SetStoreGeneratedValues(IUpdateEntry entry)
    {
        foreach (var property in entry.EntityType.GetProperties())
        {
            if (entry.HasTemporaryValue(property))
            {
                entry.SetStoreGeneratedValue(property, entry.GetCurrentValue(property));
            }

            if (property.IsRowVersion())
            {
                entry.SetStoreGeneratedValue(property, entry.GetRowVersion(property));
            }
        }
    }

    private static void WriteKeyProperties(IBsonWriter writer, IUpdateEntry entry)
    {
        var keyProperties = entry.EntityType.FindPrimaryKey()?
            .Properties
            .Where(p => !p.IsOwnedTypeKey()).ToArray() ?? [];

        if (!keyProperties.Any()) return;

        var compositeKey = keyProperties.Length > 1;
        if (compositeKey)
        {
            writer.WriteName("_id");
            writer.WriteStartDocument();
        }

        foreach (var property in keyProperties)
        {
            WriteProperty(writer, entry.GetCurrentValue(property), property);
        }

        if (compositeKey)
        {
            writer.WriteEndDocument();
        }
    }

    internal static void WriteNonKeyProperties(IBsonWriter writer, IUpdateEntry entry, Func<IProperty, bool>? propertyFilter = null)
    {
        var properties = entry.EntityType.GetProperties()
            .Where(p => !p.IsPrimaryKey() && p.GetElementName() != "")
            .Where(p => propertyFilter == null || propertyFilter(p))
            .ToArray();

        foreach (var property in properties)
        {
            WriteProperty(writer, entry.GetCurrentValue(property), property);
        }
    }


    private static void WriteProperty(IBsonWriter writer, object? value, IProperty property)
    {
        var serializationInfo = BsonSerializerFactory.GetPropertySerializationInfo(property);
        writer.WriteName(serializationInfo.ElementPath?.Last() ?? serializationInfo.ElementName);
        var root = BsonSerializationContext.CreateRoot(writer);
        serializationInfo.Serializer.Serialize(root, value);
    }

    private static void WriteOwnedEntities(IBsonWriter writer, IUpdateEntry entry)
    {
        foreach (var navigation in entry.EntityType.GetNavigations())
        {
            var fk = navigation.ForeignKey;
            if (!fk.IsOwnership
                || navigation.IsOnDependent
                || fk.DeclaringEntityType.IsDocumentRoot())
            {
                continue;
            }

            writer.WriteName(navigation.TargetEntityType.GetContainingElementName());
            var embeddedValue = entry.GetCurrentValue(navigation);

            if (embeddedValue == null)
            {
                writer.WriteNull();
            }
            else
            {
                if (navigation.IsCollection)
                {
                    // Set temporary ordinals if existing ones are invalid
                    SetTemporaryOrdinals(entry, fk, embeddedValue);

                    writer.WriteStartArray();
                    var ordinal = 1;
                    foreach (var dependent in (IEnumerable)embeddedValue)
                    {
                        var embeddedEntry =
                            ((InternalEntityEntry)entry).StateManager.TryGetEntry(dependent,
                                navigation.ForeignKey.DeclaringEntityType)!;

                        // Owned entities have a synthetic key based on order, apply that here
                        var ordinalKeyProperty = FindOrdinalKeyProperty(embeddedEntry.EntityType);
                        if (ordinalKeyProperty != null && embeddedEntry.HasTemporaryValue(ordinalKeyProperty))
                        {
                            embeddedEntry.SetStoreGeneratedValue(ordinalKeyProperty, ordinal, setModified: false);
                        }

                        WriteEntity(writer, embeddedEntry, _ => true);
                        ordinal++;
                    }

                    writer.WriteEndArray();
                }
                else
                {
                    var embeddedEntry =
                        ((InternalEntityEntry)entry).StateManager.TryGetEntry(embeddedValue,
                            navigation.ForeignKey.DeclaringEntityType)!;
                    WriteEntity(writer, embeddedEntry, _ => true);
                }
            }
        }
    }

    private static void SetTemporaryOrdinals(IUpdateEntry entry, IForeignKey fk, object embeddedValue)
    {
        var embeddedOrdinal = 1;
        var ordinalKeyProperty = FindOrdinalKeyProperty(fk.DeclaringEntityType);
        if (ordinalKeyProperty == null) return;

        var stateManager = ((InternalEntityEntry)entry).StateManager;
        var shouldSetTemporaryKeys = false;
        foreach (var dependent in (IEnumerable)embeddedValue)
        {
            var embeddedEntry = stateManager.TryGetEntry(dependent, fk.DeclaringEntityType)!;

            if ((int)embeddedEntry.GetCurrentValue(ordinalKeyProperty)! != embeddedOrdinal
                && !embeddedEntry.HasTemporaryValue(ordinalKeyProperty))
            {
                // We have old persisted ordinals that are no longer valid
                // Set temporary ones to avoid key conflicts when creating new
                // non-temporary keys.
                shouldSetTemporaryKeys = true;
                break;
            }

            embeddedOrdinal++;
        }

        if (shouldSetTemporaryKeys)
        {
            var temporaryOrdinal = -1;
            foreach (var dependent in (IEnumerable)embeddedValue)
            {
                var embeddedEntry = stateManager.TryGetEntry(dependent, fk.DeclaringEntityType)!;
                embeddedEntry.SetTemporaryValue(ordinalKeyProperty, temporaryOrdinal, setModified: false);
                temporaryOrdinal--;
            }
        }
    }
}
