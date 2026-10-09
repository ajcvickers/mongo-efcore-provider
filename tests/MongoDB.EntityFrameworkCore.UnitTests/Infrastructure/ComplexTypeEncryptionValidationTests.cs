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
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.UnitTests.Infrastructure;

/// <summary>
/// Encryption is not supported inside complex types: neither the Queryable Encryption schema generator nor the
/// encryption validation walks complex types, so an encryption annotation there would be silently ignored and the
/// value written as plaintext. The model validator must reject it.
/// </summary>
public static class ComplexTypeEncryptionValidationTests
{
    private static readonly Guid DataKey = Guid.Parse("8a6c0e7d-60a5-4f71-9f7a-3a6d2a9b6f01");

    public static TheoryData<string, object> EncryptionAnnotations => new()
    {
        { MongoAnnotationNames.QueryableEncryptionType, QueryableEncryptionType.NotQueryable },
        { MongoAnnotationNames.QueryableEncryptionType, QueryableEncryptionType.Equality },
        { MongoAnnotationNames.QueryableEncryptionType, QueryableEncryptionType.Range },
        { MongoAnnotationNames.EncryptionDataKeyId, DataKey },
        { MongoAnnotationNames.QueryableEncryptionContention, 4 },
        { MongoAnnotationNames.QueryableEncryptionRangeMin, 0 },
        { MongoAnnotationNames.QueryableEncryptionRangeMax, 10 },
        { MongoAnnotationNames.QueryableEncryptionTrimFactor, 2 },
        { MongoAnnotationNames.QueryableEncryptionPrecision, 2 },
        { MongoAnnotationNames.QueryableEncryptionSparsity, 2 },
    };

    [Theory]
    [MemberData(nameof(EncryptionAnnotations))]
    public static void Encryption_annotation_on_leaf_inside_complex_type_is_rejected(string annotation, object value)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<Customer>(mb =>
                mb.Entity<Customer>().ComplexProperty(c => c.Home).Property(a => a.City).Metadata.SetAnnotation(annotation, value));
            _ = db.Model;
        });

        AssertMessage(ex, nameof(Addr.City), nameof(Addr));
    }

    [Fact]
    public static void Queryable_encryption_with_data_key_on_leaf_inside_complex_type_is_rejected()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<Customer>(mb =>
            {
                var leaf = mb.Entity<Customer>().ComplexProperty(c => c.Home).Property(a => a.City).Metadata;
                leaf.SetAnnotation(MongoAnnotationNames.QueryableEncryptionType, QueryableEncryptionType.Equality);
                leaf.SetAnnotation(MongoAnnotationNames.EncryptionDataKeyId, DataKey);
            });
            _ = db.Model;
        });

        AssertMessage(ex, nameof(Addr.City), nameof(Addr));
    }

    [Fact]
    public static void Encryption_annotation_on_complex_property_itself_is_rejected()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<Customer>(mb =>
            {
                var complex = mb.Entity<Customer>().ComplexProperty(c => c.Home).Metadata;
                complex.SetAnnotation(MongoAnnotationNames.QueryableEncryptionType, QueryableEncryptionType.NotQueryable);
                complex.SetAnnotation(MongoAnnotationNames.EncryptionDataKeyId, DataKey);
            });
            _ = db.Model;
        });

        AssertMessage(ex, nameof(Customer.Home), nameof(Addr));
        Assert.Contains("Complex property", ex.Message);
    }

    [Fact]
    public static void Encryption_annotation_on_leaf_inside_nested_complex_type_is_rejected()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<NestedCustomer>(mb =>
                mb.Entity<NestedCustomer>().ComplexProperty(c => c.Home, h => h.ComplexProperty(a => a.Geo, g =>
                    g.Property(x => x.Lat).Metadata
                        .SetAnnotation(MongoAnnotationNames.QueryableEncryptionType, QueryableEncryptionType.Range))));
            _ = db.Model;
        });

        AssertMessage(ex, nameof(Geo.Lat), nameof(Geo));
    }

    [Fact]
    public static void Encryption_annotation_on_complex_property_inside_owned_entity_is_rejected()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<OwnerCustomer>(mb =>
            {
                // An owned navigation builder cannot configure a complex property fluently, so [ComplexType] maps it
                // by convention and the leaf is annotated through the metadata API.
                mb.Entity<OwnerCustomer>().OwnsOne(c => c.Profile);
                mb.Model.FindEntityType(typeof(Profile))!.FindComplexProperty(nameof(Profile.Home))!.ComplexType
                    .FindProperty(nameof(MarkedAddr.City))!
                    .SetAnnotation(MongoAnnotationNames.QueryableEncryptionType, QueryableEncryptionType.Equality);
            });
            _ = db.Model;
        });

        AssertMessage(ex, nameof(MarkedAddr.City), nameof(MarkedAddr));
    }

#if !EF8 && !EF9
    [Fact]
    public static void Encryption_annotation_on_leaf_inside_complex_collection_is_rejected()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<CollectionCustomer>(mb =>
                mb.Entity<CollectionCustomer>().ComplexCollection(c => c.Homes).Property(a => a.City).Metadata
                    .SetAnnotation(MongoAnnotationNames.QueryableEncryptionType, QueryableEncryptionType.NotQueryable));
            _ = db.Model;
        });

        AssertMessage(ex, nameof(Addr.City), nameof(Addr));
    }

    [Fact]
    public static void Encryption_annotation_on_complex_collection_itself_is_rejected()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<CollectionCustomer>(mb =>
                mb.Entity<CollectionCustomer>().ComplexCollection(c => c.Homes).Metadata
                    .SetAnnotation(MongoAnnotationNames.EncryptionDataKeyId, DataKey));
            _ = db.Model;
        });

        AssertMessage(ex, nameof(CollectionCustomer.Homes), nameof(Addr));
    }
#endif

    [Fact]
    public static void Encryption_on_entity_property_beside_a_complex_property_is_still_allowed()
    {
        using var db = new TestContext<Customer>(mb =>
        {
            mb.Entity<Customer>().Property(c => c.Name).IsEncrypted(DataKey);
            mb.Entity<Customer>().ComplexProperty(c => c.Home);
        });

        var schema = QueryableEncryptionSchemaGenerator.GenerateSchemas(db.Model);
        Assert.Single(schema);
    }

    private static void AssertMessage(InvalidOperationException ex, string member, string complexType)
    {
        Assert.Contains($"'{member}'", ex.Message);
        Assert.Contains(complexType, ex.Message);
        Assert.Contains("not supported for complex properties or for properties inside complex types", ex.Message);
    }

    private class Addr
    {
        public string City { get; set; } = "";
    }

    private class Geo
    {
        public double Lat { get; set; }
    }

    private class NestedAddr
    {
        public string City { get; set; } = "";
        public Geo Geo { get; set; } = new();
    }

    private class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public Addr Home { get; set; } = new();
    }

    private class NestedCustomer
    {
        public int Id { get; set; }
        public NestedAddr Home { get; set; } = new();
    }

    [System.ComponentModel.DataAnnotations.Schema.ComplexType]
    private class MarkedAddr
    {
        public string City { get; set; } = "";
    }

    private class Profile
    {
        public string Nick { get; set; } = "";
        public MarkedAddr Home { get; set; } = new();
    }

    private class OwnerCustomer
    {
        public int Id { get; set; }
        public Profile Profile { get; set; } = new();
    }

    private class CollectionCustomer
    {
        public int Id { get; set; }
        public List<Addr> Homes { get; set; } = [];
    }

    // Generic so each test gets its own context type (EF caches the model per context type); the configure delegate
    // differs per test, so the model cache is also disabled.
    private class TestContext<TEntity>(Action<ModelBuilder> configure) : DbContext where TEntity : class
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, NoCacheKeyFactory>()
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => configure(modelBuilder);
    }

    private sealed class NoCacheKeyFactory : Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory
    {
        private static int _counter;

        public object Create(DbContext context, bool designTime)
            => Interlocked.Increment(ref _counter);
    }
}
