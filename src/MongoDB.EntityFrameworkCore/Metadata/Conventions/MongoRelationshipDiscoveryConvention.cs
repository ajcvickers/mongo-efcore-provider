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
using System.ComponentModel.DataAnnotations.Schema;
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
        var model = entityTypeBuilder.Metadata.Model;
        if (shouldCreate
            && shouldBeOwned == null
            && !model.FindEntityTypes(targetClrType).Any()
            && ShouldBeOwned(targetClrType, model) == true
            && !targetClrType.IsAssignableFrom(entityTypeBuilder.Metadata.ClrType)
            && !entityTypeBuilder.Metadata.ClrType.IsAssignableFrom(targetClrType)
            && HoldsCollectionOf(targetClrType, entityTypeBuilder.Metadata.ClrType, model))
        {
            shouldBeOwned = false;
        }

        return base.TryGetTargetEntityTypeBuilder(entityTypeBuilder, targetClrType, navigationMemberInfo, shouldBeOwned, shouldCreate);
    }

    // An owned type is embedded in its owner's document, so nothing outside that document can navigate to it. When an entity type
    // navigates to a type that was embedded by convention elsewhere and the type navigates back, the two are in a relationship
    // and the type is a document of its own. This cannot be decided when the type is first reached, because the entity type
    // that refers to it may not have been added yet. Explicit configuration ([Owned], OwnsOne) is not overridden: the
    // demotion itself only undoes ownership that came from a convention.
    private void DemoteOwnedTypesReferencedFromOutsideTheirDocument(IConventionEntityType entityType)
    {
        if (!entityType.IsInModel)
        {
            return;
        }

        List<IConventionEntityType>? toDemote = null;
        foreach (var (member, (targetClrType, _)) in NavigationCandidates(entityType))
        {
            if (IsUnmapped(entityType, member))
            {
                continue;
            }

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
                        .Any(candidate => !IsUnmapped(target, candidate.Key) && candidate.Value.Type.IsAssignableFrom(entityType.ClrType)))
                {
                    (toDemote ??= []).Add(target);
                }
            }
        }

        // A type that holds a collection of an independent entity type (one that was registered or configured explicitly, not
        // merely reached through a navigation) is also referenced from outside its owner's document: the dependent documents
        // hold a foreign key to it, which an embedded type cannot provide. EF Core cannot configure that relationship from an
        // owned type either, so this changes models that would otherwise not build.
        if (entityType.IsOwned())
        {
            foreach (var (member, (collectionClrType, _)) in NavigationCandidates(entityType))
            {
                if (IsCollectionNavigation(member, collectionClrType)
                    && !IsUnmapped(entityType, member)
                    && HoldsCollectionOfIndependentRoot(entityType, collectionClrType))
                {
                    (toDemote ??= []).Add(entityType);
                    break;
                }
            }
        }
        else if (IsConfiguredExplicitly(entityType))
        {
            // A type that derives from this root and was embedded by convention is part of this root's hierarchy, whichever
            // of the two was registered first. EF Core only links a derived type to its base if it is not owned.
            foreach (var derived in OwnedTypesDerivedFrom(entityType))
            {
                (toDemote ??= []).Add(derived);
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

    private static bool IsRelatedByOwnership(IConventionEntityType first, IConventionEntityType second)
        => first.IsInOwnershipPath(second)
            || second.IsInOwnershipPath(first)
            || HasOwnerOfClrType(first, second.ClrType)
            || HasOwnerOfClrType(second, first.ClrType);

    // The owned type holds a collection of an entity type that was configured explicitly and is not owned. Types reached through
    // a navigation do not count because they are owned themselves unless something else says otherwise.
    private static bool HoldsCollectionOfIndependentRoot(IConventionEntityType owned, Type collectionClrType)
    {
        foreach (var candidate in owned.Model.FindEntityTypes(collectionClrType))
        {
            foreach (var root in candidate.GetDerivedTypesInclusive())
            {
                if (!root.IsOwned() && IsConfiguredExplicitly(root) && !IsRelatedByOwnership(owned, root))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<IConventionEntityType> OwnedTypesDerivedFrom(IConventionEntityType root)
    {
        if (root.BaseType != null)
        {
            yield break;
        }

        foreach (var other in root.Model.GetEntityTypes())
        {
            if (other.IsOwned()
                && other.ClrType.IsSubclassOf(root.ClrType)
                && !other.ClrType.IsDefined(typeof(OwnedAttribute), inherit: false)
                && !root.Model.IsOwned(other.ClrType))
            {
                yield return other;
            }
        }
    }

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

    private Type? FindCandidateNavigationType(PropertyInfo member, IConventionModel model)
#if EF8
        => Dependencies.MemberClassifier.FindCandidateNavigationPropertyType(member, model, out _);
#elif EF11
        => Dependencies.MemberClassifier.IsCandidateNavigationProperty(member, model, true, out var elementType, out _, out _)
            ? elementType ?? member.PropertyType
            : null;
#else
        => Dependencies.MemberClassifier.FindCandidateNavigationPropertyType(member, model, true, out _);
#endif

    // A navigation candidate that EF Core will not map: [NotMapped], or ignored for this entity type or one of its base types.
    private static bool IsUnmapped(IConventionEntityType entityType, MemberInfo member)
        => member.IsDefined(typeof(NotMappedAttribute), inherit: true) || entityType.IsIgnored(member.Name);

    // For a navigation candidate, the candidate type is the element type of a collection and the property type itself otherwise.
    private static bool IsCollectionNavigation(PropertyInfo member, Type candidateType)
        => member.PropertyType != candidateType;

    // Does the CLR type, which is not in the model yet, hold a collection that EF Core would map to the given CLR type or one of
    // its base types? Anything [NotMapped] is not a candidate: it is not part of the model, so nothing refers to its elements.
    private bool HoldsCollectionOf(Type holderClrType, Type elementClrType, IConventionModel model)
    {
        foreach (var member in holderClrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (member.IsDefined(typeof(NotMappedAttribute), inherit: true)
                || FindCandidateNavigationType(member, model) is not { } candidateType
                || !IsCollectionNavigation(member, candidateType))
            {
                continue;
            }

            if (candidateType.IsAssignableFrom(elementClrType))
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
        for (var baseType = targetType.BaseType; baseType != null && baseType != typeof(object); baseType = baseType.BaseType)
        {
            if (model.FindEntityType(baseType) is { } entityType && !entityType.IsOwned())
            {
                return true;
            }
        }

        return false;
    }
}
