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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Serializers;

namespace MongoDB.EntityFrameworkCore.UnitTests.Serializers;

public static class EntitySerializerComplexPropertyTests
{
    [Fact]
    public static void Complex_property_serializes_as_sub_document()
    {
        using var db = new CustomerContext();
        var complexProperty = GetAddressComplexProperty(db);
        var serializer = (IBsonSerializer<Address>)new BsonSerializerFactory()
            .GetComplexTypeSerializer(complexProperty.ComplexType);

        var address = new Address { City = "Seattle", Street = "1 Main" };
        var document = SerializeValue(serializer, address);

        Assert.Equal("Seattle", document["City"].AsString);
        Assert.Equal("1 Main", document["Street"].AsString);
    }

    [Fact]
    public static void Honors_Mongo_ElementName_annotation_on_complex_property()
    {
        using var db = new ShippingContext();
        var complexProperty = GetAddressComplexProperty(db);
        var serializer = (IBsonSerializer<Address>)new BsonSerializerFactory()
            .GetComplexTypeSerializer(complexProperty.ComplexType);

        var address = new Address { City = "Seattle", Street = "1 Main" };
        var document = SerializeValue(serializer, address);

        // The annotation names the complex property's own element, not its members'; members keep their names.
        Assert.Equal("Seattle", document["City"].AsString);
    }

    [Fact]
    public static void Entity_serializer_reports_complex_member_info()
    {
        using var db = new CustomerContext();
        var entityType = db.Model.FindEntityType(typeof(Customer));
        Assert.NotNull(entityType);

        var documentSerializer = (IBsonDocumentSerializer)new BsonSerializerFactory().GetEntitySerializer(entityType);

        Assert.True(documentSerializer.TryGetMemberSerializationInfo(nameof(Customer.Address), out var info));
        Assert.NotNull(info);
        Assert.Equal("Address", info.ElementName);
        Assert.Equal(typeof(Address), info.Serializer!.ValueType);
    }

    [Fact]
    public static void Entity_serializer_reports_annotated_complex_element_name()
    {
        using var db = new ShippingContext();
        var entityType = db.Model.FindEntityType(typeof(Customer));
        Assert.NotNull(entityType);

        var documentSerializer = (IBsonDocumentSerializer)new BsonSerializerFactory().GetEntitySerializer(entityType);

        Assert.True(documentSerializer.TryGetMemberSerializationInfo(nameof(Customer.Address), out var info));
        Assert.NotNull(info);
        Assert.Equal("shipping", info.ElementName);
    }

    [Fact]
    public static void Nested_complex_property_recurses_into_sub_documents()
    {
        using var db = new PersonContext();
        var person = new Person
        {
            Id = 1,
            Contact = new Contact { Address = new Address { City = "Portland", Street = "9 SE" } }
        };

        var contactProperty = db.Model.FindEntityType(typeof(Person))!.FindComplexProperty(nameof(Person.Contact))!;
        var serializer = (IBsonSerializer<Contact>)new BsonSerializerFactory()
            .GetComplexTypeSerializer(contactProperty.ComplexType);

        var document = SerializeValue(serializer, person.Contact);

        Assert.Equal("Portland", document["Address"]["City"].AsString);
        Assert.Equal("9 SE", document["Address"]["Street"].AsString);
    }

    [Fact]
    public static void Deserializes_sub_document_back_to_complex_value()
    {
        using var db = new CustomerContext();
        var complexProperty = GetAddressComplexProperty(db);
        var serializer = (IBsonSerializer<Address>)new BsonSerializerFactory()
            .GetComplexTypeSerializer(complexProperty.ComplexType);

        var stored = new BsonDocument { { "City", "Seattle" }, { "Street", "1 Main" } };
        var address = DeserializeValue(serializer, stored);

        Assert.Equal("Seattle", address.City);
        Assert.Equal("1 Main", address.Street);
    }

    [Fact]
    public static void Null_leaf_member_serializes_as_bson_null()
    {
        using var db = new CustomerContext();
        var complexProperty = GetAddressComplexProperty(db);
        var serializer = (IBsonSerializer<Address>)new BsonSerializerFactory()
            .GetComplexTypeSerializer(complexProperty.ComplexType);

        var address = new Address { City = "Seattle", Street = null! };
        var document = SerializeValue(serializer, address);

        Assert.True(document.Contains("Street"));
        Assert.Equal(BsonType.Null, document["Street"].BsonType);
        Assert.Equal("Seattle", document["City"].AsString);
    }

#if EF10
    [Fact]
    public static void Nullable_class_type_complex_property_returns_working_serializer()
    {
        using var db = new NullableAddressContext();
        var complexProperty = db.Model.FindEntityType(typeof(NullableCustomer))!
            .FindComplexProperty(nameof(NullableCustomer.BackupAddress))!;
        Assert.True(complexProperty.IsNullable);

        var serializer = new BsonSerializerFactory().GetComplexPropertySerializer(complexProperty);

        // A null value, written the way a parent-level writer will, serializes as a BSON null element —
        // not through a struct-constrained NullableSerializer.
        var document = new BsonDocument();
        using (var writer = new BsonDocumentWriter(document))
        {
            var context = BsonSerializationContext.CreateRoot(writer);
            writer.WriteStartDocument();
            writer.WriteName(complexProperty.GetElementName());
            serializer.Serialize(
                context, new BsonSerializationArgs { NominalType = serializer.ValueType }, null);
            writer.WriteEndDocument();
        }

        Assert.Equal(BsonType.Null, document["BackupAddress"].BsonType);

        var valueDocument = SerializeValue(serializer, new Address { City = "Seattle", Street = "1 Main" });
        Assert.Equal("Seattle", valueDocument["City"].AsString);
        Assert.Equal("1 Main", valueDocument["Street"].AsString);
    }

    private class NullableCustomer
    {
        public int Id { get; set; }
        public Address? BackupAddress { get; set; }
    }

    private class NullableAddressContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseMongoDB("mongodb://localhost:12345", "unitTests");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<NullableCustomer>().ComplexProperty(c => c.BackupAddress);
    }
#endif

    [Fact]
    public static void Missing_element_deserializes_to_default()
    {
        using var db = new CustomerContext();
        var complexProperty = GetAddressComplexProperty(db);
        var serializer = (IBsonSerializer<Address>)new BsonSerializerFactory()
            .GetComplexTypeSerializer(complexProperty.ComplexType);

        var stored = new BsonDocument();
        var address = DeserializeValue(serializer, stored);

        Assert.Null(address.City);
        Assert.Null(address.Street);
    }

#if EF10
    [Fact]
    public static void Nullable_struct_complex_property_serializes_through_the_metadata_serializer()
    {
        using var db = new NullableStructShippingContext();
        var complexProperty = db.Model.FindEntityType(typeof(StructCustomer))!
            .FindComplexProperty(nameof(StructCustomer.Address))!;
        Assert.True(complexProperty.IsNullable);

        var serializer = new BsonSerializerFactory().GetComplexPropertySerializer(complexProperty);

        // The value must serialize through the EF-metadata-driven ComplexTypeSerializer — element names
        // honored — not through a driver class map, which would rename Id to _id.
        var valueDocument = SerializeValue(serializer, new AddressPoint { Id = "A-1", City = "Seattle", Street = "1 Main" });
        Assert.Equal("A-1", valueDocument["Id"].AsString);
        Assert.False(valueDocument.Contains("_id"));
        Assert.Equal("Seattle", valueDocument["City"].AsString);
        Assert.Equal("1 Main", valueDocument["Street"].AsString);

        // And it round-trips through the nullable wrapper.
        using var reader = new BsonDocumentReader(valueDocument);
        var readContext = BsonDeserializationContext.CreateRoot(reader);
        var roundTripped = (AddressPoint?)serializer.Deserialize(readContext, new BsonDeserializationArgs());
        Assert.Equal("A-1", roundTripped?.Id);
        Assert.Equal("Seattle", roundTripped?.City);

        // A null value, written the way a parent-level writer will, serializes as a BSON null element.
        var nullDocument = new BsonDocument();
        using (var writer = new BsonDocumentWriter(nullDocument))
        {
            var context = BsonSerializationContext.CreateRoot(writer);
            writer.WriteStartDocument();
            writer.WriteName(complexProperty.GetElementName());
            serializer.Serialize(
                context, new BsonSerializationArgs { NominalType = serializer.ValueType }, null);
            writer.WriteEndDocument();
        }

        Assert.Equal(BsonType.Null, nullDocument["shipping"].BsonType);
    }

    [Fact]
    public static void Collection_complex_property_serializes_as_array()
    {
        using var db = new OrderContext();
        var linesProperty = db.Model.FindEntityType(typeof(Order))!.FindComplexProperty(nameof(Order.Lines))!;
        var serializer = new BsonSerializerFactory().GetComplexPropertySerializer(linesProperty);

        var lines = new List<Line> { new() { Sku = "X1", Quantity = 2 }, new() { Sku = "X2", Quantity = 5 } };

        // A collection serializer writes the array itself; the parent document names the element,
        // so drive it the way the parent-level writer will.
        var document = new BsonDocument();
        using (var writer = new BsonDocumentWriter(document))
        {
            var context = BsonSerializationContext.CreateRoot(writer);
            writer.WriteStartDocument();
            writer.WriteName(linesProperty.GetElementName());
            serializer.Serialize(
                context, new BsonSerializationArgs { NominalType = serializer.ValueType }, lines);
            writer.WriteEndDocument();
        }

        Assert.Equal(2, document["Lines"].AsBsonArray.Count);
        Assert.Equal("X1", document["Lines"][0]["Sku"].AsString);
        Assert.Equal(2, document["Lines"][0]["Quantity"].AsInt32);
        Assert.Equal("X2", document["Lines"][1]["Sku"].AsString);
    }
#endif

    private static IReadOnlyComplexProperty GetAddressComplexProperty(DbContext db)
        => db.Model.FindEntityType(typeof(Customer))!.FindComplexProperty(nameof(Customer.Address))
           ?? throw new InvalidOperationException("No Address complex property on the model.");

    private static BsonDocument SerializeValue(IBsonSerializer serializer, object value)
    {
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);
        var context = BsonSerializationContext.CreateRoot(writer);
        serializer.Serialize(context, new BsonSerializationArgs(), value);
        return document;
    }

    private static T DeserializeValue<T>(IBsonSerializer<T> serializer, BsonDocument document)
    {
        using var reader = new BsonDocumentReader(document);
        var context = BsonDeserializationContext.CreateRoot(reader);
        return serializer.Deserialize(context, new BsonDeserializationArgs());
    }

    private class Customer
    {
        public int Id { get; set; }
        public Address Address { get; set; } = null!;
    }

    private class Address
    {
        public string? City { get; set; }
        public string? Street { get; set; }
    }

    private class Person
    {
        public int Id { get; set; }
        public Contact Contact { get; set; } = null!;
    }

    private class Contact
    {
        public Address Address { get; set; } = null!;
    }

#if EF10
    private class Order
    {
        public int Id { get; set; }
        public List<Line> Lines { get; set; } = null!;
    }

    private class Line
    {
        public string Sku { get; set; } = null!;
        public int Quantity { get; set; }
    }

    private class StructCustomer
    {
        public int Id { get; set; }
        public AddressPoint? Address { get; set; }
    }

    private struct AddressPoint
    {
        public string Id { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
    }

    private class NullableStructShippingContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseMongoDB("mongodb://localhost:12345", "unitTests");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StructCustomer>().ComplexProperty(c => c.Address);

            // ComplexPropertyBuilder exposes no HasAnnotation in any EF version we target, so set the
            // annotation on the mutable complex property directly (it survives FinalizeModel).
            var complexProperty = modelBuilder.Model
                .FindEntityType(typeof(StructCustomer))!
                .FindComplexProperty(nameof(StructCustomer.Address))!;
            complexProperty.SetAnnotation(MongoAnnotationNames.ElementName, "shipping");
        }
    }
#endif

    private class CustomerContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseMongoDB("mongodb://localhost:12345", "unitTests");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Customer>().ComplexProperty(c => c.Address);
    }

    private class ShippingContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseMongoDB("mongodb://localhost:12345", "unitTests");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>().ComplexProperty(c => c.Address);

            // ComplexPropertyBuilder exposes no HasAnnotation in any EF version we target, so set the
            // annotation on the mutable complex property directly (it survives FinalizeModel).
            var complexProperty = modelBuilder.Model
                .FindEntityType(typeof(Customer))!
                .FindComplexProperty(nameof(Customer.Address))!;
            complexProperty.SetAnnotation(MongoAnnotationNames.ElementName, "shipping");
        }
    }

    private class PersonContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseMongoDB("mongodb://localhost:12345", "unitTests");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Person>().ComplexProperty(
                p => p.Contact,
                contact => contact.ComplexProperty(c => c.Address));
    }

#if EF10
    private class OrderContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseMongoDB("mongodb://localhost:12345", "unitTests");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Order>().ComplexCollection(o => o.Lines);
    }
#endif
}
