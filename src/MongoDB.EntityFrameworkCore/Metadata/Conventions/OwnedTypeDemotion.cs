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
    /// </remarks>
    /// <returns><see langword="true"/> if the entity type was owned by convention and is now independent.</returns>
    public static bool Demote(IConventionEntityType entityType)
    {
        if (!entityType.IsInModel
            || !entityType.IsOwned()
            || entityType is not EntityType owned
            || owned.FindOwnership() is not { } ownership
            || ownership.GetConfigurationSource() != ConfigurationSource.Convention
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

        foreach (EntityType derivedType in owned.GetDerivedTypes().ToList())
        {
            derivedType.SetIsOwned(false);
            if (derivedType.FindDeclaredOwnership() is { } derivedOwnership)
            {
                derivedType.Builder.HasNoRelationship(derivedOwnership, ConfigurationSource.Convention);
            }
        }

        return true;
    }
}
