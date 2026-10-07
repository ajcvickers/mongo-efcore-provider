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
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Metadata.Conventions;
using MongoDB.EntityFrameworkCore.Serializers;

namespace MongoDB.EntityFrameworkCore.UnitTests.Serializers;

/// <summary>
/// Member lookup through the serializers the factory builds for complex properties: what driver-LINQ (the bulk
/// bridge) sees when it asks an entity serializer, and then a complex-type serializer, where a member is stored.
/// </summary>
public static class ComplexTypeSerializerTests
{
    [Fact]
    public static void Entity_serializer_resolves_complex_property_to_its_element_and_complex_serializer()
    {
        using var db = new TestContext<Holder>(mb => mb.Entity<Holder>().ComplexProperty(e => e.Address));

        var info = Lookup(EntitySerializerFor<Holder>(db, out _), nameof(Holder.Address));

        Assert.Equal("Address", info.ElementName);
        Assert.IsType<ComplexTypeSerializer<Addr>>(info.Serializer);
        Assert.Equal(typeof(Addr), info.NominalType);
    }

    [Fact]
    public static void Complex_serializer_resolves_leaf_property()
    {
        using var db = new TestContext<LeafHolder>(mb => mb.Entity<LeafHolder>().ComplexProperty(e => e.Address));

        var complex = Lookup(EntitySerializerFor<LeafHolder>(db, out _), nameof(LeafHolder.Address));
        var leaf = Lookup(complex.Serializer, nameof(Addr.Street));

        Assert.Equal("Street", leaf.ElementName);
        Assert.IsType<StringSerializer>(leaf.Serializer);
        Assert.Equal(typeof(string), leaf.NominalType);
    }

    [Fact]
    public static void Struct_complex_property_resolves_with_struct_nominal_type_and_leaf()
    {
        using var db = new TestContext<StructHolder>(mb => mb.Entity<StructHolder>().ComplexProperty(e => e.Point));

        var complex = Lookup(EntitySerializerFor<StructHolder>(db, out _), nameof(StructHolder.Point));
        Assert.IsType<ComplexTypeSerializer<Pt>>(complex.Serializer);
        Assert.Equal(typeof(Pt), complex.NominalType);
        Assert.Equal(typeof(Pt), complex.Serializer.ValueType);

        var leaf = Lookup(complex.Serializer, nameof(Pt.Lat));
        Assert.Equal("Lat", leaf.ElementName);
        Assert.Equal(typeof(double), leaf.NominalType);
    }

    [Fact]
    public static void Element_name_overrides_apply_to_complex_property_and_leaf()
    {
        using var db = new TestContext<RenamedHolder>(mb =>
            mb.Entity<RenamedHolder>().ComplexProperty(e => e.Address, a =>
            {
                a.Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "addr");
                a.Property(x => x.Street).Metadata.SetElementName("st");
            }));

        var complex = Lookup(EntitySerializerFor<RenamedHolder>(db, out _), nameof(RenamedHolder.Address));
        Assert.Equal("addr", complex.ElementName);
        Assert.Equal("st", Lookup(complex.Serializer, nameof(Addr.Street)).ElementName);
    }

    [Fact]
    public static void Converter_leaf_uses_value_converter_serializer()
    {
        using var db = new TestContext<ConvertedHolder>(mb =>
            mb.Entity<ConvertedHolder>().ComplexProperty(e => e.Counter, c => c.Property(x => x.Count).HasConversion<string>()));

        var complex = Lookup(EntitySerializerFor<ConvertedHolder>(db, out _), nameof(ConvertedHolder.Counter));
        var leaf = Lookup(complex.Serializer, nameof(Counter.Count));

        Assert.IsType<ValueConverterSerializer<int, string>>(leaf.Serializer);
    }

    [Fact]
    public static void BsonRepresentation_leaf_uses_configured_representation()
    {
        using var db = new TestContext<RepresentedHolder>(mb =>
            mb.Entity<RepresentedHolder>().ComplexProperty(e => e.Counter,
                c => c.Property(x => x.Count).Metadata.SetBsonRepresentation(BsonType.String, null, null)));

        var complex = Lookup(EntitySerializerFor<RepresentedHolder>(db, out _), nameof(RepresentedHolder.Counter));
        var leaf = Lookup(complex.Serializer, nameof(Counter.Count));

        var int32Serializer = Assert.IsType<Int32Serializer>(leaf.Serializer);
        Assert.Equal(BsonType.String, int32Serializer.Representation);
    }

    [Fact]
    public static void Nested_complex_chain_resolves_hop_by_hop()
    {
        using var db = new TestContext<NestedHolder>(mb =>
            mb.Entity<NestedHolder>().ComplexProperty(e => e.Address, a =>
                a.ComplexProperty(x => x.Location, l => l.Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "loc"))));

        var outer = Lookup(EntitySerializerFor<NestedHolder>(db, out _), nameof(NestedHolder.Address));
        Assert.IsType<ComplexTypeSerializer<NestedAddr>>(outer.Serializer);

        var inner = Lookup(outer.Serializer, nameof(NestedAddr.Location));
        Assert.Equal("loc", inner.ElementName);
        Assert.IsType<ComplexTypeSerializer<Pt>>(inner.Serializer);
        Assert.Equal(typeof(Pt), inner.NominalType);

        var leaf = Lookup(inner.Serializer, nameof(Pt.Lon));
        Assert.Equal("Lon", leaf.ElementName);
    }

    [Fact]
    public static void Camel_case_convention_names_complex_property_and_leaf()
    {
        using var db = new TestContext<CamelHolder>(mb =>
            {
                mb.Entity<CamelHolder>().Property(e => e.Id).Metadata.SetElementName("_id");
                mb.Entity<CamelHolder>().ComplexProperty(e => e.HomeAddress);
            },
            camelCase: true);

        var complex = Lookup(EntitySerializerFor<CamelHolder>(db, out _), nameof(CamelHolder.HomeAddress));
        Assert.Equal("homeAddress", complex.ElementName);
        Assert.Equal("streetName", Lookup(complex.Serializer, nameof(CamelAddr.StreetName)).ElementName);
    }

    [Fact]
    public static void Unknown_member_is_not_resolved()
    {
        using var db = new TestContext<UnknownHolder>(mb => mb.Entity<UnknownHolder>().ComplexProperty(e => e.Address));

        var entitySerializer = (IBsonDocumentSerializer)EntitySerializerFor<UnknownHolder>(db, out _);
        Assert.False(entitySerializer.TryGetMemberSerializationInfo("Nope", out _));

        var complex = Lookup(entitySerializer, nameof(UnknownHolder.Address));
        Assert.False(((IBsonDocumentSerializer)complex.Serializer).TryGetMemberSerializationInfo("Nope", out var info));
        Assert.Null(info);
    }

    [Fact]
    public static void Same_clr_type_at_two_properties_resolves_independently()
    {
        using var db = new TestContext<TwoAddressHolder>(mb =>
        {
            var entity = mb.Entity<TwoAddressHolder>();
            entity.ComplexProperty(e => e.Home);
            entity.ComplexProperty(e => e.Work, a =>
            {
                a.Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "office");
                a.Property(x => x.Street).Metadata.SetElementName("officeStreet");
            });
        });

        var entitySerializer = EntitySerializerFor<TwoAddressHolder>(db, out _);
        var home = Lookup(entitySerializer, nameof(TwoAddressHolder.Home));
        var work = Lookup(entitySerializer, nameof(TwoAddressHolder.Work));

        Assert.Equal("Home", home.ElementName);
        Assert.Equal("office", work.ElementName);
        Assert.NotSame(home.Serializer, work.Serializer);
        Assert.Equal("Street", Lookup(home.Serializer, nameof(Addr.Street)).ElementName);
        Assert.Equal("officeStreet", Lookup(work.Serializer, nameof(Addr.Street)).ElementName);
    }

    [Fact]
    public static void Complex_property_on_owned_entity_resolves_through_owned_entity_serializer()
    {
        using var db = new TestContext<OwnerHolder>(mb => mb.Entity<OwnerHolder>().OwnsOne(e => e.Owned));

        var factory = new BsonSerializerFactory();
        var ownerSerializer = factory.GetEntitySerializer(db.Model.FindEntityType(typeof(OwnerHolder))!);
        var owned = Lookup(ownerSerializer, nameof(OwnerHolder.Owned));
        var extra = Lookup(owned.Serializer, nameof(OwnedWithComplex.Extra));

        Assert.Equal("Extra", extra.ElementName);
        Assert.IsType<ComplexTypeSerializer<OwnedExtra>>(extra.Serializer);
        Assert.Equal("Value", Lookup(extra.Serializer, nameof(OwnedExtra.Value)).ElementName);
    }

#if !EF8 && !EF9
    [Fact]
    public static void Complex_collection_is_array_serializer_over_complex_serializer()
    {
        using var db = new TestContext<CollectionHolder>(mb =>
            mb.Entity<CollectionHolder>().ComplexCollection(e => e.Addresses,
                a => a.Property(x => x.Street).Metadata.SetElementName("st")));

        var collection = Lookup(EntitySerializerFor<CollectionHolder>(db, out _), nameof(CollectionHolder.Addresses));
        Assert.Equal("Addresses", collection.ElementName);
        Assert.Equal(typeof(List<Addr>), collection.NominalType);
        Assert.Equal(typeof(List<Addr>), collection.Serializer.ValueType);

        var arraySerializer = Assert.IsAssignableFrom<IBsonArraySerializer>(collection.Serializer);
        Assert.True(arraySerializer.TryGetItemSerializationInfo(out var itemInfo));
        Assert.IsType<ComplexTypeSerializer<Addr>>(itemInfo.Serializer);
        Assert.Equal("st", Lookup(itemInfo.Serializer, nameof(Addr.Street)).ElementName);
    }

    [Fact]
    public static void Complex_array_collection_is_array_serializer_over_complex_serializer()
    {
        using var db = new TestContext<ArrayCollectionHolder>(mb =>
            mb.Entity<ArrayCollectionHolder>().ComplexCollection(e => e.Addresses));

        var collection = Lookup(EntitySerializerFor<ArrayCollectionHolder>(db, out _), nameof(ArrayCollectionHolder.Addresses));
        Assert.Equal(typeof(Addr[]), collection.Serializer.ValueType);

        var arraySerializer = Assert.IsAssignableFrom<IBsonArraySerializer>(collection.Serializer);
        Assert.True(arraySerializer.TryGetItemSerializationInfo(out var itemInfo));
        Assert.IsType<ComplexTypeSerializer<Addr>>(itemInfo.Serializer);
    }

    [Fact]
    public static void Factory_caches_complex_collection_serializer()
    {
        using var db = new TestContext<CachedCollectionHolder>(mb =>
            mb.Entity<CachedCollectionHolder>().ComplexCollection(e => e.Addresses));

        var entityType = db.Model.FindEntityType(typeof(CachedCollectionHolder))!;
        var property = entityType.FindComplexProperty(nameof(CachedCollectionHolder.Addresses))!;
        var factory = new BsonSerializerFactory();

        var first = factory.GetComplexPropertySerializer(property);
        Assert.Same(first, factory.GetComplexPropertySerializer(property));
        Assert.Same(first, Lookup(factory.GetEntitySerializer(entityType), nameof(CachedCollectionHolder.Addresses)).Serializer);
    }

    [Fact]
    public static void Optional_complex_property_resolves_like_required()
    {
        using var db = new TestContext<OptionalHolder>(mb =>
            mb.Entity<OptionalHolder>().ComplexProperty(e => e.Address).IsRequired(false));

        var complex = Lookup(EntitySerializerFor<OptionalHolder>(db, out _), nameof(OptionalHolder.Address));
        Assert.IsType<ComplexTypeSerializer<Addr>>(complex.Serializer);
        Assert.Equal("Street", Lookup(complex.Serializer, nameof(Addr.Street)).ElementName);
    }

    [Fact]
    public static void Optional_struct_complex_property_is_nullable_serializer_over_complex_serializer()
    {
        using var db = new TestContext<OptionalStructHolder>(mb =>
            mb.Entity<OptionalStructHolder>().ComplexProperty(e => e.Point).IsRequired(false));

        var complex = Lookup(EntitySerializerFor<OptionalStructHolder>(db, out _), nameof(OptionalStructHolder.Point));
        Assert.Equal(typeof(Pt?), complex.NominalType);
        var nullable = Assert.IsAssignableFrom<IChildSerializerConfigurable>(complex.Serializer);
        Assert.IsType<ComplexTypeSerializer<Pt>>(nullable.ChildSerializer);
        Assert.Equal("Lat", Lookup(nullable.ChildSerializer, nameof(Pt.Lat)).ElementName);
    }
#endif

    [Fact]
    public static void Serialize_throws_not_supported_for_whole_value_comparison()
    {
        using var db = new TestContext<SerializeHolder>(mb => mb.Entity<SerializeHolder>().ComplexProperty(e => e.Address));

        var complex = Lookup(EntitySerializerFor<SerializeHolder>(db, out _), nameof(SerializeHolder.Address));

        using var writer = new BsonDocumentWriter(new BsonDocument());
        writer.WriteStartDocument();
        writer.WriteName("x");
        var context = BsonSerializationContext.CreateRoot(writer);
        var exception = Assert.Throws<NotSupportedException>(() =>
            complex.Serializer.Serialize(context, new BsonSerializationArgs(), new Addr { Street = "s" }));
        Assert.Contains("Addr", exception.Message);
    }

    [Fact]
    public static void Deserialize_is_not_implemented()
    {
        using var db = new TestContext<DeserializeHolder>(mb => mb.Entity<DeserializeHolder>().ComplexProperty(e => e.Address));

        var complex = Lookup(EntitySerializerFor<DeserializeHolder>(db, out _), nameof(DeserializeHolder.Address));

        using var reader = new BsonDocumentReader(new BsonDocument("Street", "s"));
        var context = BsonDeserializationContext.CreateRoot(reader);
        Assert.Throws<NotImplementedException>(() => complex.Serializer.Deserialize(context, new BsonDeserializationArgs()));
    }

    [Fact]
    public static void Factory_caches_complex_serializer_per_complex_type()
    {
        using var db = new TestContext<CacheHolder>(mb =>
        {
            mb.Entity<CacheHolder>().ComplexProperty(e => e.Home);
            mb.Entity<CacheHolder>().ComplexProperty(e => e.Work);
        });

        var entityType = db.Model.FindEntityType(typeof(CacheHolder))!;
        var home = entityType.FindComplexProperty(nameof(CacheHolder.Home))!;
        var work = entityType.FindComplexProperty(nameof(CacheHolder.Work))!;
        var factory = new BsonSerializerFactory();

        var first = factory.GetComplexPropertySerializer(home);
        Assert.Same(first, factory.GetComplexPropertySerializer(home));
        Assert.Same(first, Lookup(factory.GetEntitySerializer(entityType), nameof(CacheHolder.Home)).Serializer);
        Assert.NotSame(first, factory.GetComplexPropertySerializer(work));
    }

    private static IBsonSerializer EntitySerializerFor<TEntity>(DbContext db, out IEntityType entityType)
    {
        entityType = db.Model.FindEntityType(typeof(TEntity))!;
        return new BsonSerializerFactory().GetEntitySerializer(entityType);
    }

    private static BsonSerializationInfo Lookup(IBsonSerializer serializer, string memberName)
    {
        var documentSerializer = Assert.IsAssignableFrom<IBsonDocumentSerializer>(serializer);
        Assert.True(documentSerializer.TryGetMemberSerializationInfo(memberName, out var info), $"'{memberName}' not resolved.");
        return info!;
    }

    public class Addr
    {
        public string Street { get; set; } = null!;
    }

    public struct Pt
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public class NestedAddr
    {
        public string Street { get; set; } = null!;
        public Pt Location { get; set; }
    }

    public class Counter
    {
        public int Count { get; set; }
    }

    public class CamelAddr
    {
        public string StreetName { get; set; } = null!;
    }

    class Holder { public int Id { get; set; } public Addr Address { get; set; } = null!; }
    class LeafHolder { public int Id { get; set; } public Addr Address { get; set; } = null!; }
    class StructHolder { public int Id { get; set; } public Pt Point { get; set; } }
    class RenamedHolder { public int Id { get; set; } public Addr Address { get; set; } = null!; }
    class ConvertedHolder { public int Id { get; set; } public Counter Counter { get; set; } = null!; }
    class RepresentedHolder { public int Id { get; set; } public Counter Counter { get; set; } = null!; }
    class NestedHolder { public int Id { get; set; } public NestedAddr Address { get; set; } = null!; }
    class CamelHolder { public int Id { get; set; } public CamelAddr HomeAddress { get; set; } = null!; }
    class UnknownHolder { public int Id { get; set; } public Addr Address { get; set; } = null!; }
    class TwoAddressHolder { public int Id { get; set; } public Addr Home { get; set; } = null!; public Addr Work { get; set; } = null!; }
    class SerializeHolder { public int Id { get; set; } public Addr Address { get; set; } = null!; }
    class DeserializeHolder { public int Id { get; set; } public Addr Address { get; set; } = null!; }
    class CacheHolder { public int Id { get; set; } public Addr Home { get; set; } = null!; public Addr Work { get; set; } = null!; }

    // OwnedNavigationBuilder has no ComplexProperty overload; [ComplexType] puts a complex property on an owned type.
    [ComplexType]
    public class OwnedExtra
    {
        public string Value { get; set; } = null!;
    }

    public class OwnedWithComplex
    {
        public string Note { get; set; } = null!;
        public OwnedExtra Extra { get; set; } = null!;
    }

    class OwnerHolder { public int Id { get; set; } public OwnedWithComplex Owned { get; set; } = null!; }

#if !EF8 && !EF9
    class CollectionHolder { public int Id { get; set; } public List<Addr> Addresses { get; set; } = null!; }
    class ArrayCollectionHolder { public int Id { get; set; } public Addr[] Addresses { get; set; } = null!; }
    class CachedCollectionHolder { public int Id { get; set; } public List<Addr> Addresses { get; set; } = null!; }
    class OptionalHolder { public int Id { get; set; } public Addr? Address { get; set; } }
    class OptionalStructHolder { public int Id { get; set; } public Pt? Point { get; set; } }
#endif

    // Generic so each test gets its own context type (EF caches the model per context type).
    class TestContext<TEntity>(Action<ModelBuilder> configure, bool camelCase = false) : DbContext where TEntity : class
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            if (camelCase)
            {
                configurationBuilder.Conventions.Add(_ => new CamelCaseElementNameConvention());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => configure(modelBuilder);
    }
}
