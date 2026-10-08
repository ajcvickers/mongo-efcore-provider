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
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// Whole-entity reads materialize complex properties from their stored sub-documents, on BOTH shapers the native
/// path can take: the one-pass streaming materializer (<c>MongoStreamingEntityMaterializerRewriter</c>, reached via
/// <c>.ToList()</c>) and the DOM shaper (reached via <c>.Single()</c>, which is never streaming-eligible). Ragged
/// states are seeded as raw <c>BsonDocument</c>s (the ragged-seed rule). Driver-LINQ has no complex-type oracle
/// (it throws <c>ExpressionNotSupportedException</c>), so these tests assert against hand-written expectations under
/// <c>NativeOnly</c>/<c>Native</c> only.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexMaterializerTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Address
    {
        public string City { get; set; } = "";
        public string Street { get; set; } = "";
    }

    private class Customer
    {
        public ObjectId Id { get; set; }
        public Address Address { get; set; } = null!;
    }

    private class Person
    {
        public ObjectId Id { get; set; }
        public Contact Contact { get; set; } = null!;
    }

    // Complex-in-complex: two dotted levels under the root.
    private class Contact
    {
        public Address Address { get; set; } = null!;
    }

    private static SingleEntityDbContext<T> CreateContext<T>(
        IMongoCollection<T> collection, MongoQueryMode mode, Action<ModelBuilder>? model = null)
        where T : class
        => SingleEntityDbContext.Create(
            collection,
            modelBuilderAction: model,
            optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });

    [Fact]
    public void Whole_entity_read_materializes_complex_property()
    {
        var collection = database.CreateCollection<Customer>();
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Address", new BsonDocument { { "City", "Seattle" }, { "Street", "1 Main" } } }
        });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<Customer>().ComplexProperty(c => c.Address));
        var customer = nativeOnly.Entities.ToList().Single();

        Assert.Equal("Seattle", customer.Address.City);
        Assert.Equal("1 Main", customer.Address.Street);
    }

    [Fact]
    public void Whole_entity_read_materializes_complex_property_via_dom_shaper()
    {
        var collection = database.CreateCollection<Customer>(values: ["dom"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Address", new BsonDocument { { "City", "Portland" }, { "Street", "9 SE" } } }
        });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<Customer>().ComplexProperty(c => c.Address));
        var customer = nativeOnly.Entities.Single();

        Assert.Equal("Portland", customer.Address.City);
        Assert.Equal("9 SE", customer.Address.Street);
    }

    [Fact]
    public void Nested_complex_property_materializes_recursively()
    {
        var collection = database.CreateCollection<Person>();
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Contact", new BsonDocument { { "Address", new BsonDocument { { "City", "Portland" }, { "Street", "5 Naito" } } } } }
        });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<Person>().ComplexProperty(p => p.Contact, contact => contact.ComplexProperty(c => c.Address)));
        var person = nativeOnly.Entities.ToList().Single();

        Assert.Equal("Portland", person.Contact.Address.City);
        Assert.Equal("5 Naito", person.Contact.Address.Street);
    }

    [Fact]
    public void Nested_complex_property_materializes_recursively_via_dom_shaper()
    {
        var collection = database.CreateCollection<Person>(values: ["dom"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Contact", new BsonDocument { { "Address", new BsonDocument { { "City", "Seattle" }, { "Street", "1 Main" } } } } }
        });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<Person>().ComplexProperty(p => p.Contact, contact => contact.ComplexProperty(c => c.Address)));
        var person = nativeOnly.Entities.Single();

        Assert.Equal("Seattle", person.Contact.Address.City);
        Assert.Equal("1 Main", person.Contact.Address.Street);
    }

    [Fact]
    public void Missing_required_complex_element_throws_strict()
    {
        var collection = database.CreateCollection<Customer>(values: ["missing"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() } });

        foreach (var mode in new[] { MongoQueryMode.Native, MongoQueryMode.NativeOnly })
        {
            using var context = CreateContext(
                collection, mode, mb => mb.Entity<Customer>().ComplexProperty(c => c.Address));
            // Whole-entity reads stay strict: a required complex with no stored element throws, as an
            // owned reference's "required but not present" does — never a silent null.
            Assert.Throws<InvalidOperationException>(() => context.Entities.ToList());
        }
    }

    [Fact]
    public void Missing_required_leaf_inside_present_subdocument_throws()
    {
        var collection = database.CreateCollection<Customer>(values: ["missingleaf"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Address", new BsonDocument { { "City", "Seattle" } } } // Street missing
        });

        foreach (var mode in new[] { MongoQueryMode.Native, MongoQueryMode.NativeOnly })
        {
            using var context = CreateContext(
                collection, mode, mb => mb.Entity<Customer>().ComplexProperty(c => c.Address));
            Assert.Throws<InvalidOperationException>(() => context.Entities.ToList());
        }
    }

#if EF10
    private class NullableCustomer
    {
        public ObjectId Id { get; set; }
        public Address? Address { get; set; }
    }

    private class NullablePerson
    {
        public ObjectId Id { get; set; }
        public Contact? Contact { get; set; }
    }

    private struct StructLocation
    {
        public string City { get; set; }
        public string Street { get; set; }
    }

    private class StructHolder
    {
        public ObjectId Id { get; set; }
        public StructLocation? Location { get; set; }
    }

    private class Order
    {
        public ObjectId Id { get; set; }
        public List<Line> Lines { get; set; } = [];
    }

    private class Line
    {
        public string Sku { get; set; } = "";
        public int Quantity { get; set; }
    }

    [Fact]
    public void Missing_element_materializes_default_for_nullable_complex()
    {
        var collection = database.CreateCollection<NullableCustomer>();
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() } });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<NullableCustomer>().ComplexProperty(c => c.Address));
        var customer = nativeOnly.Entities.ToList().Single();

        Assert.Null(customer.Address);
    }

    [Fact]
    public void Missing_element_materializes_default_for_nullable_complex_via_dom_shaper()
    {
        var collection = database.CreateCollection<NullableCustomer>(values: ["dom"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() } });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<NullableCustomer>().ComplexProperty(c => c.Address));
        var customer = nativeOnly.Entities.Single();

        Assert.Null(customer.Address);
    }

    [Fact]
    public void Explicit_null_subdocument_materializes_default_for_nullable_complex()
    {
        var collection = database.CreateCollection<NullableCustomer>(values: ["null"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Address", BsonNull.Value } });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<NullableCustomer>().ComplexProperty(c => c.Address));
        var customer = nativeOnly.Entities.ToList().Single();

        Assert.Null(customer.Address);
    }

    [Fact]
    public void Populated_nullable_complex_materializes_values()
    {
        var collection = database.CreateCollection<NullableCustomer>(values: ["populated"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Address", new BsonDocument { { "City", "Boise" }, { "Street", "3 Idaho" } } }
        });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<NullableCustomer>().ComplexProperty(c => c.Address));
        var customer = nativeOnly.Entities.ToList().Single();

        Assert.NotNull(customer.Address);
        Assert.Equal("Boise", customer.Address!.City);
    }

    private class MixedStats
    {
        public int Quantity { get; set; }          // value-type leaf
        public string Sku { get; set; } = null!;   // reference-type leaf
    }

    private class NullableMixedCustomer
    {
        public ObjectId Id { get; set; }
        public MixedStats? Stats { get; set; }
    }

    private class NullableQuantityStats
    {
        public int? Quantity { get; set; }
        public string Sku { get; set; } = null!;
    }

    private class NullableNullableStatsCustomer
    {
        public ObjectId Id { get; set; }
        public NullableQuantityStats? Stats { get; set; }
    }

    // EF10's all-flattened-leaves-null check must see a TRUE null for value-type leaves too: a nullable CLASS
    // complex whose leaves are all explicitly null materializes null, never a populated zero-value instance.
    [Fact]
    public void Nullable_class_complex_with_value_type_leaf_all_null_materializes_null()
    {
        var collection = database.CreateCollection<NullableMixedCustomer>();
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Stats", new BsonDocument { { "Quantity", BsonNull.Value }, { "Sku", BsonNull.Value } } }
        });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<NullableMixedCustomer>().ComplexProperty(c => c.Stats));
        var customer = nativeOnly.Entities.ToList().Single();

        Assert.Null(customer.Stats);
    }

    [Fact]
    public void Nullable_class_complex_with_value_type_leaf_all_null_materializes_null_via_dom_shaper()
    {
        var collection = database.CreateCollection<NullableMixedCustomer>(values: ["dom"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Stats", new BsonDocument { { "Quantity", BsonNull.Value }, { "Sku", BsonNull.Value } } }
        });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<NullableMixedCustomer>().ComplexProperty(c => c.Stats));
        var customer = nativeOnly.Entities.Single();

        Assert.Null(customer.Stats);
    }

    [Fact]
    public void Nullable_class_complex_with_value_type_leaf_materializes_populated()
    {
        var collection = database.CreateCollection<NullableMixedCustomer>(values: ["populated"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Stats", new BsonDocument { { "Quantity", 7 }, { "Sku", "X1" } } }
        });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<NullableMixedCustomer>().ComplexProperty(c => c.Stats));
        var customer = nativeOnly.Entities.ToList().Single();

        Assert.NotNull(customer.Stats);
        Assert.Equal(7, customer.Stats!.Quantity);
        Assert.Equal("X1", customer.Stats.Sku);
    }

    // Mixed leaves with a REQUIRED value-type leaf explicitly null: EF10 core's nullable-complex null check
    // (StructuralTypeMaterializerSource.HandleNullableComplexTypeMaterialization) checks ONLY the first required
    // scalar leaf when one exists — Quantity reads null, so the whole complex materializes default(null) without
    // ever reading the non-null sibling. Both shapers must match that, not throw and not populate.
    [Fact]
    public void Required_value_type_leaf_explicit_null_materializes_default_like_ef_core()
    {
        var collection = database.CreateCollection<NullableMixedCustomer>(values: ["mixed-strict"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Stats", new BsonDocument { { "Quantity", BsonNull.Value }, { "Sku", "X1" } } }
        });

        using var streaming = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<NullableMixedCustomer>().ComplexProperty(c => c.Stats));
        var streamingCustomer = streaming.Entities.ToList().Single();
        Assert.Null(streamingCustomer.Stats);

        using var dom = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<NullableMixedCustomer>().ComplexProperty(c => c.Stats));
        var domCustomer = dom.Entities.Single();
        Assert.Null(domCustomer.Stats);
    }

    // The nullable-leaf mixed variant materializes populated with the null member read as null — the
    // NullableContext local read must convert back to the member type without losing the sibling's value.
    [Fact]
    public void Nullable_value_type_leaf_null_with_non_null_sibling_materializes_populated()
    {
        var collection = database.CreateCollection<NullableNullableStatsCustomer>(values: ["mixed-nullable"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Stats", new BsonDocument { { "Quantity", BsonNull.Value }, { "Sku", "X1" } } }
        });

        using var streaming = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<NullableNullableStatsCustomer>().ComplexProperty(c => c.Stats));
        var customer = streaming.Entities.ToList().Single();

        Assert.NotNull(customer.Stats);
        Assert.Null(customer.Stats!.Quantity);
        Assert.Equal("X1", customer.Stats.Sku);

        using var dom = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<NullableNullableStatsCustomer>().ComplexProperty(c => c.Stats));
        var domCustomer = dom.Entities.Single();

        Assert.NotNull(domCustomer.Stats);
        Assert.Null(domCustomer.Stats!.Quantity);
        Assert.Equal("X1", domCustomer.Stats.Sku);
    }

    [Fact]
    public void All_null_leaves_materialize_default_for_struct()
    {
        var collection = database.CreateCollection<StructHolder>();
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Location", new BsonDocument { { "City", BsonNull.Value }, { "Street", BsonNull.Value } } }
        });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<StructHolder>().ComplexProperty(h => h.Location));
        var holder = nativeOnly.Entities.ToList().Single();

        // Structural-null semantics: all flattened leaves null => default — for a nullable struct complex the
        // materialized value is null (ClrType is the Nullable<StructLocation> itself), exactly as EF10's
        // HandleNullableComplexTypeMaterialization Default(clrType) produces.
        Assert.Null(holder.Location);
    }

    [Fact]
    public void Populated_struct_complex_materializes_values()
    {
        var collection = database.CreateCollection<StructHolder>(values: ["populated"]);
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Location", new BsonDocument { { "City", "Oslo" }, { "Street", "5 Fjord" } } }
        });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<StructHolder>().ComplexProperty(h => h.Location));
        var holder = nativeOnly.Entities.ToList().Single();

        Assert.Equal(new StructLocation { City = "Oslo", Street = "5 Fjord" }, holder.Location);
    }

    [Fact]
    public void Nullable_nested_complex_with_all_flattened_leaves_null_materializes_default()
    {
        var collection = database.CreateCollection<NullablePerson>();
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            // Contact present but every flattened leaf (including the nested Address's) null: EF10's
            // all-flattened-leaves check materializes the whole nullable complex as default.
            { "Contact", new BsonDocument() }
        });

        using var nativeOnly = CreateContext(
            collection, MongoQueryMode.NativeOnly,
            mb => mb.Entity<NullablePerson>().ComplexProperty(p => p.Contact, contact => contact.ComplexProperty(c => c.Address)));
        var person = nativeOnly.Entities.ToList().Single();

        Assert.Null(person.Contact);
    }

    // A root-level complex collection materializes on the streaming shaper (Task 7): the whole stored array
    // reads in one pass through the property's own collection serializer, in both native modes.
    [Fact]
    public void Complex_collection_query_materializes()
    {
        var collection = database.CreateCollection<Order>();
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            {
                "Lines", new BsonArray
                {
                    new BsonDocument { { "Sku", "X1" }, { "Quantity", 2 } },
                    new BsonDocument { { "Sku", "X2" }, { "Quantity", 5 } }
                }
            }
        });

        foreach (var mode in new[] { MongoQueryMode.Native, MongoQueryMode.NativeOnly })
        {
            using var context = CreateContext(
                collection, mode,
                mb => mb.Entity<Order>().ComplexCollection(o => o.Lines));
            var order = context.Entities.ToList().Single();
            Assert.Equal(2, order.Lines.Count);
            Assert.Equal("X1", order.Lines[0].Sku);
            Assert.Equal(2, order.Lines[0].Quantity);
            Assert.Equal(5, order.Lines[1].Quantity);
        }
    }

    // Whole-entity complex collections routed to the DOM shaper have NO driver-LINQ oracle: materialization
    // always runs through the provider's shaper (driver LINQ supplies MQL only), so the DOM decline surfaces
    // under DriverLinq too — a no-oracle decline, never silent wrong data.
    [Fact]
    public void Complex_collection_dom_route_declines_under_driver_linq()
    {
        var collection = database.CreateCollection<Order>();
        var raw = database.GetCollection<BsonDocument>(collection.CollectionNamespace);
        raw.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            {
                "Lines", new BsonArray
                {
                    new BsonDocument { { "Sku", "X1" }, { "Quantity", 2 } },
                    new BsonDocument { { "Sku", "X2" }, { "Quantity", 5 } }
                }
            }
        });

        using var context = CreateContext(
            collection, MongoQueryMode.DriverLinq,
            mb => mb.Entity<Order>().ComplexCollection(o => o.Lines));

        var exception = Assert.Throws<NativeTranslationNotSupportedException>(() => context.Entities.ToList());
        Assert.Contains("DOM materializer", exception.Message);
        Assert.Contains("streaming", exception.Message);
    }
#endif
}
