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
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Expressions;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// Appends the <c>$lookup</c>/<c>$unwind</c> aggregation stages for cross-collection reference
/// <c>Include</c>s onto a native pipeline. Mirrors the simple-form BSON the driver-LINQ path emits
/// (see <c>MongoEFToLinqTranslatingExpressionVisitor.EmitLookupStages</c>): a
/// <c>{ from, localField, foreignField, as }</c> <c>$lookup</c> followed by a
/// <c>{ path, preserveNullAndEmptyArrays: true }</c> <c>$unwind</c>.
/// </summary>
internal static class NativeLookupStages
{
    /// <summary>
    /// Appends a <c>$lookup</c> + <c>$unwind</c> pair for each pending reference lookup on
    /// <paramref name="queryExpression"/> to <paramref name="pipeline"/>. Only single-level reference
    /// lookups are supported in the native pipeline; anything else throws
    /// <see cref="NativeTranslationNotSupportedException"/> so the caller falls back to the driver-LINQ path.
    /// </summary>
    /// <param name="pipeline">The native pipeline being assembled; lookup stages are appended in place.</param>
    /// <param name="queryExpression">The query expression carrying the pending lookups.</param>
    internal static void AppendReferenceLookupStages(List<BsonDocument> pipeline, MongoQueryExpression queryExpression)
    {
        var referenceLookups = queryExpression.GetStreamingReferenceLookups();

        // A join query whose joins are not all expressible as single-level reference lookups (e.g. a
        // transitive join, or a join shape GetStreamingReferenceLookups could not map to a direct root
        // reference navigation) yields fewer reference lookups than inner collections. The native pipeline
        // cannot reproduce those joins, so emitting a partial pipeline would silently drop the join and
        // return wrong results — fall back to the driver-LINQ path instead.
        if (queryExpression.IsJoinQuery && referenceLookups.Count < queryExpression.InnerCollections.Count)
        {
            throw new NativeTranslationNotSupportedException(
                "Native pipeline does not support this join shape (only single-level reference includes).");
        }

        foreach (var lookup in referenceLookups)
        {
            // Only single-level REFERENCE lookups are supported natively in this slice.
            if (!lookup.IsReference
                || lookup.PipelineStages.Count > 0                  // filtered include -> fall back
                || lookup.LocalField.StartsWith("_lookup_"))        // transitive/nested lookup -> fall back
            {
                throw new NativeTranslationNotSupportedException(
                    $"Native pipeline does not support lookup for navigation '{lookup.Navigation.Name}' (only single-level reference includes).");
            }

            pipeline.Add(new BsonDocument("$lookup", new BsonDocument
            {
                { "from", lookup.From },
                { "localField", lookup.LocalField },
                { "foreignField", lookup.ForeignField },
                { "as", lookup.As }
            }));
            pipeline.Add(new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$" + lookup.As },
                { "preserveNullAndEmptyArrays", true }
            }));
        }
    }
}
