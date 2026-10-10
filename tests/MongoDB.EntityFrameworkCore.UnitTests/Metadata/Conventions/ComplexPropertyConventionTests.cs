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

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Metadata.Conventions;

namespace MongoDB.EntityFrameworkCore.UnitTests.Metadata.Conventions;

public static class ComplexPropertyConventionTests
{
    [Fact]
    public static void CamelCase_applies_to_complex_property_and_its_leaves()
    {
        using var db = new TestContext<CamelHolder>(mb =>
            {
                mb.Entity<CamelHolder>().Property(e => e.Id).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "_id");
                mb.Entity<CamelHolder>().ComplexProperty(e => e.HomeAddress);
            },
            camelCase: true);

        var complexProperty = db.Model.FindEntityType(typeof(CamelHolder))!.FindComplexProperty(nameof(CamelHolder.HomeAddress))!;
        Assert.Equal("homeAddress", complexProperty.GetElementName());
        Assert.Equal("streetName", complexProperty.ComplexType.FindProperty(nameof(Addr.StreetName))!.GetElementName());
    }

    [Fact]
    public static void CamelCase_applies_to_nested_complex_property_and_its_leaves()
    {
        using var db = new TestContext<NestedHolder>(
            mb =>
            {
                mb.Entity<NestedHolder>().Property(e => e.Id).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "_id");
                mb.Entity<NestedHolder>().ComplexProperty(e => e.HomeAddress, a => a.ComplexProperty(x => x.GeoPoint));
            },
            camelCase: true);

        var outer = db.Model.FindEntityType(typeof(NestedHolder))!.FindComplexProperty(nameof(NestedHolder.HomeAddress))!;
        var inner = outer.ComplexType.FindComplexProperty(nameof(NestedAddr.GeoPoint))!;
        Assert.Equal("geoPoint", inner.GetElementName());
        Assert.Equal("latValue", inner.ComplexType.FindProperty(nameof(Geo.LatValue))!.GetElementName());
    }

    [Fact]
    public static void Without_camel_case_convention_names_default_to_clr_names()
    {
        using var db = new TestContext<PlainHolder>(mb => mb.Entity<PlainHolder>().ComplexProperty(e => e.HomeAddress));

        var complexProperty = db.Model.FindEntityType(typeof(PlainHolder))!.FindComplexProperty(nameof(PlainHolder.HomeAddress))!;
        Assert.Equal("HomeAddress", complexProperty.GetElementName());
        Assert.Equal("StreetName", complexProperty.ComplexType.FindProperty(nameof(Addr.StreetName))!.GetElementName());
    }

    [Fact]
    public static void BsonElement_on_complex_property_sets_element_name()
    {
        using var db = new TestContext<BsonElementHolder>(mb => mb.Entity<BsonElementHolder>().ComplexProperty(e => e.Address));

        var complexProperty = db.Model.FindEntityType(typeof(BsonElementHolder))!.FindComplexProperty(nameof(BsonElementHolder.Address))!;
        Assert.Equal("addr", complexProperty.GetElementName());
    }

    [Fact]
    public static void BsonElement_on_complex_property_wins_over_camel_case()
    {
        using var db = new TestContext<BsonElementCamelHolder>(
            mb =>
            {
                mb.Entity<BsonElementCamelHolder>().Property(e => e.Id).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "_id");
                mb.Entity<BsonElementCamelHolder>().ComplexProperty(e => e.Address);
            },
            camelCase: true);

        var complexProperty = db.Model.FindEntityType(typeof(BsonElementCamelHolder))!.FindComplexProperty(nameof(BsonElementCamelHolder.Address))!;
        Assert.Equal("addr", complexProperty.GetElementName());
    }

    [Fact]
    public static void Explicit_element_names_on_complex_property_and_leaf_win_over_camel_case()
    {
        using var db = new TestContext<FluentCamelHolder>(
            mb =>
            {
                mb.Entity<FluentCamelHolder>().Property(e => e.Id).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "_id");
                mb.Entity<FluentCamelHolder>().ComplexProperty(e => e.HomeAddress, a =>
                {
                    a.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "Home_Address");
                    a.Property(x => x.StreetName).Metadata.SetElementName("Street_Name");
                });
            },
            camelCase: true);

        var complexProperty = db.Model.FindEntityType(typeof(FluentCamelHolder))!.FindComplexProperty(nameof(FluentCamelHolder.HomeAddress))!;
        Assert.Equal("Home_Address", complexProperty.GetElementName());
        Assert.Equal("Street_Name", complexProperty.ComplexType.FindProperty(nameof(Addr.StreetName))!.GetElementName());
    }

    // The spellings docs/complex-types.md documents for a member inside a complex type (a fluent HasElementName does not
    // exist for complex properties or their members: CS1929 on EF8/EF9/EF10). The complex property's own spelling,
    // HasPropertyAnnotation(MongoAnnotationNames.ElementName, ...), and the leaf's Metadata.SetElementName are covered above.
    [Fact]
    public static void HasAnnotation_on_a_complex_member_sets_its_element_name()
    {
        using var db = new TestContext<MemberAnnotationHolder>(
            mb =>
            {
                mb.Entity<MemberAnnotationHolder>().Property(e => e.Id).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "_id");
                mb.Entity<MemberAnnotationHolder>().ComplexProperty(e => e.HomeAddress,
                    a => a.Property(x => x.StreetName).HasAnnotation(MongoAnnotationNames.ElementName, "st"));
            },
            camelCase: true);

        var complexProperty = db.Model.FindEntityType(typeof(MemberAnnotationHolder))!.FindComplexProperty(nameof(MemberAnnotationHolder.HomeAddress))!;
        Assert.Equal("homeAddress", complexProperty.GetElementName());
        Assert.Equal("st", complexProperty.ComplexType.FindProperty(nameof(Addr.StreetName))!.GetElementName());
    }

    [Fact]
    public static void Camel_casing_that_makes_a_complex_property_collide_with_a_scalar_fails_validation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<CamelCollisionHolder>(
                mb =>
                {
                    mb.Entity<CamelCollisionHolder>().Property(e => e.Id).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "_id");
                    mb.Entity<CamelCollisionHolder>().ComplexProperty(e => e.Home);
                },
                camelCase: true);
            _ = db.Model;
        });

        AssertMessage(ex, "Home", "home", "home", nameof(CamelCollisionHolder));
    }

    [Fact]
    public static void Column_on_complex_property_sets_element_name()
    {
        using var db = new TestContext<ColumnHolder>(mb => mb.Entity<ColumnHolder>().ComplexProperty(e => e.Address));

        var complexProperty = db.Model.FindEntityType(typeof(ColumnHolder))!.FindComplexProperty(nameof(ColumnHolder.Address))!;
        Assert.Equal("addr", complexProperty.GetElementName());
    }

    [Fact]
    public static void BsonElement_on_leaf_inside_complex_type_sets_element_name()
    {
        using var db = new TestContext<LeafAttrHolder>(mb => mb.Entity<LeafAttrHolder>().ComplexProperty(e => e.Address));

        var complexProperty = db.Model.FindEntityType(typeof(LeafAttrHolder))!.FindComplexProperty(nameof(LeafAttrHolder.Address))!;
        Assert.Equal("st", complexProperty.ComplexType.FindProperty(nameof(LeafAttrAddr.Street))!.GetElementName());
        Assert.Equal("col", complexProperty.ComplexType.FindProperty(nameof(LeafAttrAddr.Other))!.GetElementName());
    }

    [Fact]
    public static void BsonIgnore_on_complex_property_itself_is_unmapped()
    {
        using var db = new TestContext<IgnoredHolder>(mb => mb.Entity<IgnoredHolder>());

        var entityType = db.Model.FindEntityType(typeof(IgnoredHolder))!;
        Assert.Null(entityType.FindComplexProperty(nameof(IgnoredHolder.Secret)));
        Assert.NotNull(entityType.FindComplexProperty(nameof(IgnoredHolder.Visible)));
    }

    [Fact]
    public static void BsonIgnore_on_leaf_inside_complex_type_is_unmapped()
    {
        using var db = new TestContext<IgnoredLeafHolder>(mb => mb.Entity<IgnoredLeafHolder>().ComplexProperty(e => e.Address));

        var complexType = db.Model.FindEntityType(typeof(IgnoredLeafHolder))!.FindComplexProperty(nameof(IgnoredLeafHolder.Address))!.ComplexType;
        Assert.Null(complexType.FindProperty(nameof(IgnoredLeafAddr.Hidden)));
        Assert.NotNull(complexType.FindProperty(nameof(IgnoredLeafAddr.Street)));
    }

    [Fact]
    public static void BsonRequired_on_leaf_inside_complex_type_makes_it_required()
    {
        using var db = new TestContext<RequiredLeafHolder>(mb => mb.Entity<RequiredLeafHolder>().ComplexProperty(e => e.Address));

        var complexType = db.Model.FindEntityType(typeof(RequiredLeafHolder))!.FindComplexProperty(nameof(RequiredLeafHolder.Address))!.ComplexType;
        Assert.False(complexType.FindProperty(nameof(RequiredLeafAddr.Street))!.IsNullable);
    }

    [Fact]
    public static void Two_complex_properties_mapped_to_same_element_fail_validation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<TwoComplexHolder>(mb =>
            {
                var entity = mb.Entity<TwoComplexHolder>();
                entity.ComplexProperty(e => e.First).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "same");
                entity.ComplexProperty(e => e.Second).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "same");
            });
            _ = db.Model;
        });

        AssertMessage(ex, "First", "Second", "same", nameof(TwoComplexHolder));
    }

    [Fact]
    public static void Complex_property_colliding_with_scalar_element_fails_validation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<ScalarCollisionHolder>(mb =>
            {
                var entity = mb.Entity<ScalarCollisionHolder>();
                entity.Property(e => e.Name).HasElementName("clash");
                entity.ComplexProperty(e => e.Address).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "clash");
            });
            _ = db.Model;
        });

        AssertMessage(ex, "Address", "Name", "clash", nameof(ScalarCollisionHolder));
    }

    [Fact]
    public static void Complex_property_colliding_with_owned_navigation_element_fails_validation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<NavCollisionHolder>(mb =>
            {
                var entity = mb.Entity<NavCollisionHolder>();
                entity.OwnsOne(e => e.Owned).HasElementName("clash");
                entity.ComplexProperty(e => e.Address).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "clash");
            });
            _ = db.Model;
        });

        AssertMessage(ex, "Address", "Owned", "clash", nameof(NavCollisionHolder));
    }

    [Fact]
    public static void Complex_property_mapped_to_id_collides_with_key_and_fails_validation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<IdCollisionHolder>(mb =>
                mb.Entity<IdCollisionHolder>().ComplexProperty(e => e.Address).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "_id"));
            _ = db.Model;
        });

        AssertMessage(ex, "Address", "Id", "_id", nameof(IdCollisionHolder));
    }

    [Fact]
    public static void Leaf_inside_complex_type_colliding_with_sibling_fails_validation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<LeafCollisionHolder>(mb =>
            {
                var complex = mb.Entity<LeafCollisionHolder>().ComplexProperty(e => e.Address);
                complex.Property(a => a.Street).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "clash");
                complex.Property(a => a.City).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "clash");
            });
            _ = db.Model;
        });

        AssertMessage(ex, "Street", "City", "clash", nameof(Addr2));
    }

    [Fact]
    public static void Leaf_inside_nested_complex_type_colliding_with_nested_complex_property_fails_validation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<NestedCollisionHolder>(mb =>
            {
                var complex = mb.Entity<NestedCollisionHolder>().ComplexProperty(e => e.Address);
                complex.Property(a => a.Street).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "clash");
                complex.ComplexProperty(a => a.Geo).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "clash");
            });
            _ = db.Model;
        });

        AssertMessage(ex, "Street", "Geo", "clash", nameof(NestedCollisionAddr));
    }

    [Theory]
    [InlineData("$bad", "'$'")]
    [InlineData("a.b", "'.'")]
    public static void Complex_property_element_name_with_reserved_character_fails_validation(string name, string character)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new TestContext<ReservedHolder>(mb =>
                mb.Entity<ReservedHolder>().ComplexProperty(e => e.Address).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, name));
            _ = db.Model;
        });

        Assert.Contains(name, ex.Message);
        Assert.Contains("reserved character " + character, ex.Message);
    }

    [Fact]
    public static void Same_element_name_on_different_entity_types_is_allowed()
    {
        using var db = new TestContext<TwoEntityHolderA>(mb =>
        {
            mb.Entity<TwoEntityHolderA>().ComplexProperty(e => e.Address).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "same");
            mb.Entity<TwoEntityHolderB>().ComplexProperty(e => e.Address).Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "same");
        });

        Assert.NotNull(db.Model);
    }

    static void AssertMessage(InvalidOperationException ex, string first, string second, string element, string typeName)
    {
        Assert.Contains($"'{first}'", ex.Message);
        Assert.Contains($"'{second}'", ex.Message);
        Assert.Contains($"'{element}'", ex.Message);
        Assert.Contains(typeName, ex.Message);
    }

    class Addr
    {
        public string StreetName { get; set; }
    }

    class Geo
    {
        public double LatValue { get; set; }
    }

    class NestedAddr
    {
        public string StreetName { get; set; }
        public Geo GeoPoint { get; set; }
    }

    class CamelHolder { public int Id { get; set; } public Addr HomeAddress { get; set; } }
    class PlainHolder { public int Id { get; set; } public Addr HomeAddress { get; set; } }
    class CamelCollisionHolder { public int Id { get; set; } public Addr Home { get; set; } public string home { get; set; } }
    class MemberAnnotationHolder { public int Id { get; set; } public Addr HomeAddress { get; set; } }
    class FluentCamelHolder { public int Id { get; set; } public Addr HomeAddress { get; set; } }
    class NestedHolder { public int Id { get; set; } public NestedAddr HomeAddress { get; set; } }

    class BsonElementHolder { public int Id { get; set; } [BsonElement("addr")] public Addr Address { get; set; } }
    class BsonElementCamelHolder { public int Id { get; set; } [BsonElement("addr")] public Addr Address { get; set; } }
    class ColumnHolder { public int Id { get; set; } [Column("addr")] public Addr Address { get; set; } }

    class LeafAttrAddr
    {
        [BsonElement("st")] public string Street { get; set; }
        [Column("col")] public string Other { get; set; }
    }
    class LeafAttrHolder { public int Id { get; set; } public LeafAttrAddr Address { get; set; } }

    [ComplexType]
    class AutoAddr { public string Street { get; set; } }

    class IgnoredHolder
    {
        public int Id { get; set; }
        [BsonIgnore] public AutoAddr Secret { get; set; }
        public AutoAddr Visible { get; set; }
    }

    class IgnoredLeafAddr
    {
        public string Street { get; set; }
        [BsonIgnore] public string Hidden { get; set; }
    }
    class IgnoredLeafHolder { public int Id { get; set; } public IgnoredLeafAddr Address { get; set; } }

    class RequiredLeafAddr { [BsonRequired] public string Street { get; set; } }
    class RequiredLeafHolder { public int Id { get; set; } public RequiredLeafAddr Address { get; set; } }

    class TwoComplexHolder { public int Id { get; set; } public Addr First { get; set; } public Addr Second { get; set; } }
    class ScalarCollisionHolder { public int Id { get; set; } public string Name { get; set; } public Addr Address { get; set; } }
    class Owned { public string Value { get; set; } }
    class NavCollisionHolder { public int Id { get; set; } public Owned Owned { get; set; } public Addr Address { get; set; } }
    class IdCollisionHolder { public int Id { get; set; } public Addr Address { get; set; } }

    class Addr2 { public string Street { get; set; } public string City { get; set; } }
    class LeafCollisionHolder { public int Id { get; set; } public Addr2 Address { get; set; } }

    class NestedCollisionAddr { public string Street { get; set; } public Geo Geo { get; set; } }
    class NestedCollisionHolder { public int Id { get; set; } public NestedCollisionAddr Address { get; set; } }

    class ReservedHolder { public int Id { get; set; } public Addr Address { get; set; } }
    class TwoEntityHolderA { public int Id { get; set; } public Addr Address { get; set; } }
    class TwoEntityHolderB { public int Id { get; set; } public Addr Address { get; set; } }

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
