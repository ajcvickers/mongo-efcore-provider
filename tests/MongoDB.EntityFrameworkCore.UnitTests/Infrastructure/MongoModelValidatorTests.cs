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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.UnitTests.Infrastructure;

public static class MongoModelValidatorTests
{
    class EntityWithTwoUnderscoreIds
    {
        [Column("_id")]
        public int key1 { get; set; }

        [Column("_id")]
        public string key2 { get; set; }
    }

    class DoubleNamedEntity
    {
        public int _id { get; set; }

        public string name1 { get; set; }
        public string name2 { get; set; }
    }

    class ConfiguredIdNamedEntity
    {
        public string ThisWillBePrimaryKey { get; set; }
        public string SomethingElse { get; set; }
    }

    class WithTwoOwnedEntities
    {
        public int _id { get; set; }
        public Location First { get; set; }
        public Location Second { get; set; }
        public string Different { get; set; }
    }

    class Location
    {
        public Decimal Longitude { get; set; }
        public Decimal Latitude { get; set; }
    }

    [Fact]
    public static void Validate_throws_when_multiple_properties_attributed_to_same_element_name()
    {
        using var db = SingleEntityDbContext.Create<EntityWithTwoUnderscoreIds>();

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(EntityWithTwoUnderscoreIds)}'", ex.Message);
        Assert.Contains("'_id'", ex.Message);
        Assert.Contains($"'{nameof(EntityWithTwoUnderscoreIds.key1)}'", ex.Message);
        Assert.Contains($"'{nameof(EntityWithTwoUnderscoreIds.key2)}'", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_multiple_properties_configured_to_same_element_name()
    {
        using var db = SingleEntityDbContext.Create<DoubleNamedEntity>(mb =>
        {
            var dneBuilder = mb.Entity<DoubleNamedEntity>();
            dneBuilder.Property(p => p.name1).HasElementName("name");
            dneBuilder.Property(p => p.name2).HasElementName("name");
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(DoubleNamedEntity)}'", ex.Message);
        Assert.Contains("'name'", ex.Message);
        Assert.Contains($"'{nameof(DoubleNamedEntity.name1)}'", ex.Message);
        Assert.Contains($"'{nameof(DoubleNamedEntity.name2)}'", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_property_element_name_starts_with_dollar_sign()
    {
        using var db = SingleEntityDbContext.Create<DoubleNamedEntity>(mb =>
        {
            var dneBuilder = mb.Entity<DoubleNamedEntity>();
            dneBuilder.Property(p => p.name1).HasElementName("$something");
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(DoubleNamedEntity)}'", ex.Message);
        Assert.Contains($"'{nameof(DoubleNamedEntity.name1)}'", ex.Message);
        Assert.Contains("'$something'", ex.Message);
    }

    [Fact]
    public static void Validate_succeeds_if_property_element_name_ends_with_dollar_sign()
    {
        using var db = SingleEntityDbContext.Create<DoubleNamedEntity>(mb =>
        {
            var dneBuilder = mb.Entity<DoubleNamedEntity>();
            dneBuilder.Property(p => p.name1).HasElementName("something$");
        });

        Assert.NotNull(db.Model);
    }

    [Fact]
    public static void Validate_succeeds_if_property_element_name_contains_dollar_sign_not_at_the_start()
    {
        using var db = SingleEntityDbContext.Create<DoubleNamedEntity>(mb =>
        {
            var dneBuilder = mb.Entity<DoubleNamedEntity>();
            dneBuilder.Property(p => p.name1).HasElementName("some$thing");
        });

        Assert.NotNull(db.Model);
    }

    [Fact]
    public static void Validate_throws_when_property_element_name_starts_with_dot()
    {
        using var db = SingleEntityDbContext.Create<DoubleNamedEntity>(mb =>
        {
            var dneBuilder = mb.Entity<DoubleNamedEntity>();
            dneBuilder.Property(p => p.name1).HasElementName(".something");
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(DoubleNamedEntity)}'", ex.Message);
        Assert.Contains($"'{nameof(DoubleNamedEntity.name1)}'", ex.Message);
        Assert.Contains("'.something'", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_property_element_name_ends_with_dot()
    {
        using var db = SingleEntityDbContext.Create<DoubleNamedEntity>(mb =>
        {
            var dneBuilder = mb.Entity<DoubleNamedEntity>();
            dneBuilder.Property(p => p.name1).HasElementName("something.");
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(DoubleNamedEntity)}'", ex.Message);
        Assert.Contains($"'{nameof(DoubleNamedEntity.name1)}'", ex.Message);
        Assert.Contains("'something.'", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_property_element_name_contains_dot()
    {
        using var db = SingleEntityDbContext.Create<DoubleNamedEntity>(mb =>
        {
            var dneBuilder = mb.Entity<DoubleNamedEntity>();
            dneBuilder.Property(p => p.name1).HasElementName("some.thing");
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(DoubleNamedEntity)}'", ex.Message);
        Assert.Contains($"'{nameof(DoubleNamedEntity.name1)}'", ex.Message);
        Assert.Contains("'some.thing'", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_multiple_navigations_configured_to_same_element_name()
    {
        using var db = SingleEntityDbContext.Create<WithTwoOwnedEntities>(mb =>
        {
            var dneBuilder = mb.Entity<WithTwoOwnedEntities>();
            dneBuilder.OwnsOne(p => p.First, r => r.HasElementName("location"));
            dneBuilder.OwnsOne(p => p.Second, r => r.HasElementName("location"));
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities)}'", ex.Message);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities.First)}'", ex.Message);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities.Second)}'", ex.Message);
        Assert.Contains("'location'", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_navigation_element_name_starts_with_dollar_sign()
    {
        using var db = SingleEntityDbContext.Create<WithTwoOwnedEntities>(mb =>
        {
            var dneBuilder = mb.Entity<WithTwoOwnedEntities>();
            dneBuilder.OwnsOne(p => p.First).HasElementName("$something");
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities)}'", ex.Message);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities.First)}'", ex.Message);
        Assert.Contains("'$something'", ex.Message);
    }

    [Fact]
    public static void Validate_succeeds_if_navigation_element_name_ends_with_dollar_sign()
    {
        using var db = SingleEntityDbContext.Create<WithTwoOwnedEntities>(mb =>
        {
            var dneBuilder = mb.Entity<WithTwoOwnedEntities>();
            dneBuilder.OwnsOne(p => p.First).HasElementName("something$");
        });

        Assert.NotNull(db.Model);
    }

    [Fact]
    public static void Validate_succeeds_if_navigation_element_name_contains_dollar_sign_not_at_the_start()
    {
        using var db = SingleEntityDbContext.Create<WithTwoOwnedEntities>(mb =>
        {
            var dneBuilder = mb.Entity<WithTwoOwnedEntities>();
            dneBuilder.OwnsOne(p => p.First).HasElementName("some$thing");
        });

        Assert.NotNull(db.Model);
    }

    [Fact]
    public static void Validate_throws_when_navigation_element_name_starts_with_dot()
    {
        using var db = SingleEntityDbContext.Create<WithTwoOwnedEntities>(mb =>
        {
            var dneBuilder = mb.Entity<WithTwoOwnedEntities>();
            dneBuilder.OwnsOne(p => p.First).HasElementName(".why");
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities)}'", ex.Message);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities.First)}'", ex.Message);
        Assert.Contains("'.why'", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_navigation_element_name_ends_with_dot()
    {
        using var db = SingleEntityDbContext.Create<WithTwoOwnedEntities>(mb =>
        {
            var dneBuilder = mb.Entity<WithTwoOwnedEntities>();
            dneBuilder.OwnsOne(p => p.First).HasElementName("notokay.");
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities)}'", ex.Message);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities.First)}'", ex.Message);
        Assert.Contains("'notokay.'", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_navigation_element_name_contains_dot()
    {
        using var db = SingleEntityDbContext.Create<WithTwoOwnedEntities>(mb =>
        {
            var dneBuilder = mb.Entity<WithTwoOwnedEntities>();
            dneBuilder.OwnsOne(p => p.First).HasElementName("one.dot.is.too.many");
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities)}'", ex.Message);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities.First)}'", ex.Message);
        Assert.Contains("'one.dot.is.too.many'", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_navigation_and_property_configured_to_same_element_name()
    {
        using var db = SingleEntityDbContext.Create<WithTwoOwnedEntities>(mb =>
        {
            var dneBuilder = mb.Entity<WithTwoOwnedEntities>();
            dneBuilder.OwnsOne(p => p.First, r => r.HasElementName("someTarget"));
            dneBuilder.Property(p => p.Different).HasElementName("someTarget");
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities)}'", ex.Message);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities.First)}'", ex.Message);
        Assert.Contains($"'{nameof(WithTwoOwnedEntities.Different)}'", ex.Message);
        Assert.Contains("'someTarget'", ex.Message);
    }

    [Fact]
    public static void Validate_succeeds_when_primary_key_configured_correctly()
    {
        using var db = SingleEntityDbContext.Create<ConfiguredIdNamedEntity>(mb =>
        {
            var dneBuilder = mb.Entity<ConfiguredIdNamedEntity>();
            dneBuilder.HasKey(e => e.ThisWillBePrimaryKey);
            dneBuilder.Property(p => p.ThisWillBePrimaryKey).HasElementName("_id");
        });

        Assert.NotNull(db.Model);
    }

    [Fact]
    public static void Validate_throws_when_primary_key_conflicts_with_different_id_mapped_property()
    {
        using var db = SingleEntityDbContext.Create<ConfiguredIdNamedEntity>(mb =>
        {
            var dneBuilder = mb.Entity<ConfiguredIdNamedEntity>();
            dneBuilder.HasKey(e => e.ThisWillBePrimaryKey);
            dneBuilder.Property(p => p.SomethingElse).HasElementName("_id");
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(ConfiguredIdNamedEntity)}'", ex.Message);
        Assert.Contains($"'{nameof(ConfiguredIdNamedEntity.ThisWillBePrimaryKey)}'", ex.Message);
        Assert.Contains("'_id'", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_entity_constructor_has_BsonConstructor_attribute()
    {
        using var context = SingleEntityDbContext.Create<EntityWithBsonConstructor>();

        var ex = Assert.Throws<NotSupportedException>(() => context.GetProperty((EntityWithBsonConstructor e) => e.name));
        Assert.Contains($"'{nameof(EntityWithBsonConstructor)}'", ex.Message);
        Assert.Contains($"'{nameof(BsonConstructorAttribute)}'", ex.Message);
    }

    class EntityWithBsonConstructor
    {
        public int _id { get; set; }

        public string name { get; set; }

        [BsonConstructor]
        public EntityWithBsonConstructor(string name)
        {
            this.name = name;
        }
    }

    [Fact]
    public static void Validate_throws_when_entity_class_has_BsonDiscriminator_attribute()
    {
        using var context = SingleEntityDbContext.Create<EntityWithBsonDiscriminator>();

        var ex = Assert.Throws<NotSupportedException>(() => context.GetProperty((EntityWithBsonDiscriminator e) => e.name));
        Assert.Contains($"'{nameof(EntityWithBsonDiscriminator)}'", ex.Message);
        Assert.Contains($"'{nameof(BsonDiscriminatorAttribute)}'", ex.Message);
    }

    [BsonDiscriminator]
    class EntityWithBsonDiscriminator
    {
        public int _id { get; set; }

        public string name { get; set; }
    }

    [Fact]
    public static void Validate_throws_when_entity_class_has_BsonKnownTypes_attribute()
    {
        using var context = SingleEntityDbContext.Create<EntityWithBsonKnownTypes>();

        var ex = Assert.Throws<NotSupportedException>(() => context.GetProperty((EntityWithBsonKnownTypes e) => e.name));
        Assert.Contains($"'{nameof(EntityWithBsonKnownTypes)}'", ex.Message);
        Assert.Contains($"'{nameof(BsonKnownTypesAttribute)}'", ex.Message);
    }

    [BsonKnownTypes]
    class EntityWithBsonKnownTypes
    {
        public int _id { get; set; }

        public string name { get; set; }
    }

    [Fact]
    public static void Validate_throws_when_entity_class_has_BsonMemberMapAttributeUsage_attribute()
    {
        using var context = SingleEntityDbContext.Create<EntityWithBsonMemberMapAttributeUsage>();

        var ex = Assert.Throws<NotSupportedException>(
            () => context.GetProperty((EntityWithBsonMemberMapAttributeUsage e) => e.name));
        Assert.Contains($"'{nameof(EntityWithBsonMemberMapAttributeUsage)}'", ex.Message);
        Assert.Contains($"'{nameof(BsonMemberMapAttributeUsageAttribute)}'", ex.Message);
    }

    [BsonMemberMapAttributeUsage]
    class EntityWithBsonMemberMapAttributeUsage
    {
        public int _id { get; set; }

        public string name { get; set; }
    }

    [Fact]
    public static void Validate_throws_when_entity_class_has_EntityWithBsonNoId_attribute()
    {
        using var context = SingleEntityDbContext.Create<EntityWithBsonNoId>();

        var ex = Assert.Throws<NotSupportedException>(() => context.GetProperty((EntityWithBsonNoId e) => e.name));
        Assert.Contains($"'{nameof(EntityWithBsonNoId)}'", ex.Message);
        Assert.Contains($"'{nameof(BsonNoIdAttribute)}'", ex.Message);
    }

    [BsonNoId]
    class EntityWithBsonNoId
    {
        public int _id { get; set; }

        public string name { get; set; }
    }

    [Fact]
    public static void Validate_throws_when_entity_class_has_EntityWithBsonSerializer_attribute()
    {
        using var context = SingleEntityDbContext.Create<EntityWithBsonSerializer>();

        var ex = Assert.Throws<NotSupportedException>(() => context.GetProperty((EntityWithBsonSerializer e) => e.name));
        Assert.Contains($"'{nameof(EntityWithBsonSerializer)}'", ex.Message);
        Assert.Contains($"'{nameof(BsonSerializerAttribute)}'", ex.Message);
    }

    [BsonSerializer]
    class EntityWithBsonSerializer
    {
        public int _id { get; set; }

        public string name { get; set; }
    }

    [Fact]
    public static void Validate_throws_when_entity_method_has_BsonFactoryMethod_attribute()
    {
        using var context = SingleEntityDbContext.Create<EntityWithBsonFactoryMethod>();

        var ex = Assert.Throws<NotSupportedException>(() => context.GetProperty((EntityWithBsonFactoryMethod e) => e.name));
        Assert.Contains($"'{nameof(EntityWithBsonFactoryMethod)}.{nameof(EntityWithBsonFactoryMethod.Create)}'", ex.Message);
        Assert.Contains($"'{nameof(BsonFactoryMethodAttribute)}'", ex.Message);
    }

    class EntityWithBsonFactoryMethod
    {
        public int _id { get; set; }

        public string name { get; set; }

        [BsonFactoryMethod]
        public static EntityWithBsonFactoryMethod Create() => new();
    }

    [Fact]
    public static void Validate_throws_when_entity_has_multiple_timestamp_attributes()
    {
        using var context = SingleEntityDbContext.Create<DoubleTimestampedEntity>();

        var ex = Assert.Throws<NotSupportedException>(() => context.GetProperty((DoubleTimestampedEntity e) => e.name));
        Assert.Contains($"{nameof(DoubleTimestampedEntity.Version1)}", ex.Message);
        Assert.Contains($"{nameof(DoubleTimestampedEntity.Version2)}", ex.Message);
    }

    class DoubleTimestampedEntity
    {
        public int _id { get; set; }

        public string name { get; set; }

        [Timestamp] public int Version1 { get; set; }
        [Timestamp] public int Version2 { get; set; }
    }

    [Fact]
    public static void Validate_throws_when_entity_has_multiple_IsRowVersion_configurations()
    {
        using var context = SingleEntityDbContext.Create<VersionableEntity>(mb =>
            mb.Entity<VersionableEntity>(e =>
                {
                    e.Property(v => v.VersionA).IsRowVersion();
                    e.Property(v => v.VersionB).IsRowVersion();
                }));

        var ex = Assert.Throws<NotSupportedException>(() => context.GetProperty((VersionableEntity e) => e.name));
        Assert.Contains($"{nameof(VersionableEntity.VersionA)}", ex.Message);
        Assert.Contains($"{nameof(VersionableEntity.VersionB)}", ex.Message);
    }

    class OwnerWithConcurrencyCheckedOwned
    {
        public int _id { get; set; }
        public ConcurrencyTokenOwned Owned { get; set; }
    }

    class ConcurrencyTokenOwned
    {
        [ConcurrencyCheck] public int Version { get; set; }
        public string Text { get; set; }
    }

    class OwnerWithPlainOwned
    {
        public int _id { get; set; }
        public int RootVersion { get; set; }
        public PlainOwned Owned { get; set; }
    }

    class PlainOwned
    {
        public int Version { get; set; }
        public string Text { get; set; }
    }

    [Fact]
    public static void Validate_throws_when_owned_entity_has_concurrency_token_attribute()
    {
        using var db = SingleEntityDbContext.Create<OwnerWithConcurrencyCheckedOwned>();

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains($"'{nameof(ConcurrencyTokenOwned.Version)}'", ex.Message);
        Assert.Contains("concurrency token", ex.Message);
        Assert.Contains("owned entity", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_owned_entity_has_fluent_concurrency_token()
    {
        using var db = SingleEntityDbContext.Create<OwnerWithPlainOwned>(mb =>
            mb.Entity<OwnerWithPlainOwned>()
                .OwnsOne(e => e.Owned, b => b.Property(p => p.Version).IsConcurrencyToken()));

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains($"'{nameof(PlainOwned.Version)}'", ex.Message);
        Assert.Contains("concurrency token", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_owned_entity_has_row_version()
    {
        using var db = SingleEntityDbContext.Create<OwnerWithPlainOwned>(mb =>
            mb.Entity<OwnerWithPlainOwned>()
                .OwnsOne(e => e.Owned, b => b.Property(p => p.Version).IsRowVersion()));

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains($"'{nameof(PlainOwned.Version)}'", ex.Message);
    }

    [Fact]
    public static void Validate_succeeds_when_root_entity_has_concurrency_token()
    {
        using var db = SingleEntityDbContext.Create<OwnerWithPlainOwned>(mb =>
            mb.Entity<OwnerWithPlainOwned>(e =>
            {
                e.Property(p => p.RootVersion).IsConcurrencyToken();
                e.OwnsOne(o => o.Owned);
            }));

        Assert.NotNull(db.Model);
    }

    class ByteArrayVersionEntity
    {
        public int _id { get; set; }
        public byte[] Version { get; set; }
    }

    class TimestampedByteArrayEntity
    {
        public int _id { get; set; }
        [Timestamp] public byte[] Version { get; set; }
    }

    class TwoByteArrayVersionsEntity
    {
        public int _id { get; set; }
        public byte[] VersionA { get; set; }
        public byte[] VersionB { get; set; }
    }

    class NullableVersionEntity
    {
        public int _id { get; set; }
        public int? Version { get; set; }
    }

    class GuidVersionEntity
    {
        public int _id { get; set; }
        public Guid Version { get; set; }
    }

    [Fact]
    public static void Validate_succeeds_when_byte_array_is_configured_as_row_version()
    {
        using var db = SingleEntityDbContext.Create<ByteArrayVersionEntity>(mb =>
            mb.Entity<ByteArrayVersionEntity>().Property(e => e.Version).IsRowVersion());

        Assert.NotNull(db.Model);
    }

    [Fact]
    public static void Validate_succeeds_when_byte_array_has_timestamp_attribute()
    {
        using var db = SingleEntityDbContext.Create<TimestampedByteArrayEntity>();

        Assert.NotNull(db.Model);
    }

    [Fact]
    public static void Validate_throws_when_two_byte_array_row_versions_are_configured()
    {
        using var db = SingleEntityDbContext.Create<TwoByteArrayVersionsEntity>(mb =>
        {
            mb.Entity<TwoByteArrayVersionsEntity>().Property(e => e.VersionA).IsRowVersion();
            mb.Entity<TwoByteArrayVersionsEntity>().Property(e => e.VersionB).IsRowVersion();
        });

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains(nameof(TwoByteArrayVersionsEntity.VersionA), ex.Message);
        Assert.Contains(nameof(TwoByteArrayVersionsEntity.VersionB), ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_nullable_int_is_configured_as_row_version()
    {
        using var db = SingleEntityDbContext.Create<NullableVersionEntity>(mb =>
            mb.Entity<NullableVersionEntity>().Property(e => e.Version).IsRowVersion());

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains($"'{nameof(NullableVersionEntity.Version)}'", ex.Message);
    }

    class NullableLongVersionEntity { public int _id { get; set; } public long? Version { get; set; } }
    class NullableUIntVersionEntity { public int _id { get; set; } public uint? Version { get; set; } }
    class OwnerWithGuidVersionOwned { public int _id { get; set; } public GuidVersionOwned Owned { get; set; } }
    class GuidVersionOwned { public Guid Version { get; set; } public string Text { get; set; } }

    [Fact]
    public static void Validate_throws_when_nullable_long_is_configured_as_row_version()
    {
        using var db = SingleEntityDbContext.Create<NullableLongVersionEntity>(mb =>
            mb.Entity<NullableLongVersionEntity>().Property(e => e.Version).IsRowVersion());

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains($"'{nameof(NullableLongVersionEntity.Version)}'", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_nullable_uint_is_configured_as_row_version()
    {
        using var db = SingleEntityDbContext.Create<NullableUIntVersionEntity>(mb =>
            mb.Entity<NullableUIntVersionEntity>().Property(e => e.Version).IsRowVersion());

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains($"'{nameof(NullableUIntVersionEntity.Version)}'", ex.Message);
    }

    [Fact]
    public static void Validate_reports_the_row_version_type_before_the_owned_entity_for_an_unsupported_row_version_on_an_owned_entity()
    {
        using var db = SingleEntityDbContext.Create<OwnerWithGuidVersionOwned>(mb =>
            mb.Entity<OwnerWithGuidVersionOwned>()
                .OwnsOne(e => e.Owned, b => b.Property(p => p.Version).IsRowVersion()));

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains($"'{nameof(GuidVersionOwned.Version)}'", ex.Message);
        Assert.Contains("row version", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_a_user_maintained_date_time_token_is_generated_on_add_or_update()
    {
        // Rejected because EF cannot tell it from a row version; the message tells the user to drop ValueGeneratedOnAddOrUpdate.
        using var db = SingleEntityDbContext.Create<DateTimeTokenEntity>(mb =>
            mb.Entity<DateTimeTokenEntity>().Property(e => e.Stamp).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate());

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains($"'{nameof(DateTimeTokenEntity.Stamp)}'", ex.Message);
        Assert.Contains("ValueGeneratedOnAddOrUpdate", ex.Message);
    }

    [Fact]
    public static void Validate_succeeds_when_a_user_maintained_date_time_token_is_a_plain_concurrency_token()
    {
        using var db = SingleEntityDbContext.Create<DateTimeTokenEntity>(mb =>
            mb.Entity<DateTimeTokenEntity>().Property(e => e.Stamp).IsConcurrencyToken());

        Assert.NotNull(db.Model);
    }

    class DateTimeTokenEntity { public int _id { get; set; } public DateTime Stamp { get; set; } }

    [Fact]
    public static void Validate_throws_when_guid_is_configured_as_row_version()
    {
        using var db = SingleEntityDbContext.Create<GuidVersionEntity>(mb =>
            mb.Entity<GuidVersionEntity>().Property(e => e.Version).IsRowVersion());

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains($"'{nameof(GuidVersionEntity.Version)}'", ex.Message);
    }

    class OwnerWithByteArrayVersionOwned
    {
        public int _id { get; set; }
        public ByteArrayVersionOwned Owned { get; set; }
    }

    class ByteArrayVersionOwned
    {
        public byte[] Version { get; set; }
        public string Text { get; set; }
    }

    class VersionedBase
    {
        public int _id { get; set; }
    }

    class VersionedDerived : VersionedBase
    {
        public byte[] Version { get; set; }
    }

    enum VersionEnum { One }

    class EnumVersionEntity
    {
        public int _id { get; set; }
        public VersionEnum Version { get; set; }
    }

    class UIntVersionEntity { public int _id { get; set; } public uint Version { get; set; } }
    class LongVersionEntity { public int _id { get; set; } public long Version { get; set; } }
    class ULongVersionEntity { public int _id { get; set; } public ulong Version { get; set; } }
    class StringVersionEntity { public int _id { get; set; } public string Version { get; set; } }

    [Fact]
    public static void Validate_throws_when_byte_array_row_version_is_on_an_owned_entity()
    {
        using var db = SingleEntityDbContext.Create<OwnerWithByteArrayVersionOwned>(mb =>
            mb.Entity<OwnerWithByteArrayVersionOwned>()
                .OwnsOne(e => e.Owned, b => b.Property(p => p.Version).IsRowVersion()));

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains($"'{nameof(ByteArrayVersionOwned.Version)}'", ex.Message);
        Assert.Contains("owned entity", ex.Message);
    }

    [Fact]
    public static void Validate_succeeds_when_byte_array_row_version_is_on_a_derived_type()
    {
        using var db = SingleEntityDbContext.Create<VersionedBase>(mb =>
        {
            mb.Entity<VersionedBase>();
            mb.Entity<VersionedDerived>().Property(e => e.Version).IsRowVersion();
        });

        Assert.NotNull(db.Model);
    }

    [Fact]
    public static void Validate_throws_when_enum_is_configured_as_row_version()
    {
        using var db = SingleEntityDbContext.Create<EnumVersionEntity>(mb =>
            mb.Entity<EnumVersionEntity>().Property(e => e.Version).IsRowVersion());

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains($"'{nameof(EnumVersionEntity.Version)}'", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_a_concurrency_token_is_generated_on_add_or_update_with_an_unsupported_type()
    {
        using var db = SingleEntityDbContext.Create<StringVersionEntity>(mb =>
            mb.Entity<StringVersionEntity>().Property(e => e.Version).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate());

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains($"'{nameof(StringVersionEntity.Version)}'", ex.Message);
        Assert.Contains("ValueGeneratedOnAddOrUpdate", ex.Message);
        Assert.Contains("byte[]", ex.Message);
    }

    [Fact]
    public static void Validate_succeeds_when_a_non_concurrency_property_is_generated_on_add_or_update()
    {
        using var db = SingleEntityDbContext.Create<StringVersionEntity>(mb =>
            mb.Entity<StringVersionEntity>().Property(e => e.Version).ValueGeneratedOnAddOrUpdate());

        Assert.NotNull(db.Model);
    }

    [Fact]
    public static void Validate_succeeds_when_unsigned_and_long_row_versions_are_supported_types()
    {
        using (var db = SingleEntityDbContext.Create<UIntVersionEntity>(mb => mb.Entity<UIntVersionEntity>().Property(e => e.Version).IsRowVersion()))
            Assert.NotNull(db.Model);
        using (var db = SingleEntityDbContext.Create<LongVersionEntity>(mb => mb.Entity<LongVersionEntity>().Property(e => e.Version).IsRowVersion()))
            Assert.NotNull(db.Model);
        using (var db = SingleEntityDbContext.Create<ULongVersionEntity>(mb => mb.Entity<ULongVersionEntity>().Property(e => e.Version).IsRowVersion()))
            Assert.NotNull(db.Model);
    }

    [Fact]
    public static void Validate_succeeds_when_byte_array_is_a_plain_concurrency_token()
    {
        using var db = SingleEntityDbContext.Create<ByteArrayVersionEntity>(mb =>
            mb.Entity<ByteArrayVersionEntity>().Property(e => e.Version).IsConcurrencyToken());

        Assert.NotNull(db.Model);
    }

    class VersionableEntity
    {
        public int _id { get; set; }

        public string name { get; set; }

        public int VersionA { get; set; }
        public int VersionB { get; set; }
    }

    [Fact]
    public static void Validate_throws_when_entity_has_mutiple_timestamp_rowversions()
    {
        using var context = SingleEntityDbContext.Create<VersionableTimestampedEntity>(mb =>
            mb.Entity<VersionableTimestampedEntity>(e =>
            {
                e.Property(v => v.VersionB2).IsRowVersion();
            }));

        var ex = Assert.Throws<NotSupportedException>(() => context.GetProperty((VersionableTimestampedEntity e) => e.name));
        Assert.Contains($"{nameof(VersionableTimestampedEntity.VersionA1)}", ex.Message);
        Assert.Contains($"{nameof(VersionableTimestampedEntity.VersionB2)}", ex.Message);
    }

    class VersionableTimestampedEntity
    {
        public int _id { get; set; }

        public string name { get; set; }

        [Timestamp] public int VersionA1 { get; set; }
        public int VersionB2 { get; set; }
    }

    class Customer
    {
        public int _id { get; set; }
        public Address HomeAddress { get; set; }
        public Address BillingAddress { get; set; }
    }

    class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
    }

    [Fact]
    public static void Validate_throws_when_same_owned_clr_type_has_differing_property_element_names()
    {
        using var db = SingleEntityDbContext.Create<Customer>(mb =>
        {
            mb.Entity<Customer>(e =>
            {
                e.OwnsOne(c => c.HomeAddress,
                    o => o.Property(a => a.Street).HasElementName("street_name"));
                e.OwnsOne(c => c.BillingAddress,
                    o => o.Property(a => a.Street).HasElementName("road"));
            });
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(Address)}'", ex.Message);
        Assert.Contains($"'{nameof(Address.Street)}'", ex.Message);
        Assert.Contains("'street_name'", ex.Message);
        Assert.Contains("'road'", ex.Message);
    }

    [Fact]
    public static void Validate_throws_when_one_owned_navigation_has_custom_element_name_and_other_uses_default()
    {
        using var db = SingleEntityDbContext.Create<Customer>(mb =>
        {
            mb.Entity<Customer>(e =>
            {
                e.OwnsOne(c => c.HomeAddress,
                    o => o.Property(a => a.Street).HasElementName("street_name"));
                e.OwnsOne(c => c.BillingAddress);
            });
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains($"'{nameof(Address)}'", ex.Message);
        Assert.Contains($"'{nameof(Address.Street)}'", ex.Message);
        Assert.Contains("'street_name'", ex.Message);
        Assert.Contains($"'{nameof(Address.Street)}'", ex.Message); // default element name equals property name
    }

    [Fact]
    public static void Validate_succeeds_when_same_owned_clr_type_has_identical_custom_element_names()
    {
        using var db = SingleEntityDbContext.Create<Customer>(mb =>
        {
            mb.Entity<Customer>(e =>
            {
                e.OwnsOne(c => c.HomeAddress,
                    o => o.Property(a => a.Street).HasElementName("street_name"));
                e.OwnsOne(c => c.BillingAddress,
                    o => o.Property(a => a.Street).HasElementName("street_name"));
            });
        });

        Assert.NotNull(db.Model);
    }

    [Fact]
    public static void Validate_succeeds_when_same_owned_clr_type_has_default_mappings_on_both_navigations()
    {
        using var db = SingleEntityDbContext.Create<Customer>(mb =>
        {
            mb.Entity<Customer>(e =>
            {
                e.OwnsOne(c => c.HomeAddress);
                e.OwnsOne(c => c.BillingAddress);
            });
        });

        Assert.NotNull(db.Model);
    }

    class Book
    {
        public ObjectId Id { get; set; }
        public float[] Floats1 { get; set; }
        public float[] Floats2 { get; set; }
        public List<Chapter> Chapters { get; set; } = new();
    }

    class Chapter
    {
        public float[] Floats { get; set; }
    }

    [Fact]
    public static void Validate_throws_when_vector_index_has_multiple_properties()
    {
        using var context = SingleEntityDbContext.Create<Book>(mb =>
            mb.Entity<Book>().HasIndex(e => new { e.Floats1, e.Floats2 })
                .IsVectorIndex(VectorSimilarity.Cosine, 2));

        Assert.Contains(
            "A vector index on 'Book' is defined over properties 'Floats1,Floats2'.",
            Assert.Throws<InvalidOperationException>(() => context.Model).Message);
    }

    [Fact]
    public static void Validate_throws_when_vector_index_has_bad_quantization()
    {
        using (var context = SingleEntityDbContext.Create<Book>(mb => mb.Entity<Book>().HasIndex(e => new { e.Floats1 })
                   .IsVectorIndex(VectorSimilarity.Cosine, 2).HasQuantization((VectorQuantization?)-1)))
        {
            Assert.Contains(
                "Vector quantization is set to '-1' which is not a valid value from the 'VectorQuantization' enum.",
                Assert.Throws<InvalidOperationException>(() => context.Model).Message);
        }

        using (var context = SingleEntityDbContext.Create<Book>(mb => mb.Entity<Book>().HasIndex(e => new { e.Floats1 })
                   .IsVectorIndex(VectorSimilarity.Cosine, 2).HasQuantization((VectorQuantization?)4)))
        {
            Assert.Contains(
                "Vector quantization is set to '4' which is not a valid value from the 'VectorQuantization' enum.",
                Assert.Throws<InvalidOperationException>(() => context.Model).Message);
        }
    }

    [Fact]
    public static void Validate_throws_when_vector_index_has_bad_similarity()
    {
        using (var context = SingleEntityDbContext.Create<Book>(mb => mb.Entity<Book>().HasIndex(e => new { e.Floats1 })
                   .IsVectorIndex((VectorSimilarity)(-1), 2)))
        {
            Assert.Contains(
                "Vector similarity is set to '-1' which is not a valid value from the 'VectorSimilarity' enum.'",
                Assert.Throws<InvalidOperationException>(() => context.Model).Message);
        }

        using (var context = SingleEntityDbContext.Create<Book>(mb => mb.Entity<Book>().HasIndex(e => new { e.Floats1 })
                   .IsVectorIndex((VectorSimilarity)3, 2)))
        {
            Assert.Contains(
                "Vector similarity is set to '3' which is not a valid value from the 'VectorSimilarity' enum.'",
                Assert.Throws<InvalidOperationException>(() => context.Model).Message);
        }
    }

    [Fact]
    public static void Validate_throws_when_vector_index_is_on_nested_collection()
    {
        using var context = SingleEntityDbContext.Create<Book>(mb =>
            mb.Entity<Book>(b =>
            {
                b.OwnsMany(x => x.Chapters,
                    b => { b.HasIndex(e => e.Floats).IsVectorIndex(VectorSimilarity.Cosine, 2); });
            }));

        Assert.Contains(
            "The entity type 'Chapter' cannot have a vector index because it is part of an nested collection.",
            Assert.Throws<InvalidOperationException>(() => context.Model).Message);
    }

}
