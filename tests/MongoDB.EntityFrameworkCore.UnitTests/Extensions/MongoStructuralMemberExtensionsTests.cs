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
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.UnitTests.Extensions;

public static class MongoStructuralMemberExtensionsTests
{
    [Fact]
    public static void GetElementName_defaults_to_property_name()
    {
        using var db = new TestContext<Holder>(mb => mb.Entity<Holder>().ComplexProperty(e => e.Address));

        var property = db.Model.FindEntityType(typeof(Holder))!.FindComplexProperty(nameof(Holder.Address))!;
        Assert.Equal("Address", property.GetElementName());
    }

    [Fact]
    public static void GetElementName_uses_element_name_annotation()
    {
        using var db = new TestContext<RenamedHolder>(mb =>
            mb.Entity<RenamedHolder>().ComplexProperty(e => e.Address).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "addr"));

        var property = db.Model.FindEntityType(typeof(RenamedHolder))!.FindComplexProperty(nameof(RenamedHolder.Address))!;
        Assert.Equal("addr", property.GetElementName());
    }

    [Fact]
    public static void Required_single_complex_property_is_neither_collection_nor_optional()
    {
        using var db = new TestContext<Holder>(mb => mb.Entity<Holder>().ComplexProperty(e => e.Address));

        var property = db.Model.FindEntityType(typeof(Holder))!.FindComplexProperty(nameof(Holder.Address))!;
        Assert.False(property.IsEmbeddedCollection());
        Assert.False(property.IsOptional());
    }

#if !EF8 && !EF9
    [Fact]
    public static void Complex_collection_is_embedded_collection()
    {
        using var db = new TestContext<CollectionHolder>(mb => mb.Entity<CollectionHolder>().ComplexCollection(e => e.Addresses));

        var property = db.Model.FindEntityType(typeof(CollectionHolder))!.FindComplexProperty(nameof(CollectionHolder.Addresses))!;
        Assert.True(property.IsEmbeddedCollection());
    }

    [Fact]
    public static void Optional_complex_property_is_optional()
    {
        using var db = new TestContext<OptionalHolder>(mb => mb.Entity<OptionalHolder>().ComplexProperty(e => e.Address).IsRequired(false));

        var property = db.Model.FindEntityType(typeof(OptionalHolder))!.FindComplexProperty(nameof(OptionalHolder.Address))!;
        Assert.True(property.IsOptional());
    }
#endif

    class Addr
    {
        public string Street { get; set; }
    }

    class Holder
    {
        public int Id { get; set; }
        public Addr Address { get; set; }
    }

    class RenamedHolder
    {
        public int Id { get; set; }
        public Addr Address { get; set; }
    }

    class OptionalHolder
    {
        public int Id { get; set; }
        public Addr Address { get; set; }
    }

#if !EF8 && !EF9
    class CollectionHolder
    {
        public int Id { get; set; }
        public List<Addr> Addresses { get; set; }
    }
#endif

    // Generic so each test gets its own context type (EF caches the model per context type).
    class TestContext<TEntity>(Action<ModelBuilder> configure) : DbContext where TEntity : class
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => configure(modelBuilder);
    }
}
