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

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// Shared expression-resolution helpers used by the native MQL translators
/// (<see cref="MongoPredicateTranslator"/> and <see cref="MongoPipelineTranslator"/>).
/// </summary>
internal static class NativeExpressionHelpers
{
    // Property access is a member access off the lambda parameter: c.PropName.
    public static bool TryResolveMemberProperty(Expression node, IEntityType entityType, out IProperty? property, out string? element)
    {
        property = null;
        element = null;
        if (node is MemberExpression { Expression: ParameterExpression } me)
        {
            property = entityType.FindProperty(me.Member.Name);
        }

        if (property is null)
            return false;

        // A component of a composite primary key is stored nested under "_id" (e.g. { _id: { Key1, Key2 } }),
        // so its top-level element name does not address the stored field. The driver-LINQ path resolves the
        // dotted "_id.<name>" path; the native translator does not, so refuse it here and let the query fall
        // back rather than emit a $match against a non-existent top-level field (which silently returns nothing).
        if (property.IsPrimaryKey() && property.FindContainingPrimaryKey()!.Properties.Count > 1)
            return false;

        element = property.GetElementName();
        return true;
    }

    // Value nodes are EF query parameters (QueryParameterExpression) or literal constants.
    public static object? EvaluateValue(Expression node, QueryContext queryContext)
    {
        switch (node)
        {
            case ConstantExpression c:
                return c.Value;
#if EF8 || EF9
            // In EF8/EF9 query parameters are plain ParameterExpressions keyed in ParameterValues.
            case ParameterExpression p when p.Name is not null && queryContext.ParameterValues.TryGetValue(p.Name, out var pv):
                return pv;
#else
            case QueryParameterExpression qp when queryContext.Parameters.TryGetValue(qp.Name, out var pv):
                return pv;
#endif
            default:
                throw new NativeTranslationNotSupportedException($"Native translation does not support value node: {node.NodeType} ({node}).");
        }
    }
}
