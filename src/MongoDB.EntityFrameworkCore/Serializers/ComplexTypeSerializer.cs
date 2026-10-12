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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.Bson.Serialization;
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.Serializers;

/// <summary>
/// Provides the interface between the EF Core <see cref="IReadOnlyComplexType"/> metadata and the MongoDB LINQ
/// provider's <see cref="IBsonDocumentSerializer"/> interface, so member lookups inside a complex property's
/// subdocument resolve to the element names and serializers the write path uses.
/// </summary>
/// <remarks>
/// Like <see cref="EntitySerializer{TValue}"/>, this serializer only answers member lookups. Whole-value
/// serialization (comparing a complex value by example) is not supported here, and materialization does not go
/// through serializers.
/// </remarks>
/// <typeparam name="TValue">The CLR type of the complex type (a class or a struct; never <see cref="Nullable{T}"/>).</typeparam>
internal class ComplexTypeSerializer<TValue> :
    IBsonSerializer<TValue>,
    IBsonDocumentSerializer
{
    private readonly IReadOnlyComplexType _complexType;
    private readonly BsonSerializerFactory _bsonSerializerFactory;

    /// <summary>
    /// Create a new instance of <see cref="ComplexTypeSerializer{TValue}"/>.
    /// </summary>
    /// <param name="complexType">The <see cref="IReadOnlyComplexType"/> this serializer relates to in EF Core.</param>
    /// <param name="bsonSerializerFactory">The <see cref="BsonSerializerFactory"/> to obtain nested complex serializers from.</param>
    public ComplexTypeSerializer(
        IReadOnlyComplexType complexType,
        BsonSerializerFactory bsonSerializerFactory)
    {
        ArgumentNullException.ThrowIfNull(complexType);
        ArgumentNullException.ThrowIfNull(bsonSerializerFactory);

        _complexType = complexType;
        _bsonSerializerFactory = bsonSerializerFactory;
    }

    /// <inheritdoc />
    public Type ValueType => typeof(TValue);

    /// <inheritdoc />
    public TValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => throw new NotImplementedException();

    /// <inheritdoc />
    object? IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => Deserialize(context, args);

    /// <inheritdoc />
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TValue value)
    {
        if (value == null)
        {
            context.Writer.WriteNull();
            return;
        }

        // Matching a complex value by example could mismatch on unmapped fields or default values in stored
        // subdocuments, so whole-value comparison is not rendered through the serializer.
        throw new NotSupportedException(
            $"Comparing complex type '{_complexType.DisplayName()}' as a whole value is not supported. Compare its properties instead.");
    }

    /// <inheritdoc />
    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        => Serialize(context, args, (TValue)value);

    /// <inheritdoc />
    public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo? serializationInfo)
    {
        var property = _complexType.FindProperty(memberName);
        if (property != null)
        {
            serializationInfo = BsonSerializerFactory.GetPropertySerializationInfo(property);
            return true;
        }

        var complexProperty = _complexType.FindComplexProperty(memberName);
        if (complexProperty != null)
        {
            serializationInfo = _bsonSerializerFactory.GetComplexPropertySerializationInfo(complexProperty);
            return true;
        }

        serializationInfo = default;
        return false;
    }
}
