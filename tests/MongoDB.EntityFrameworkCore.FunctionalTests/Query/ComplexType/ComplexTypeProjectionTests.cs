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
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// Whole complex-property projection leaves (<c>Select(c => new { c.Name, c.Address })</c>): the sub-document
/// projects under the complex property's element name (the alias-agreement invariant) and materializes through
/// the complex-type serializer. See NativeProjectionBinder.TryGetComplexPropertyLeaf.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeProjectionTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Address
    {
        public string City { get; set; } = "";
        public string Zip { get; set; } = "";
    }

    private class Customer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public Address Address { get; set; } = null!;
    }

    private static readonly Action<ModelBuilder> CustomerModel =
        mb => mb.Entity<Customer>().ComplexProperty(c => c.Address);

    private class NullableCustomer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public Address? Address { get; set; }
    }

    private static readonly Action<ModelBuilder> NullableCustomerModel =
        mb => mb.Entity<NullableCustomer>().ComplexProperty(c => c.Address);

    private static SingleEntityDbContext<T> CreateContext<T>(
        IMongoCollection<T> collection, MongoQueryMode mode, Action<ModelBuilder>? modelBuilderAction = null)
        where T : class
        => SingleEntityDbContext.Create(
            collection,
            modelBuilderAction: modelBuilderAction,
            optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });

    private static SingleEntityDbContext<T> CreateContextWithLogging<T>(
        IMongoCollection<T> collection, MongoQueryMode mode, Action<ModelBuilder> modelBuilderAction,
        out SpyLoggerProvider spyLogger)
        where T : class
    {
        var (loggerFactory, provider) = SpyLoggerProvider.Create();
        spyLogger = provider;

        return SingleEntityDbContext.Create(
            collection,
            loggerFactory,
            modelBuilderAction: modelBuilderAction,
            optionsBuilderAction: b =>
            {
                b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                b.EnableSensitiveDataLogging();
                new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
            });
    }

    private string UniqueCollectionName(string name)
        => TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];

    private IMongoCollection<Customer> SeedCustomers(string name)
    {
        var coll = database.MongoDatabase.GetCollection<BsonDocument>(UniqueCollectionName(name));
        coll.InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Alpha" },
                { "Address", new BsonDocument { { "City", "NYC" }, { "Zip", "10001" } } }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Beta" },
                { "Address", new BsonDocument { { "City", "LA" }, { "Zip", "90001" } } }
            },
        ]);
        return database.MongoDatabase.GetCollection<Customer>(coll.CollectionNamespace.CollectionName);
    }

    [Fact]
    public void Whole_complex_property_in_anonymous_projection_goes_native_and_reads_correct_values()
    {
        var collection = SeedCustomers(nameof(Whole_complex_property_in_anonymous_projection_goes_native_and_reads_correct_values));
        using var db = CreateContext(collection, MongoQueryMode.NativeOnly, CustomerModel);

        // Success under NativeOnly is the routing proof.
        var results = db.Entities.AsNoTracking()
            .Select(c => new { c.Name, c.Address })
            .OrderBy(r => r.Name)
            .ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("Alpha", results[0].Name);
        Assert.Equal("NYC", results[0].Address.City);
        Assert.Equal("10001", results[0].Address.Zip);
        Assert.Equal("Beta", results[1].Name);
        Assert.Equal("LA", results[1].Address.City);
    }

    // The alias must equal the complex property's document path: with a Mongo:ElementName annotation the stored
    // element is "shipping", so the seed uses that element (raw documents — a context-seeded fixture couldn't
    // express the annotation/element split), the $project aliases it, and the shaper reads it back.
    [Fact]
    public void Projection_leaf_alias_uses_element_name()
    {
        var coll = database.MongoDatabase.GetCollection<BsonDocument>(
            UniqueCollectionName(nameof(Projection_leaf_alias_uses_element_name)));
        coll.InsertMany(
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Alpha" },
                { "shipping", new BsonDocument { { "City", "NYC" }, { "Zip", "10001" } } }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Beta" },
                { "shipping", new BsonDocument { { "City", "LA" }, { "Zip", "90001" } } }
            },
        ]);
        var collection = database.MongoDatabase.GetCollection<Customer>(coll.CollectionNamespace.CollectionName);

        using var db = CreateContextWithLogging(
            collection, MongoQueryMode.NativeOnly, mb =>
            {
                CustomerModel(mb);

                // ComplexPropertyBuilder exposes no HasAnnotation in any EF version we target, so set the
                // annotation on the mutable complex property directly (it survives FinalizeModel).
                var complexProperty = mb.Model
                    .FindEntityType(typeof(Customer))!
                    .FindComplexProperty(nameof(Customer.Address))!;
                complexProperty.SetAnnotation(MongoAnnotationNames.ElementName, "shipping");
            }, out var spyLogger);

        var results = db.Entities.AsNoTracking()
            .Select(c => new { c.Name, c.Address })
            .OrderBy(r => r.Name)
            .ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("NYC", results[0].Address.City);
        Assert.Equal("10001", results[0].Address.Zip);
        Assert.Equal("LA", results[1].Address.City);

        // The alias must equal the complex property's document path ("shipping"): a fallback strips the
        // projection and hands the shaper whole documents, which only carry an element named `alias`.
        spyLogger.AssertExecutedMqlContains(
            "{ \"$project\" : { \"Name\" : \"$Name\", \"shipping\" : \"$shipping\", \"_id\" : \"$_id\" } }");
    }

    // A bare complex-property body declines like the owned-reference-entity leaf's bare body (wrapped bodies
    // only): a bare body's alias is derived from the leaf, and the fallback read cannot answer for it. Under
    // Native mode the fallback must still read the correct values off whole documents.
    [Fact]
    public void Bare_complex_property_body_declines_but_reads_correct_values()
    {
        var collection = SeedCustomers(nameof(Bare_complex_property_body_declines_but_reads_correct_values));

        using (var nativeOnly = CreateContext(collection, MongoQueryMode.NativeOnly, CustomerModel))
        {
            Assert.Throws<NativeTranslationNotSupportedException>(
                () => nativeOnly.Entities.AsNoTracking().Select(c => c.Address).ToList());
        }

        using var db = CreateContext(collection, MongoQueryMode.Native, CustomerModel);
        var results = db.Entities.AsNoTracking()
            .Select(c => c.Address)
            .OrderBy(a => a.City)
            .ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("LA", results[0].City);
        Assert.Equal("NYC", results[1].City);
    }

    // Driver LINQ materializes complex members through the same serializer work (Task 2), so it is a working
    // oracle here: the native results must match it exactly.
    [Fact]
    public void Complex_property_projection_parity_between_native_and_driver_linq()
    {
        var collection = SeedCustomers(nameof(Complex_property_projection_parity_between_native_and_driver_linq));

        List<(string Name, string City, string Zip)> driver;
        using (var db = CreateContext(collection, MongoQueryMode.DriverLinq, CustomerModel))
        {
            driver = db.Entities.AsNoTracking()
                .Select(c => new { c.Name, c.Address })
                .OrderBy(r => r.Name)
                .ToList()
                .Select(r => (r.Name, r.Address.City, r.Address.Zip)).ToList();
        }

        List<(string Name, string City, string Zip)> native;
        using (var db = CreateContext(collection, MongoQueryMode.NativeOnly, CustomerModel))
        {
            native = db.Entities.AsNoTracking()
                .Select(c => new { c.Name, c.Address })
                .OrderBy(r => r.Name)
                .ToList()
                .Select(r => (r.Name, r.Address.City, r.Address.Zip)).ToList();
        }

        Assert.Equal(driver, native);
    }

    // A complex type whose LEAF members carry Mongo:ElementName annotations: the write path stores them under
    // those names (Task 2), so the projection read must go through the complex-type serializer — a CLR class-map
    // read would look for "City" and read null. Pins the serializer-aware alias read.
    [Fact]
    public void Projection_reads_complex_leaf_element_annotations()
    {
        var collection = database.CreateCollection<Customer>();

        static Action<ModelBuilder> AnnotatedModel(Action<ModelBuilder> tail) => mb =>
        {
            mb.Entity<Customer>().ComplexProperty(c => c.Address, address =>
                address.Property(a => a.City).HasAnnotation(MongoAnnotationNames.ElementName, "town"));
            tail(mb);
        };

        // Seed through a context configured the same way, so the stored shape proves the write path.
        using (var db = SingleEntityDbContext.Create(collection, AnnotatedModel(_ => { })))
        {
            db.Entities.Add(new Customer { Id = ObjectId.GenerateNewId(), Name = "Alpha", Address = new Address { City = "NYC", Zip = "10001" } });
            db.SaveChanges();
        }

        var stored = collection
            .Find(Builders<Customer>.Filter.Empty)
            .Project(Builders<Customer>.Projection.As<BsonDocument>())
            .Single();
        Assert.Equal("NYC", stored["Address"]["town"].AsString);

        // The query context uses the SAME annotated model: the serializer-aware read resolves the complex
        // property from it, so the leaf annotation applies to the read side too.
        using (var queryDb = CreateContext(collection, MongoQueryMode.NativeOnly, AnnotatedModel(_ => { })))
        {
            var projected = Assert.Single(
                queryDb.Entities.AsNoTracking().Select(c => new { c.Name, c.Address }).ToList());
            Assert.Equal("Alpha", projected.Name);
            Assert.Equal("NYC", projected.Address.City);
        }
    }

#if EF10
    // EF10-only: a nullable complex property materializes default when the element is missing — including as a
    // projection leaf. Ragged-seeded per the ragged-seed rule.
    [Fact]
    public void Projected_nullable_complex_reads_missing_element_as_default()
    {
        var coll = database.MongoDatabase.GetCollection<BsonDocument>(
            UniqueCollectionName(nameof(Projected_nullable_complex_reads_missing_element_as_default)));
        coll.InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "NoAddr" } },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "WithAddr" },
                { "Address", new BsonDocument { { "City", "NYC" }, { "Zip", "10001" } } }
            }
        ]);
        var collection = database.MongoDatabase.GetCollection<NullableCustomer>(coll.CollectionNamespace.CollectionName);

        using var db = CreateContext(collection, MongoQueryMode.NativeOnly, NullableCustomerModel);

        var results = db.Entities.AsNoTracking()
            .Select(c => new { c.Name, c.Address })
            .OrderBy(r => r.Name)
            .ToList();

        Assert.Equal(2, results.Count);
        Assert.Null(results.Single(r => r.Name == "NoAddr").Address);
        Assert.Equal("NYC", results.Single(r => r.Name == "WithAddr").Address!.City);
    }
#endif
}
