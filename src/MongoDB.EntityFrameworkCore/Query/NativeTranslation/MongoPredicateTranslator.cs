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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.EntityFrameworkCore.Serializers;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>Translates a <c>Where</c> predicate lambda body into a MongoDB <c>$match</c> filter document.</summary>
internal sealed class MongoPredicateTranslator
{
    private readonly IEntityType _entityType;
    private readonly QueryContext _queryContext;

    public MongoPredicateTranslator(IEntityType entityType, QueryContext queryContext)
    {
        _entityType = entityType;
        _queryContext = queryContext;
    }

    /// <summary>Translate a predicate body to a filter document. Throws <see cref="NativeTranslationNotSupportedException"/> on unsupported nodes.</summary>
    public BsonDocument Translate(Expression body)
        => TranslateNode(Unwrap(body));

    private static Expression Unwrap(Expression e)
        => e is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u ? Unwrap(u.Operand) : e;

    private BsonDocument TranslateNode(Expression node)
    {
        switch (node)
        {
            case BinaryExpression { NodeType: ExpressionType.AndAlso } and:
                return new BsonDocument("$and", new BsonArray { TranslateNode(Unwrap(and.Left)), TranslateNode(Unwrap(and.Right)) });

            case BinaryExpression { NodeType: ExpressionType.OrElse } orElse:
                return new BsonDocument("$or", new BsonArray { TranslateNode(Unwrap(orElse.Left)), TranslateNode(Unwrap(orElse.Right)) });

            case BinaryExpression be when IsComparison(be.NodeType):
                return TranslateComparison(be);

            case UnaryExpression { NodeType: ExpressionType.Not } not when NativeExpressionHelpers.TryResolveMemberProperty(Unwrap(not.Operand), _entityType, out var notProp, out var notElement):
                // !boolProperty => { field: false }
                return new BsonDocument(notElement, ToBsonValue(notProp!, false));

            default:
                // bare boolean property: c.Active => { field: true }
                if (NativeExpressionHelpers.TryResolveMemberProperty(node, _entityType, out var prop, out var element) && prop!.ClrType == typeof(bool))
                    return new BsonDocument(element, ToBsonValue(prop, true));
                throw NotSupported(node);
        }
    }

    private BsonDocument TranslateComparison(BinaryExpression be)
    {
        IProperty? property;
        string? element;
        Expression valueNode;
        if (NativeExpressionHelpers.TryResolveMemberProperty(Unwrap(be.Left), _entityType, out property, out element))
            valueNode = Unwrap(be.Right);
        else if (NativeExpressionHelpers.TryResolveMemberProperty(Unwrap(be.Right), _entityType, out property, out element))
            valueNode = Unwrap(be.Left);
        else
            throw NotSupported(be);

        var value = ToBsonValue(property!, NativeExpressionHelpers.EvaluateValue(valueNode, _queryContext));
        var op = be.NodeType switch
        {
            ExpressionType.Equal => null,
            ExpressionType.NotEqual => "$ne",
            ExpressionType.LessThan => "$lt",
            ExpressionType.LessThanOrEqual => "$lte",
            ExpressionType.GreaterThan => "$gt",
            ExpressionType.GreaterThanOrEqual => "$gte",
            _ => throw NotSupported(be)
        };
        return op is null
            ? new BsonDocument(element!, value)
            : new BsonDocument(element!, new BsonDocument(op, value));
    }

    private static bool IsComparison(ExpressionType t)
        => t is ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.LessThan
            or ExpressionType.LessThanOrEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual;

    // Convert a CLR value to a BsonValue using the property's serializer (correct element representation).
    private static BsonValue ToBsonValue(IProperty property, object? value)
    {
        var info = BsonSerializerFactory.GetPropertySerializationInfo(property);
        var doc = new BsonDocument();
        using (var writer = new BsonDocumentWriter(doc))
        {
            writer.WriteStartDocument();
            writer.WriteName("v");
            info.Serializer.Serialize(BsonSerializationContext.CreateRoot(writer), value);
            writer.WriteEndDocument();
        }
        return doc["v"];
    }

    private static NativeTranslationNotSupportedException NotSupported(Expression node)
        => new($"Native predicate translation does not support: {node.NodeType} ({node}).");
}
