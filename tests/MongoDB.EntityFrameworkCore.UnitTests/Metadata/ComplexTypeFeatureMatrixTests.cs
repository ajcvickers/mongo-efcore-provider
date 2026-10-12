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

namespace MongoDB.EntityFrameworkCore.UnitTests.Metadata;

/// <summary>
/// Probes which complex type features the pinned EF Core version supports in the provider's convention set.
/// </summary>
public static class ComplexTypeFeatureMatrixTests
{
    [Fact]
    public static void Class_complex_property_builds()
    {
        using var db = new MatrixContext<ClassHolder>(mb => mb.Entity<ClassHolder>().ComplexProperty(e => e.Address));

        var property = db.Model.FindEntityType(typeof(ClassHolder))!.FindComplexProperty(nameof(ClassHolder.Address));
        Assert.NotNull(property);
        Assert.False(property.IsCollection);
        Assert.NotNull(property.ComplexType.FindProperty(nameof(Addr.Street)));
    }

    [Fact]
    public static void Struct_complex_property_builds()
    {
        using var db = new MatrixContext<StructHolder>(mb => mb.Entity<StructHolder>().ComplexProperty(e => e.Point));

        var property = db.Model.FindEntityType(typeof(StructHolder))!.FindComplexProperty(nameof(StructHolder.Point));
        Assert.NotNull(property);
        Assert.True(property.ComplexType.ClrType.IsValueType);
        Assert.NotNull(property.ComplexType.FindProperty(nameof(Pt.Lat)));
    }

    [Fact]
    public static void Nested_complex_property_builds()
    {
        using var db = new MatrixContext<NestedHolder>(mb => mb.Entity<NestedHolder>().ComplexProperty(e => e.Address, b => b.ComplexProperty(a => a.Location)));

        var outer = db.Model.FindEntityType(typeof(NestedHolder))!.FindComplexProperty(nameof(NestedHolder.Address));
        Assert.NotNull(outer);
        var inner = outer.ComplexType.FindComplexProperty(nameof(Addr2.Location));
        Assert.NotNull(inner);
        Assert.NotNull(inner.ComplexType.FindProperty(nameof(Pt.Lon)));
    }

#if !EF8 && !EF9
    [Fact]
    public static void Optional_complex_property_builds()
    {
        using var db = new MatrixContext<OptionalHolder>(mb => mb.Entity<OptionalHolder>().ComplexProperty(e => e.Address));

        var property = db.Model.FindEntityType(typeof(OptionalHolder))!.FindComplexProperty(nameof(OptionalHolder.Address));
        Assert.NotNull(property);
        Assert.True(property.IsNullable);
    }
#else
    [Fact]
    public static void Optional_complex_property_is_rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new MatrixContext<OptionalHolder>(mb => mb.Entity<OptionalHolder>().ComplexProperty(e => e.Address));
            _ = db.Model;
        });
        Assert.Contains("IsRequired", exception.Message);
    }
#endif

#if !EF8 && !EF9
    [Fact]
    public static void Complex_collection_builds()
    {
        using var db = new MatrixContext<CollectionHolder>(mb => mb.Entity<CollectionHolder>().ComplexCollection(e => e.Addresses));

        var property = db.Model.FindEntityType(typeof(CollectionHolder))!.FindComplexProperty(nameof(CollectionHolder.Addresses));
        Assert.NotNull(property);
        Assert.True(property.IsCollection);
    }
#endif

    class Addr
    {
        public string Street { get; set; }
    }

    struct Pt
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    class Addr2
    {
        public string Street { get; set; }
        public Pt Location { get; set; }
    }

    class ClassHolder
    {
        public int Id { get; set; }
        public Addr Address { get; set; }
    }

    class StructHolder
    {
        public int Id { get; set; }
        public Pt Point { get; set; }
    }

    class NestedHolder
    {
        public int Id { get; set; }
        public Addr2 Address { get; set; }
    }

    class OptionalHolder
    {
        public int Id { get; set; }
        public Addr? Address { get; set; }
    }

#if !EF8 && !EF9
    class CollectionHolder
    {
        public int Id { get; set; }
        public List<Addr> Addresses { get; set; }
    }
#endif

    // Generic so each test gets its own context type (EF caches the model per context type).
    class MatrixContext<TEntity>(Action<ModelBuilder> configure) : DbContext where TEntity : class
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => configure(modelBuilder);
    }
}
