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
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.Query;

/// <summary>
/// Gives a tracked entity loaded by a query the discriminator values of its optional complex properties (EF10
/// <c>HasDiscriminator()</c>). The discriminator is a shadow property of the complex type that the stored subdocument carries
/// as an ordinary leaf, but a query tracks the entity with an empty shadow snapshot, so without this its current value would
/// be null, and EF reads a null discriminator as "the property is null": an <c>Update(entity)</c> would store
/// <c>Discriminator: null</c> and the property would read back null. Original and current are set to the same value, so
/// the entry stays unchanged.
/// </summary>
internal static class ComplexDiscriminatorSeeding
{
#if !EF8 && !EF9
    private static readonly ConditionalWeakTable<IEntityType, object> Cache = new();
    private static readonly object Yes = new();
    private static readonly object No = new();

    internal static InternalEntityEntry Seed(InternalEntityEntry entry)
    {
        var entityType = entry.EntityType;
        if (!Cache.TryGetValue(entityType, out var has))
        {
            has = HasDiscriminator(entityType) ? Yes : No;
            Cache.TryAdd(entityType, has);
        }

        if (ReferenceEquals(has, Yes))
        {
            var entityEntry = new EntityEntry(entry);
            foreach (var complexProperty in entityType.GetComplexProperties().Where(p => !p.IsCollection))
            {
                SeedProperty(entityEntry.ComplexProperty(complexProperty.Name), complexProperty);
            }
        }

        return entry;
    }

    private static bool HasDiscriminator(ITypeBase type)
        => type.GetComplexProperties().Any(
            p => !p.IsCollection && (p.ComplexType.FindDiscriminatorProperty() != null || HasDiscriminator(p.ComplexType)));

    private static void SeedProperty(ComplexPropertyEntry entry, IComplexProperty property)
    {
        if (entry.CurrentValue is null)
        {
            return;
        }

        if (property.ComplexType.FindDiscriminatorProperty() is { } discriminator
            && entry.Property(discriminator.Name) is { CurrentValue: null } discriminatorEntry
            && property.ComplexType.GetDiscriminatorValue() is { } value)
        {
            discriminatorEntry.OriginalValue = value;
            discriminatorEntry.CurrentValue = value;
            discriminatorEntry.IsModified = false;
        }

        foreach (var nested in property.ComplexType.GetComplexProperties().Where(n => !n.IsCollection))
        {
            SeedProperty(entry.ComplexProperty(nested.Name), nested);
        }
    }
#else
    internal static InternalEntityEntry Seed(InternalEntityEntry entry) => entry;
#endif
}
