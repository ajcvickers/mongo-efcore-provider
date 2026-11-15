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

using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace MongoDB.EntityFrameworkCore.Metadata.Conventions;

internal static class OwnedTypeDemotion
{
    /// <summary>
    /// Makes an entity type that was embedded by convention an independent (root) entity type. Ownership that came from
    /// <c>OwnsOne</c>, <c>OwnsMany</c>, <c>[Owned]</c> or <c>Owned&lt;T&gt;()</c> is never undone: explicit configuration wins.
    /// </summary>
    /// <remarks>
    /// <see cref="Microsoft.EntityFrameworkCore.Metadata.Builders.IConventionEntityTypeBuilder"/> cannot do this, and
    /// <c>InternalEntityTypeBuilder.IsOwned(false, ...)</c> refuses once something has configured the entity type explicitly
    /// (for example <c>UsingEntity&lt;T&gt;()</c> on a join entity type), even if the ownership itself was only ever a convention's
    /// guess. So the ownership is removed here the way <c>IsOwned(false)</c> removes it, after checking where it came from.
    ///
    /// Demotion only ever goes one way, so the events it raises cannot undo it and building the model terminates. The entity type
    /// stays in the model, and the convention does not own an entity type that is already in the model as an independent one.
    /// The one way ownership could come back is for the same CLR type to be embedded again from another owner, which is why a
    /// CLR type that is embedded from more than one place is left alone.
    /// </remarks>
    /// <returns><see langword="true"/> if the entity type was owned by convention and is now independent.</returns>
    public static bool Demote(IConventionEntityType entityType)
    {
        if (!entityType.IsInModel
            || !entityType.IsOwned()
            || entityType is not EntityType owned
            || owned.FindOwnership() is not { } ownership
            || ownership.GetConfigurationSource() != ConfigurationSource.Convention
            // Explicit ownership (OwnsOne, [Owned], Owned<T>()) normally shows in the source of the ownership itself, which is
            // checked above. These say the same thing from the type's side, so that ownership that is explicit in any form is
            // never undone, whatever order EF Core records it in.
            || owned.Model.FindIsOwnedConfigurationSource(owned.ClrType) != null
            || owned.Model.Configuration?.GetConfigurationType(owned.ClrType) == TypeConfigurationType.OwnedEntityType
            || owned.ClrType.IsDefined(typeof(OwnedAttribute), inherit: false)
            // The CLR type is embedded from more than one place. EF Core names such entity types after their owner and navigation
            // and never lets the CLR type be an independent entity type again, so there is nothing to demote to. Explicit
            // OwnsOne/OwnsMany calls that are still to come would also find the type already configured as not owned.
            || owned.HasSharedClrType)
        {
            return false;
        }

        owned.SetIsOwned(false);
        owned.Builder.HasNoRelationship(ownership, ConfigurationSource.Convention);
        RemoveKeyMadeForOwnership(owned);

        // An owned type's derived types are owned with it, so they stop being owned with it: a hierarchy is either all owned or
        // all independent.
        foreach (EntityType derivedType in owned.GetDerivedTypes().ToList())
        {
            derivedType.SetIsOwned(false);
            if (derivedType.FindDeclaredOwnership() is { } derivedOwnership)
            {
                derivedType.Builder.HasNoRelationship(derivedOwnership, ConfigurationSource.Convention);
            }

            RemoveKeyMadeForOwnership(derivedType);
        }

        return true;
    }

    // An owned type has no key of its own: it is given a shadow one made of the properties that point at its owner (plus an
    // ordinal if it is in a collection). Such a key means nothing for a document of its own. When the type has a key property of
    // its own, key discovery has already replaced it. When it does not, remove what is left, so that the model is rejected for
    // the missing key instead of silently storing a document whose key is a reference to a parent that is no longer its owner.
    private static void RemoveKeyMadeForOwnership(EntityType entityType)
    {
        if (entityType.BaseType != null
            || entityType.FindPrimaryKey() is not { } key
            || key.GetConfigurationSource() != ConfigurationSource.Convention
            || key.Properties.Any(p => !p.IsShadowProperty()))
        {
            return;
        }

        var properties = key.Properties.ToList();
        entityType.Builder.HasNoKey(key, ConfigurationSource.Convention);
        foreach (var property in properties)
        {
            if (property.GetConfigurationSource() == ConfigurationSource.Convention
                && property.DeclaringType == entityType
                && !property.IsKey()
                && !property.IsForeignKey()
                && !property.IsIndex())
            {
                entityType.Builder.RemoveProperty(property, ConfigurationSource.Convention);
            }
        }
    }
}
