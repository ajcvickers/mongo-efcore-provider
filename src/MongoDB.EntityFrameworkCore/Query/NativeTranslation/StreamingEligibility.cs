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
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>Decides whether an entity type can be materialized by the forward-only streaming reader.</summary>
internal static class StreamingEligibility
{
    /// <summary>
    /// Eligible: a simple single-property primary key; navigations are only single (reference) owned
    /// sub-documents whose target types are themselves eligible. No owned collections, no
    /// cross-collection / non-owned navigations, no TPH discriminator hierarchy. Scalar and mapped-array
    /// properties are always fine (read via their serializers).
    /// </summary>
    public static bool IsEligible(IEntityType entityType)
        => IsEligible(entityType, new HashSet<IEntityType>());

    private static bool IsEligible(IEntityType entityType, HashSet<IEntityType> visiting)
    {
        if (!visiting.Add(entityType))
        {
            return true; // already validating this type (avoid cycles)
        }

        // No discriminator hierarchy (single concrete type only).
        if (entityType.BaseType != null || entityType.GetDirectlyDerivedTypes().Any())
        {
            return false;
        }

        // Simple single-property primary key.
        var pk = entityType.FindPrimaryKey();
        if (pk == null || pk.Properties.Count != 1)
        {
            return false;
        }

        // Only single (reference) owned navigations, to eligible owned types. (A required owned reference is
        // still eligible — the rewriter reproduces EF's "required but missing" throw via the present flag;
        // see MongoStreamingEntityMaterializerRewriter.RewriteOwnedNavigation.)
        foreach (var navigation in entityType.GetNavigations())
        {
            if (navigation.IsCollection
                || !navigation.TargetEntityType.IsOwned()
                || !IsEligible(navigation.TargetEntityType, visiting))
            {
                return false;
            }
        }

        // Skip-navigations make it ineligible.
        if (entityType.GetSkipNavigations().Any())
        {
            return false;
        }

        return true;
    }
}
