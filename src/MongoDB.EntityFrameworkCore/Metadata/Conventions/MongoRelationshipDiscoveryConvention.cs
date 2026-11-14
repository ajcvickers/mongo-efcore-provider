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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace MongoDB.EntityFrameworkCore.Metadata.Conventions;

/// <summary>
/// A convention that ensures embedded objects are correctly configured as owned entities.
/// </summary>
public class MongoRelationshipDiscoveryConvention : RelationshipDiscoveryConvention
{
    /// <summary>
    /// Creates a <see cref="MongoRelationshipDiscoveryConvention"/>.
    /// </summary>
    /// <param name="dependencies">The <see cref="MongoRelationshipDiscoveryConvention"/> required by this convention.</param>
    public MongoRelationshipDiscoveryConvention(ProviderConventionSetBuilderDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <inheritdoc />
    protected override bool? ShouldBeOwned(Type targetType, IConventionModel model)
        => ShouldBeOwnedType(targetType, model);

#if EF8
    // DiscoverRelationships is not overridable on EF8, so check for ownership that has to be undone just before the events that
    // start relationship discovery.

    /// <inheritdoc />
    public override void ProcessEntityTypeAdded(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionContext<IConventionEntityTypeBuilder> context)
    {
        DemoteOwnedTypesReferencedFromOutsideTheirDocument(entityTypeBuilder.Metadata);
        base.ProcessEntityTypeAdded(entityTypeBuilder, context);
    }

    /// <inheritdoc />
    public override void ProcessForeignKeyRemoved(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionForeignKey foreignKey,
        IConventionContext<IConventionForeignKey> context)
    {
        DemoteOwnedTypesReferencedFromOutsideTheirDocument(entityTypeBuilder.Metadata);
        base.ProcessForeignKeyRemoved(entityTypeBuilder, foreignKey, context);
    }

    /// <inheritdoc />
    public override void ProcessForeignKeyOwnershipChanged(
        IConventionForeignKeyBuilder relationshipBuilder,
        IConventionContext<bool?> context)
    {
        DemoteOwnedTypesReferencedFromOutsideTheirDocument(relationshipBuilder.Metadata.DeclaringEntityType);
        base.ProcessForeignKeyOwnershipChanged(relationshipBuilder, context);
    }

    /// <inheritdoc />
    public override void ProcessEntityTypeBaseTypeChanged(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionEntityType? newBaseType,
        IConventionEntityType? oldBaseType,
        IConventionContext<IConventionEntityType> context)
    {
        DemoteOwnedTypesReferencedFromOutsideTheirDocument(entityTypeBuilder.Metadata);
        base.ProcessEntityTypeBaseTypeChanged(entityTypeBuilder, newBaseType, oldBaseType, context);
    }

    /// <inheritdoc />
    public override void ProcessNavigationAdded(
        IConventionNavigationBuilder navigationBuilder,
        IConventionContext<IConventionNavigationBuilder> context)
    {
        DemoteOwnedTypesReferencedFromOutsideTheirDocument(navigationBuilder.Metadata.DeclaringEntityType);
        base.ProcessNavigationAdded(navigationBuilder, context);
    }

    /// <inheritdoc />
    public override void ProcessNavigationRemoved(
        IConventionEntityTypeBuilder sourceEntityTypeBuilder,
        IConventionEntityTypeBuilder targetEntityTypeBuilder,
        string navigationName,
        MemberInfo? memberInfo,
        IConventionContext<string> context)
    {
        DemoteOwnedTypesReferencedFromOutsideTheirDocument(sourceEntityTypeBuilder.Metadata);
        DemoteOwnedTypesReferencedFromOutsideTheirDocument(targetEntityTypeBuilder.Metadata);
        base.ProcessNavigationRemoved(sourceEntityTypeBuilder, targetEntityTypeBuilder, navigationName, memberInfo, context);
    }

    /// <inheritdoc />
    public override void ProcessForeignKeyNullNavigationSet(
        IConventionForeignKeyBuilder relationshipBuilder,
        bool pointsToPrincipal,
        IConventionContext<IConventionNavigation> context)
    {
        DemoteOwnedTypesReferencedFromOutsideTheirDocument(relationshipBuilder.Metadata.DeclaringEntityType);
        base.ProcessForeignKeyNullNavigationSet(relationshipBuilder, pointsToPrincipal, context);
    }
#else
    /// <inheritdoc />
    protected override void DiscoverRelationships(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionContext context,
        bool discoverUnmatchedInverses = false)
    {
        DemoteOwnedTypesReferencedFromOutsideTheirDocument(entityTypeBuilder.Metadata);
        base.DiscoverRelationships(entityTypeBuilder, context, discoverUnmatchedInverses);
    }
#endif

    /// <inheritdoc />
    protected override IConventionEntityTypeBuilder? TryGetTargetEntityTypeBuilder(
        IConventionEntityTypeBuilder entityTypeBuilder,
        Type targetClrType,
        MemberInfo navigationMemberInfo,
        bool? shouldBeOwned = null,
        bool shouldCreate = true)
    {
        // A type that holds a collection of its would-be owner cannot be embedded in it: the collection makes the type the
        // principal of a one-to-many relationship, so the two are related documents, not parent and embedded child (#65). A navigation
        // between types of one hierarchy is not that: the target is the owner's base or derived type.
        if (shouldCreate
            && shouldBeOwned == null
            && !entityTypeBuilder.Metadata.Model.FindEntityTypes(targetClrType).Any()
            && ShouldBeOwned(targetClrType, entityTypeBuilder.Metadata.Model) == true
            && !targetClrType.IsAssignableFrom(entityTypeBuilder.Metadata.ClrType)
            && !entityTypeBuilder.Metadata.ClrType.IsAssignableFrom(targetClrType)
            && HasCollectionOf(targetClrType, entityTypeBuilder.Metadata.ClrType))
        {
            shouldBeOwned = false;
        }

        return base.TryGetTargetEntityTypeBuilder(entityTypeBuilder, targetClrType, navigationMemberInfo, shouldBeOwned, shouldCreate);
    }

    // An owned type is embedded in its owner's document, so nothing outside that document can navigate to it. When an entity type
    // navigates to a type that was embedded by convention elsewhere and the type navigates back, the two are in a relationship
    // and the type is a document of its own. This cannot be decided when the type is first reached, because the entity type
    // that refers to it may not have been added yet. Explicit configuration ([Owned], OwnsOne) is not overridden: the
    // data annotation source below only overrides ownership set by convention.
    private void DemoteOwnedTypesReferencedFromOutsideTheirDocument(IConventionEntityType entityType)
    {
        if (!entityType.IsInModel)
        {
            return;
        }

        List<IConventionEntityType>? toDemote = null;
        foreach (var (member, (targetClrType, _)) in NavigationCandidates(entityType))
        {
            foreach (var target in entityType.Model.FindEntityTypes(targetClrType))
            {
                if (target.IsOwned()
                    && target != entityType
                    && !entityType.IsInOwnershipPath(target)
                    && !target.IsInOwnershipPath(entityType)
                    && !HasOwnerOfClrType(target, entityType.ClrType)
                    && !HasOwnerOfClrType(entityType, target.ClrType)
                    && !IsOwnedThroughSameMember(target, member)
                    && NavigationCandidates(target)
                        .Any(candidate => candidate.Value.Type.IsAssignableFrom(entityType.ClrType)))
                {
                    (toDemote ??= []).Add(target);
                }
            }
        }

        // Something outside an owner's document also references the owned type if it holds a collection of an independent entity
        // type (one that was registered or configured explicitly, not merely reached through a navigation): the dependent documents hold a foreign key to it, which an embedded type cannot provide. EF Core cannot configure
        // that relationship from an owned type either, so this only changes models that would otherwise not build.
        foreach (var other in entityType.Model.GetEntityTypes())
        {
            if (other == entityType || other.HasSharedClrType && !other.IsOwned() || entityType.IsInOwnershipPath(other) || other.IsInOwnershipPath(entityType)
                || HasOwnerOfClrType(other, entityType.ClrType)
                || HasOwnerOfClrType(entityType, other.ClrType))
            {
                continue;
            }

            if (!entityType.IsOwned() && IsConfiguredExplicitly(entityType) && other.IsOwned() && HasCollectionOf(other.ClrType, entityType.ClrType))
            {
                (toDemote ??= []).Add(other);
            }
            else if (entityType.IsOwned() && !other.IsOwned() && IsConfiguredExplicitly(other) && HasCollectionOf(entityType.ClrType, other.ClrType))
            {
                (toDemote ??= []).Add(entityType);
            }
        }

        if (toDemote == null)
        {
            return;
        }

        foreach (var target in toDemote)
        {
            OwnedTypeDemotion.Demote(target);
        }
    }

    private static bool IsConfiguredExplicitly(IConventionEntityType entityType)
        => entityType.GetConfigurationSource() == ConfigurationSource.Explicit;

    // The same CLR type embedded from two different owners is two different owned entity types. A type that is owned by another
    // embedding of the entity type's own CLR type (or of a type in its hierarchy) belongs to that embedding's document, so it is not
    // outside this one.
    private static bool HasOwnerOfClrType(IConventionEntityType entityType, Type clrType)
    {
        for (var owner = entityType.FindOwnership()?.PrincipalEntityType; owner != null; owner = owner.FindOwnership()?.PrincipalEntityType)
        {
            if (owner.ClrType.IsAssignableFrom(clrType) || clrType.IsAssignableFrom(owner.ClrType))
            {
                return true;
            }
        }

        return false;
    }

    // A navigation that is inherited by several types is the same member in each of them. When the target was embedded through
    // that very member by another type, this type merely inherits the navigation: it is a sibling embedding, not an outside reference.
    private static bool IsOwnedThroughSameMember(IConventionEntityType target, MemberInfo member)
        => target.FindOwnership()?.PrincipalToDependent?.GetIdentifyingMemberInfo() is { } ownedThrough
            && ownedThrough.Name == member.Name
            && ownedThrough.DeclaringType == member.DeclaringType;

    private IEnumerable<KeyValuePair<PropertyInfo, (Type Type, bool? ShouldBeOwned)>> NavigationCandidates(IConventionEntityType entityType)
#if EF8
        => Dependencies.MemberClassifier.GetNavigationCandidates(entityType);
#else
        => Dependencies.MemberClassifier.GetNavigationCandidates(entityType, true);
#endif


    private static bool HasCollectionOf(Type holderType, Type elementType)
    {
        foreach (var property in holderType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propertyType = property.PropertyType;
            if (propertyType == typeof(string) || property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            var enumerable = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                ? propertyType
                : propertyType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerable != null
                && enumerable.GetGenericArguments()[0] is var item
                && item != typeof(object)
                && item.IsAssignableFrom(elementType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determine if a given entity type should be added as an owned entity if not already in the model.
    /// </summary>
    /// <param name="targetType">Target entity <see cref="Type"/>.</param>
    /// <param name="model">The <see cref="IConventionModel"/> being built.</param>
    /// <returns><see langword="true"/> if the type is to be owned, <see langword="false"/> otherwise.</returns>
    public static bool ShouldBeOwnedType(
        Type targetType,
        IConventionModel model)
    {
        // If the type is already in the model as an independent entity (e.g. has its own DbSet),
        // don't auto-configure it as owned/embedded - it has its own collection.
        var existingEntityType = model.FindEntityType(targetType);
        if (existingEntityType != null && existingEntityType.FindOwnership() == null)
        {
            return false;
        }

        // A type deriving from a root entity type is part of that root's hierarchy and is stored in its collection, so it
        // cannot also be an embedded document. EF Core only links a discovered type into a hierarchy if it is not owned,
        // so an owned derived type would be cut off from its base (#65). An explicit [Owned] / OwnsOne is left alone:
        // explicit configuration wins. The reverse (a base type of a root) is deliberately not handled here: a later
        // explicit HasBaseType(null) or OwnsOne can legitimately take the type out of the hierarchy, and EF Core already
        // rejects an explicit OwnsOne on a base type that has a non-owned derived type.
        if (!targetType.IsDefined(typeof(OwnedAttribute), inherit: false)
            && !model.IsOwned(targetType)
            && IsDerivedTypeOfRootEntityType(targetType, model))
        {
            return false;
        }

        return !targetType.IsGenericType
               || targetType == typeof(Dictionary<string, object>)
               || targetType.GetInterface(typeof(IEnumerable<>).Name) == null;
    }

    private static bool IsDerivedTypeOfRootEntityType(Type targetType, IConventionModel model)
    {
        foreach (var entityType in model.GetEntityTypes())
        {
            if (entityType.IsOwned()
                || entityType.HasSharedClrType
                || entityType.ClrType == targetType)
            {
                continue;
            }

            if (targetType.IsSubclassOf(entityType.ClrType))
            {
                return true;
            }
        }

        return false;
    }
}
