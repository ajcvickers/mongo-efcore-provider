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
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.Serializers;

/// <summary>
/// Serializes a complex type's CLR value as a BSON sub-document whose elements are named from the complex type's
/// EF metadata: leaf properties by their configured element name, nested complex properties recursively, and
/// collection complex properties as arrays. Unlike a driver class map this honors
/// <see cref="MongoAnnotationNames.ElementName"/> annotations and never applies driver conventions such as naming
/// a member <c>Id</c> to <c>_id</c> — complex types have no keys.
/// </summary>
/// <typeparam name="TValue">The complex CLR type being handled by this serializer.</typeparam>
internal sealed class ComplexTypeSerializer<TValue> :
    IBsonSerializer<TValue>,
    IBsonDocumentSerializer
{
    private sealed class MemberBinding
    {
        public MemberBinding(string memberName, string elementName, IBsonSerializer serializer, PropertyInfo propertyInfo)
        {
            MemberName = memberName;
            ElementName = elementName;
            Serializer = serializer;
            PropertyInfo = propertyInfo;
        }

        public string MemberName { get; }
        public string ElementName { get; }
        public IBsonSerializer Serializer { get; }
        public PropertyInfo PropertyInfo { get; }
    }

    private readonly MemberBinding[] _members;

    /// <summary>
    /// Create a new instance of <see cref="ComplexTypeSerializer{TValue}"/>.
    /// </summary>
    /// <param name="complexType">The <see cref="IReadOnlyComplexType"/> this serializer relates to in EF Core.</param>
    /// <param name="bsonSerializerFactory">The <see cref="BsonSerializerFactory"/> to obtain member serializers from.</param>
    public ComplexTypeSerializer(IReadOnlyComplexType complexType, BsonSerializerFactory bsonSerializerFactory)
    {
        ArgumentNullException.ThrowIfNull(complexType);
        ArgumentNullException.ThrowIfNull(bsonSerializerFactory);

        _members = complexType.GetProperties()
            .Select(p => new MemberBinding(
                p.Name, p.GetElementName(), BsonSerializerFactory.CreateTypeSerializer(p), RequirePropertyInfo(complexType, p.Name)))
            .Cast<MemberBinding>()
            .Concat(complexType.GetComplexProperties()
                .Select(cp => new MemberBinding(
                    cp.Name, cp.GetElementName(), bsonSerializerFactory.GetComplexPropertySerializer(cp),
                    RequirePropertyInfo(complexType, cp.Name))))
            .ToArray();
    }

    private static PropertyInfo RequirePropertyInfo(IReadOnlyComplexType complexType, string memberName)
        => complexType.ClrType.GetProperty(memberName)
           ?? throw new InvalidOperationException(
               $"Complex type '{complexType.DisplayName()}' member '{memberName}' has no CLR property to serialize.");

    /// <inheritdoc />
    public Type ValueType => typeof(TValue);

    /// <inheritdoc />
    public TValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = (BsonDocument)new BsonDocumentSerializer().Deserialize(context, args);

        var boxed = Activator.CreateInstance(typeof(TValue))!;
        foreach (var member in _members)
        {
            if (!document.TryGetElement(member.ElementName, out var element) || element.Value.IsBsonNull)
            {
                continue;
            }

            member.PropertyInfo.SetValue(boxed, DeserializeMemberElement(member.Serializer, element));
        }

        return (TValue)boxed;
    }

    /// <inheritdoc />
    object? IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => Deserialize(context, args);

    /// <inheritdoc />
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TValue value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
            return;
        }

        context.Writer.WriteStartDocument();
        foreach (var member in _members)
        {
            var memberValue = member.PropertyInfo.GetValue(value);
            if (memberValue is null)
            {
                // The writer is only in Value state after WriteName; writing a null without one throws.
                context.Writer.WriteName(member.ElementName);
                context.Writer.WriteNull();
            }
            else
            {
                context.Writer.WriteName(member.ElementName);
                // Set the nominal type the way the driver's generic Serialize extension does: a collection
                // member serializer wraps values in a {_t, _v} document when the nominal type doesn't match.
                member.Serializer.Serialize(
                    context, new BsonSerializationArgs { NominalType = member.Serializer.ValueType }, memberValue);
            }
        }

        context.Writer.WriteEndDocument();
    }

    /// <inheritdoc />
    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        => Serialize(context, args, (TValue)value);

    /// <inheritdoc />
    public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo? memberSerializationInfo)
    {
        var member = _members.FirstOrDefault(m => m.MemberName == memberName);
        if (member == null)
        {
            memberSerializationInfo = null;
            return false;
        }

        memberSerializationInfo = new BsonSerializationInfo(member.ElementName, member.Serializer, member.Serializer.ValueType);
        return true;
    }

    private static object? DeserializeMemberElement(IBsonSerializer serializer, BsonElement element)
    {
        var reader = new BsonDocumentReader(new BsonDocument(element));
        var context = BsonDeserializationContext.CreateRoot(reader);
        reader.ReadStartDocument();
        reader.ReadName();
        var value = serializer.Deserialize(context, new BsonDeserializationArgs());
        reader.ReadEndDocument();
        return value;
    }
}
