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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Metadata.Conventions;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// The stored (raw BSON) shape of complex properties written by <c>SaveChanges</c> on insert.
/// </summary>
[XUnitCollection("UpdateTests")]
public class ComplexTypeWriteTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private static void ConfigureCustomer(ModelBuilder mb)
        => mb.Entity<CustomerWithAddress>().ComplexProperty(c => c.Address, a => a.ComplexProperty(x => x.Location));

    private static CustomerWithAddress NewCustomer()
        => new()
        {
            Name = "Alice",
            Address = new ComplexAddress
            {
                Street = "1 Main St", City = "Springfield", Location = new GeoPoint { Lat = 12.5, Lon = -3.25 }
            }
        };

    private BsonDocument ReadSingleRaw<T>(IMongoCollection<T> collection)
        => database.GetCollection<BsonDocument>(collection.CollectionNamespace)
            .Find(FilterDefinition<BsonDocument>.Empty).Single();

    [Fact]
    public void Insert_writes_complex_property_as_subdocument_with_nested_struct()
    {
        var collection = database.CreateCollection<CustomerWithAddress>();
        var customer = NewCustomer();

        using (var db = SingleEntityDbContext.Create(collection, ConfigureCustomer))
        {
            db.Entities.Add(customer);
            db.SaveChanges();
        }

        var raw = ReadSingleRaw(collection);

        Assert.Equal(customer.Id, raw["_id"].AsObjectId);
        Assert.Equal("Alice", raw["Name"].AsString);
        Assert.Equal(new[] { "_id", "Name", "Address" }, raw.Names.ToArray());

        var address = raw["Address"].AsBsonDocument;
        Assert.Equal(new[] { "City", "Location", "Street" }, address.Names.OrderBy(n => n, StringComparer.Ordinal).ToArray());
        Assert.False(address.Contains("_id"));
        Assert.Equal("1 Main St", address["Street"].AsString);
        Assert.Equal("Springfield", address["City"].AsString);

        var location = address["Location"].AsBsonDocument;
        Assert.Equal(new[] { "Lat", "Lon" }, location.Names.OrderBy(n => n, StringComparer.Ordinal).ToArray());
        Assert.Equal(new BsonDouble(12.5), location["Lat"]);
        Assert.Equal(new BsonDouble(-3.25), location["Lon"]);
    }

    [Fact]
    public void Update_of_scalar_on_entity_with_complex_property_keeps_subdocument()
    {
        // Every Modified entry passes through the complex writer's change filter; a scalar-only change must skip the
        // complex property (not rewrite it), which only the sent $set can show: the stored values are equal either way.
        // (Leaf-change semantics are covered in ComplexTypeTrackingTests.)
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddress>());
        var customer = NewCustomer();

        using (var db = SingleEntityDbContext.Create(collection, ConfigureCustomer))
        {
            db.Entities.Add(customer);
            db.SaveChanges();
            capture.Clear();
            customer.Name = "Alicia";
            db.SaveChanges();
        }

        var set = capture.SingleSet();
        Assert.False(set.Contains("Address"), set.ToJson());
        Assert.Equal(new[] { "_id", "Name" }, set.Names.ToArray());

        var raw = ReadSingleRaw(collection);
        Assert.Equal("Alicia", raw["Name"].AsString);
        Assert.Equal("Springfield", raw["Address"]["City"].AsString);
        Assert.Equal(new BsonDouble(12.5), raw["Address"]["Location"]["Lat"]);
    }

    [Fact]
    public void Insert_with_default_struct_writes_zeroed_subdocument()
    {
        var collection = database.CreateCollection<CustomerWithAddress>();

        using (var db = SingleEntityDbContext.Create(collection, ConfigureCustomer))
        {
            db.Entities.Add(new CustomerWithAddress
            {
                Name = "Bob", Address = new ComplexAddress { Street = "S", City = "C" }
            });
            db.SaveChanges();
        }

        var location = ReadSingleRaw(collection)["Address"]["Location"].AsBsonDocument;
        Assert.Equal(new BsonDouble(0), location["Lat"]);
        Assert.Equal(new BsonDouble(0), location["Lon"]);
    }

    [Fact]
    public void HasElementName_on_complex_property_and_leaf_changes_stored_names()
    {
        var collection = database.CreateCollection<CustomerWithAddress>();

        using (var db = SingleEntityDbContext.Create(collection, mb =>
               {
                   mb.Entity<CustomerWithAddress>().ComplexProperty(c => c.Address, a =>
                   {
                       a.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "addr");
                       a.Property(x => x.City).Metadata.SetElementName("town");
                       a.ComplexProperty(x => x.Location).HasPropertyAnnotation(MongoAnnotationNames.ElementName, "geo");
                   });
               }))
        {
            db.Entities.Add(NewCustomer());
            db.SaveChanges();
        }

        var raw = ReadSingleRaw(collection);
        Assert.False(raw.Contains("Address"));
        var address = raw["addr"].AsBsonDocument;
        Assert.False(address.Contains("City"));
        Assert.Equal("Springfield", address["town"].AsString);
        Assert.False(address.Contains("Location"));
        Assert.Equal(new BsonDouble(12.5), address["geo"]["Lat"]);
    }

    [Fact]
    public void CamelCase_convention_applies_to_complex_property_and_leaves()
    {
        var collection = database.CreateCollection<CustomerWithAddress>();

        using (var db = SingleEntityDbContext.Create(collection, ConfigureCustomer,
                   cb => cb.Conventions.Add(_ => new CamelCaseElementNameConvention())))
        {
            db.Entities.Add(NewCustomer());
            db.SaveChanges();
        }

        var raw = ReadSingleRaw(collection);
        var address = raw["address"].AsBsonDocument;
        Assert.Equal("1 Main St", address["street"].AsString);
        Assert.Equal("Springfield", address["city"].AsString);
        Assert.Equal(new BsonDouble(-3.25), address["location"]["lon"]);
    }

    public enum Tier
    {
        Bronze,
        Gold
    }

    public class Details
    {
        public ObjectId Ref { get; set; }
        public Guid Token { get; set; }
        public DateTime When { get; set; }
        public Tier Tier { get; set; }
        public int Code { get; set; }
        public decimal Amount { get; set; }
    }

    public class EntityWithDetails
    {
        public ObjectId Id { get; set; }
        public Details Details { get; set; } = null!;
    }

    [Fact]
    public void Complex_leaves_use_provider_serializers_converters_and_representations()
    {
        var collection = database.CreateCollection<EntityWithDetails>();
        var objectId = ObjectId.GenerateNewId();
        var guid = Guid.NewGuid();
        var when = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);

        using (var db = SingleEntityDbContext.Create(collection, mb =>
               {
                   mb.Entity<EntityWithDetails>().ComplexProperty(e => e.Details, d =>
                   {
                       d.Property(x => x.Tier).HasConversion<string>();
                       d.Property(x => x.Code).Metadata.SetBsonRepresentation(BsonType.String, null, null);
                   });
               }))
        {
            db.Entities.Add(new EntityWithDetails
            {
                Details = new Details
                {
                    Ref = objectId, Token = guid, When = when, Tier = Tier.Gold, Code = 42, Amount = 1.5m
                }
            });
            db.SaveChanges();
        }

        var details = ReadSingleRaw(collection)["Details"].AsBsonDocument;
        Assert.Equal(new BsonObjectId(objectId), details["Ref"]);
        Assert.Equal(new BsonBinaryData(guid, GuidRepresentation.Standard), details["Token"]);
        Assert.Equal(new BsonDateTime(when), details["When"]);
        Assert.Equal(new BsonString("Gold"), details["Tier"]);
        Assert.Equal(new BsonString("42"), details["Code"]);
        Assert.Equal(new BsonDecimal128(1.5m), details["Amount"]);
    }

    public class Inner
    {
        public string Value { get; set; } = null!;
    }

    public class Middle
    {
        public int Number { get; set; }
        public Inner Inner { get; set; } = null!;
    }

    public class Outer
    {
        public string Label { get; set; } = null!;
        public Middle Middle { get; set; } = null!;
    }

    public class EntityWithDeepNesting
    {
        public ObjectId Id { get; set; }
        public Outer Outer { get; set; } = null!;
        public Inner Sibling { get; set; } = null!;
    }

    [Fact]
    public void Deeply_nested_and_sibling_complex_properties_write_independently()
    {
        var collection = database.CreateCollection<EntityWithDeepNesting>();

        using (var db = SingleEntityDbContext.Create(collection, mb =>
               {
                   var entity = mb.Entity<EntityWithDeepNesting>();
                   entity.ComplexProperty(e => e.Outer, o => o.ComplexProperty(x => x.Middle, m => m.ComplexProperty(x => x.Inner)));
                   entity.ComplexProperty(e => e.Sibling);
               }))
        {
            db.Entities.Add(new EntityWithDeepNesting
            {
                Outer = new Outer { Label = "L", Middle = new Middle { Number = 7, Inner = new Inner { Value = "deep" } } },
                Sibling = new Inner { Value = "side" }
            });
            db.SaveChanges();
        }

        var raw = ReadSingleRaw(collection);
        Assert.Equal("L", raw["Outer"]["Label"].AsString);
        Assert.Equal(7, raw["Outer"]["Middle"]["Number"].AsInt32);
        Assert.Equal("deep", raw["Outer"]["Middle"]["Inner"]["Value"].AsString);
        Assert.Equal(new BsonDocument("Value", "side"), raw["Sibling"].AsBsonDocument);
    }

    public class Tag
    {
        public string Text { get; set; } = null!;
    }

    public class OrderWithOwnedAndComplex
    {
        public ObjectId Id { get; set; }
        public ComplexAddress Billing { get; set; } = null!;
        public Tag Owned { get; set; } = null!;
    }

    [Fact]
    public void Complex_property_coexists_with_owned_navigation()
    {
        var collection = database.CreateCollection<OrderWithOwnedAndComplex>();

        using (var db = SingleEntityDbContext.Create(collection, mb =>
               {
                   var entity = mb.Entity<OrderWithOwnedAndComplex>();
                   entity.ComplexProperty(e => e.Billing, a => a.ComplexProperty(x => x.Location));
                   entity.OwnsOne(e => e.Owned);
               }))
        {
            db.Entities.Add(new OrderWithOwnedAndComplex
            {
                Billing = new ComplexAddress { Street = "B", City = "C", Location = new GeoPoint { Lat = 1, Lon = 2 } },
                Owned = new Tag { Text = "t" }
            });
            db.SaveChanges();
        }

        var raw = ReadSingleRaw(collection);
        Assert.Equal("B", raw["Billing"]["Street"].AsString);
        Assert.Equal(new BsonDouble(2), raw["Billing"]["Location"]["Lon"]);
        Assert.Equal(new BsonDocument("Text", "t"), raw["Owned"].AsBsonDocument);
    }

    // OwnedNavigationBuilder has no ComplexProperty overload; [ComplexType] is how a complex property gets onto an
    // owned type.
    [System.ComponentModel.DataAnnotations.Schema.ComplexType]
    public class OwnedExtra
    {
        public string Value { get; set; } = null!;
    }

    public class OwnedWithComplex
    {
        public string Note { get; set; } = null!;
        public OwnedExtra Extra { get; set; } = null!;
    }

    public class EntityWithOwnedHoldingComplex
    {
        public ObjectId Id { get; set; }
        public OwnedWithComplex Owned { get; set; } = null!;
    }

    [Fact]
    public void Complex_property_on_owned_entity_is_written_inside_owned_subdocument()
    {
        var collection = database.CreateCollection<EntityWithOwnedHoldingComplex>();

        using (var db = SingleEntityDbContext.Create(collection, mb =>
               {
                   mb.Entity<EntityWithOwnedHoldingComplex>().OwnsOne(e => e.Owned);
               }))
        {
            db.Entities.Add(new EntityWithOwnedHoldingComplex
            {
                Owned = new OwnedWithComplex { Note = "n", Extra = new OwnedExtra { Value = "x" } }
            });
            db.SaveChanges();
        }

        var owned = ReadSingleRaw(collection)["Owned"].AsBsonDocument;
        Assert.Equal("n", owned["Note"].AsString);
        Assert.Equal(new BsonDocument("Value", "x"), owned["Extra"].AsBsonDocument);
    }

#if !EF8 && !EF9
    [Fact]
    public void Insert_writes_complex_collection_as_array_of_subdocuments_in_order()
    {
        var collection = database.CreateCollection<CustomerWithAddressList>();

        using (var db = SingleEntityDbContext.Create(collection,
                   mb => mb.Entity<CustomerWithAddressList>().ComplexCollection(c => c.Addresses, a => a.ComplexProperty(x => x.Location))))
        {
            db.Entities.Add(new CustomerWithAddressList
            {
                Name = "Carol",
                Addresses =
                [
                    new ComplexAddress { Street = "A", City = "X", Location = new GeoPoint { Lat = 1, Lon = 2 } },
                    new ComplexAddress { Street = "B", City = "Y" }
                ]
            });
            db.SaveChanges();
        }

        var addresses = ReadSingleRaw(collection)["Addresses"].AsBsonArray;
        Assert.Equal(2, addresses.Count);
        Assert.Equal("A", addresses[0]["Street"].AsString);
        Assert.Equal(new BsonDouble(2), addresses[0]["Location"]["Lon"]);
        Assert.False(addresses[0].AsBsonDocument.Contains("_id"));
        Assert.Equal("B", addresses[1]["Street"].AsString);
        Assert.Equal(new BsonDouble(0), addresses[1]["Location"]["Lat"]);
    }

    [Fact]
    public void Insert_writes_empty_complex_collection_as_empty_array()
    {
        var collection = database.CreateCollection<CustomerWithAddressList>();

        using (var db = SingleEntityDbContext.Create(collection,
                   mb => mb.Entity<CustomerWithAddressList>().ComplexCollection(c => c.Addresses, a => a.ComplexProperty(x => x.Location))))
        {
            db.Entities.Add(new CustomerWithAddressList { Name = "Dan" });
            db.SaveChanges();
        }

        Assert.Equal(new BsonArray(), ReadSingleRaw(collection)["Addresses"]);
    }
#endif
}
