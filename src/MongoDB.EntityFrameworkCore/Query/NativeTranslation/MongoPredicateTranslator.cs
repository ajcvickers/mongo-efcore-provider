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
using System.Globalization;
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
                return CombineAnd(TranslateNode(Unwrap(and.Left)), TranslateNode(Unwrap(and.Right)));

            case BinaryExpression { NodeType: ExpressionType.OrElse } orElse:
                return CombineOr(TranslateNode(Unwrap(orElse.Left)), TranslateNode(Unwrap(orElse.Right)));

            case BinaryExpression be when IsComparison(be.NodeType):
                return TranslateComparison(be);

            case UnaryExpression { NodeType: ExpressionType.Not } not when NativeExpressionHelpers.TryResolveMemberProperty(Unwrap(not.Operand), _entityType, out var notProp, out var notElement):
                // !boolProperty => { field: { $ne: true } }. This matches the driver-LINQ rendering and is
                // correct for missing/null fields (a plain { field: false } would not match those).
                return new BsonDocument(notElement, new BsonDocument("$ne", ToBsonValue(notProp!, true)));

            default:
                // bare boolean property: c.Active => { field: true }
                if (NativeExpressionHelpers.TryResolveMemberProperty(node, _entityType, out var prop, out var element) && prop!.ClrType == typeof(bool))
                    return new BsonDocument(element, ToBsonValue(prop, true));
                throw NotSupported(node);
        }
    }

    /// <summary>
    /// Combine two filter documents with AND, merging them into a single document when every top-level key is
    /// distinct (and none is an operator like <c>$and</c>/<c>$or</c>). This mirrors the driver-LINQ rendering,
    /// which only falls back to an explicit <c>$and</c> array when keys would collide. Nested <c>$and</c>
    /// operands are flattened so chained predicates do not nest redundantly.
    /// </summary>
    public static BsonDocument CombineAnd(BsonDocument left, BsonDocument right)
    {
        var clauses = new List<BsonDocument>();
        AddAndOperand(clauses, left);
        AddAndOperand(clauses, right);

        var merged = new BsonDocument();
        foreach (var clause in clauses)
        {
            // A clause is mergeable only if it is a single field whose name is not an operator.
            if (clause.ElementCount != 1 || clause.GetElement(0).Name.StartsWith('$'))
                return new BsonDocument("$and", new BsonArray(clauses));

            var element = clause.GetElement(0);
            if (!merged.Contains(element.Name))
            {
                merged.Add(element);
                continue;
            }

            // Same field appears twice (e.g. x > a && x < b). The driver merges the operator sub-documents
            // into one field: { x: { $gt: a, $lt: b } }. This is only valid when both values are pure operator
            // documents with no overlapping operator; otherwise fall back to an explicit $and array.
            if (TryMergeOperatorDocs(merged[element.Name], element.Value, out var combined))
                merged[element.Name] = combined;
            else
                return new BsonDocument("$and", new BsonArray(clauses));
        }

        return merged;
    }

    private static bool TryMergeOperatorDocs(BsonValue existing, BsonValue addition, out BsonValue combined)
    {
        combined = BsonNull.Value;
        if (existing is not BsonDocument ed || addition is not BsonDocument ad)
            return false;
        if (!IsAllOperators(ed) || !IsAllOperators(ad))
            return false;

        var result = new BsonDocument();
        result.AddRange(ed);
        foreach (var op in ad)
        {
            if (result.Contains(op.Name))
                return false; // overlapping operator (e.g. two $gt) cannot merge
            result.Add(op);
        }

        combined = result;
        return true;
    }

    private static bool IsAllOperators(BsonDocument doc)
    {
        if (doc.ElementCount == 0)
            return false;
        foreach (var e in doc)
        {
            if (!e.Name.StartsWith('$'))
                return false;
        }

        return true;
    }

    private static void AddAndOperand(List<BsonDocument> clauses, BsonDocument doc)
    {
        if (doc.ElementCount == 1 && doc.GetElement(0).Name == "$and" && doc[0] is BsonArray array)
        {
            foreach (var item in array)
                clauses.Add((BsonDocument)item);
        }
        else
        {
            clauses.Add(doc);
        }
    }

    /// <summary>
    /// Combine two filter documents with OR into a single flat <c>$or</c> array, flattening any nested
    /// <c>$or</c> operands so chained <c>OrElse</c> predicates render as one array (matching driver-LINQ).
    /// </summary>
    private static BsonDocument CombineOr(BsonDocument left, BsonDocument right)
    {
        var clauses = new BsonArray();
        AddOrOperand(clauses, left);
        AddOrOperand(clauses, right);
        return new BsonDocument("$or", clauses);
    }

    private static void AddOrOperand(BsonArray clauses, BsonDocument doc)
    {
        if (doc.ElementCount == 1 && doc.GetElement(0).Name == "$or" && doc[0] is BsonArray array)
        {
            foreach (var item in array)
                clauses.Add(item);
        }
        else
        {
            clauses.Add(doc);
        }
    }

    private BsonDocument TranslateComparison(BinaryExpression be)
    {
        IProperty? property;
        string? element;
        Expression valueNode;
        Expression memberOperand;
        if (NativeExpressionHelpers.TryResolveMemberProperty(Unwrap(be.Left), _entityType, out property, out element))
        {
            memberOperand = be.Left;
            valueNode = Unwrap(be.Right);
        }
        else if (NativeExpressionHelpers.TryResolveMemberProperty(Unwrap(be.Right), _entityType, out property, out element))
        {
            memberOperand = be.Right;
            valueNode = Unwrap(be.Left);
        }
        else
        {
            throw NotSupported(be);
        }

        // A numeric cast on the member side (e.g. (double)decimalProp > 100.0) changes the comparison type.
        // The driver compares in the cast type; serializing the value with the property's own serializer would
        // produce the wrong representation (e.g. $numberDecimal instead of a double). Fall back in that case.
        if (HasNumericConvert(memberOperand, property!.ClrType))
            throw new NativeTranslationNotSupportedException($"Native predicate translation does not support a cast on member '{property.Name}'.");

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

    // True when the operand wraps the member in a Convert/ConvertChecked to a different (non-nullable-widening)
    // type — i.e. a cast that changes the comparison semantics. A plain nullable<->underlying convert is benign.
    private static bool HasNumericConvert(Expression operand, Type propertyClrType)
    {
        var underlying = Nullable.GetUnderlyingType(propertyClrType) ?? propertyClrType;
        while (operand is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
        {
            var to = Nullable.GetUnderlyingType(u.Type) ?? u.Type;
            if (to != underlying)
                return true;
            operand = u.Operand;
        }

        return false;
    }

    private static bool IsComparison(ExpressionType t)
        => t is ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.LessThan
            or ExpressionType.LessThanOrEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual;

    // Convert a CLR value to a BsonValue using the property's serializer (correct element representation).
    // EF may hand us a value whose CLR type does not exactly match the property serializer's expected type
    // (e.g. an Int32/Int64 literal for a UInt16/UInt32 property, or a widened closure parameter). The
    // property serializer casts hard and throws InvalidCastException on a mismatch. Coerce the value to the
    // property's CLR type first, and treat any remaining serialization failure as "not natively supported"
    // so the query falls back to the driver-LINQ path instead of crashing or mistranslating.
    private static BsonValue ToBsonValue(IProperty property, object? value)
    {
        var info = BsonSerializerFactory.GetPropertySerializationInfo(property);
        try
        {
            value = CoerceToPropertyType(property.ClrType, value);
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
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or InvalidOperationException)
        {
            throw new NativeTranslationNotSupportedException(
                $"Native predicate translation cannot serialize value '{value}' for property '{property.Name}'.");
        }
    }

    // Coerce a CLR value to the property's CLR type so the property serializer (which casts hard to its
    // exact type) accepts it. Returns the value unchanged if no safe coercion applies; the serializer will
    // then either accept it or throw, which the caller maps to a fallback.
    private static object? CoerceToPropertyType(Type clrType, object? value)
    {
        if (value is null)
            return null;

        var target = Nullable.GetUnderlyingType(clrType) ?? clrType;
        var valueType = value.GetType();
        if (valueType == target)
            return value;

        if (target.IsEnum && Enum.GetUnderlyingType(target) is var enumBase && (valueType == enumBase || value is IConvertible))
            return Enum.ToObject(target, value);

        if (value is IConvertible && (target.IsPrimitive || target == typeof(decimal)))
            return Convert.ChangeType(value, target, CultureInfo.InvariantCulture);

        return value;
    }

    private static NativeTranslationNotSupportedException NotSupported(Expression node)
        => new($"Native predicate translation does not support: {node.NodeType} ({node}).");
}
