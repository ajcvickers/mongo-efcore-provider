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
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// Complex-property persistence: a complex property is stored as a BSON sub-document at its configured
/// element name. The materialization half of the round trip is Task 4's acceptance test.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeSerializationTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    public class Address
    {
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
    }

    public class Customer
    {
        public int Id { get; set; }
        public Address Address { get; set; } = null!;
    }

    [Fact]
    public void Insert_writes_complex_property_sub_document()
    {
        var collection = database.CreateCollection<Customer>();

        {
            using var db = SingleEntityDbContext.Create(
                collection,
                modelBuilder => modelBuilder.Entity<Customer>().ComplexProperty(c => c.Address));

            db.Entities.Add(new Customer { Id = 1, Address = new Address { City = "Seattle", Street = "1 Main" } });
            db.SaveChanges();
        }

        {
            var actual = collection
                .Find(Builders<Customer>.Filter.Empty)
                .Project(Builders<Customer>.Projection.As<BsonDocument>())
                .Single();

            var expected = BsonDocument.Parse("{ _id : 1, Address: { City: 'Seattle', Street: '1 Main' } }");
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void Insert_honors_annotated_complex_element_name()
    {
        var collection = database.CreateCollection<Customer>();

        {
            using var db = SingleEntityDbContext.Create(
                collection,
                modelBuilder =>
                {
                    modelBuilder.Entity<Customer>().ComplexProperty(c => c.Address);

                    // ComplexPropertyBuilder exposes no HasAnnotation in any EF version we target, so set the
                    // annotation on the mutable complex property directly (it survives FinalizeModel).
                    var complexProperty = modelBuilder.Model
                        .FindEntityType(typeof(Customer))!
                        .FindComplexProperty(nameof(Customer.Address))!;
                    complexProperty.SetAnnotation(MongoAnnotationNames.ElementName, "shipping");
                });

            db.Entities.Add(new Customer { Id = 1, Address = new Address { City = "Portland", Street = "9 SE" } });
            db.SaveChanges();
        }

        {
            var actual = collection
                .Find(Builders<Customer>.Filter.Empty)
                .Project(Builders<Customer>.Projection.As<BsonDocument>())
                .Single();

            var expected = BsonDocument.Parse("{ _id : 1, shipping: { City: 'Portland', Street: '9 SE' } }");
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void Insert_then_query_round_trips_complex_property()
    {
        var collection = database.CreateCollection<Customer>();
        using (var db = SingleEntityDbContext.Create(
                   collection,
                   modelBuilder => modelBuilder.Entity<Customer>().ComplexProperty(c => c.Address)))
        {
            db.Entities.Add(new Customer { Id = 1, Address = new Address { City = "Seattle", Street = "1 Main" } });
            db.SaveChanges();
        }

        using (var queryDb = SingleEntityDbContext.Create(
                   collection,
                   modelBuilder => modelBuilder.Entity<Customer>().ComplexProperty(c => c.Address)))
        {
            var customer = queryDb.Entities.Single(c => c.Id == 1);
            Assert.Equal("Seattle", customer.Address.City);
            Assert.Equal("1 Main", customer.Address.Street);
        }
    }

    [Fact]
    public void Insert_then_query_round_trips_annotated_complex_element_name()
    {
        var collection = database.CreateCollection<Customer>();
        using (var db = SingleEntityDbContext.Create(
                   collection,
                   modelBuilder =>
                   {
                       modelBuilder.Entity<Customer>().ComplexProperty(c => c.Address);
                       var complexProperty = modelBuilder.Model
                           .FindEntityType(typeof(Customer))!
                           .FindComplexProperty(nameof(Customer.Address))!;
                       complexProperty.SetAnnotation(MongoAnnotationNames.ElementName, "shipping");
                   }))
        {
            db.Entities.Add(new Customer { Id = 1, Address = new Address { City = "Boise", Street = "3 Idaho" } });
            db.SaveChanges();
        }

        using (var queryDb = SingleEntityDbContext.Create(
                   collection,
                   modelBuilder =>
                   {
                       modelBuilder.Entity<Customer>().ComplexProperty(c => c.Address);
                       var complexProperty = modelBuilder.Model
                           .FindEntityType(typeof(Customer))!
                           .FindComplexProperty(nameof(Customer.Address))!;
                       complexProperty.SetAnnotation(MongoAnnotationNames.ElementName, "shipping");
                   }))
        {
            var customer = queryDb.Entities.Single(c => c.Id == 1);
            Assert.Equal("Boise", customer.Address.City);
            Assert.Equal("3 Idaho", customer.Address.Street);
        }
    }

    [Fact]
    public void Insert_then_query_round_trips_nested_complex_property()
    {
        var collection = database.CreateCollection<Person>();
        Action<ModelBuilder> model = mb
            => mb.Entity<Person>().ComplexProperty(p => p.Contact, contact => contact.ComplexProperty(c => c.Address));

        using (var db = SingleEntityDbContext.Create(collection, model))
        {
            db.Entities.Add(new Person
            {
                Id = 1,
                Contact = new Contact { Address = new Address { City = "Portland", Street = "9 SE" } }
            });
            db.SaveChanges();
        }

        using (var queryDb = SingleEntityDbContext.Create(collection, model))
        {
            var person = queryDb.Entities.Single(p => p.Id == 1);
            Assert.Equal("Portland", person.Contact.Address.City);
            Assert.Equal("9 SE", person.Contact.Address.Street);
        }
    }

#if EF10
    [Fact]
    public void Insert_writes_collection_complex_property_as_array()
    {
        var collection = database.CreateCollection<Order>();

        {
            using var db = SingleEntityDbContext.Create(
                collection,
                modelBuilder => modelBuilder.Entity<Order>().ComplexCollection(o => o.Lines));

            db.Entities.Add(new Order
            {
                Id = 1,
                Lines = [new Line { Sku = "X1", Quantity = 2 }, new Line { Sku = "X2", Quantity = 5 }]
            });
            db.SaveChanges();
        }

        {
            var actual = collection
                .Find(Builders<Order>.Filter.Empty)
                .Project(Builders<Order>.Projection.As<BsonDocument>())
                .Single();

            var expected = BsonDocument.Parse(
                "{ _id : 1, Lines: [ { Quantity: 2, Sku: 'X1' }, { Quantity: 5, Sku: 'X2' } ] }");
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void Insert_then_query_round_trips_nullable_complex_property()
    {
        var collection = database.CreateCollection<NullableCustomer>();
        Action<ModelBuilder> model = mb => mb.Entity<NullableCustomer>().ComplexProperty(c => c.Address);

        using (var db = SingleEntityDbContext.Create(collection, model))
        {
            db.Entities.Add(new NullableCustomer { Id = 1, Address = new Address { City = "Oslo", Street = "5 Fjord" } });
            db.Entities.Add(new NullableCustomer { Id = 2, Address = null });
            db.SaveChanges();
        }

        using (var queryDb = SingleEntityDbContext.Create(collection, model))
        {
            var populated = queryDb.Entities.Single(c => c.Id == 1);
            Assert.Equal("Oslo", populated.Address!.City);
            Assert.Equal("5 Fjord", populated.Address.Street);

            var absent = queryDb.Entities.Single(c => c.Id == 2);
            Assert.Null(absent.Address);
        }
    }
#endif

    public class Person
    {
        public int Id { get; set; }
        public Contact Contact { get; set; } = null!;
    }

    public class Contact
    {
        public Address Address { get; set; } = null!;
    }

#if EF10
    public class NullableCustomer
    {
        public int Id { get; set; }
        public Address? Address { get; set; }
    }
#endif

    public class Line
    {
        public string Sku { get; set; } = null!;
        public int Quantity { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public List<Line> Lines { get; set; } = null!;
    }
}
