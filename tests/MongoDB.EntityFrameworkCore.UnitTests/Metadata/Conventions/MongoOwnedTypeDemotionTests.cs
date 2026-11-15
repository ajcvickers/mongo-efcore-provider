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
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.UnitTests.Metadata.Conventions;

/// <summary>
/// Tests for when a type that was reached through a navigation is embedded in its owner and when it is its own document (#65),
/// beyond the basics covered by <see cref="MongoRelationshipDiscoveryConventionTests"/>.
/// </summary>
public static class MongoOwnedTypeDemotionTests
{
    // ---- Properties that are not mapped are not part of the model, so they cannot make a type a root ----

    [Fact]
    public static void Unmapped_collection_of_a_base_class_of_the_owner_does_not_stop_the_type_being_embedded()
    {
        // Audit.Touched is [NotMapped], so Audit holds no collection of its owner's base class and is embedded in Order.
        using var db = new ModelDbContext(mb => mb.Entity<BaseOrder>());

        var audit = db.Model.FindEntityType(typeof(BaseAudit))!;
        Assert.NotNull(audit.FindOwnership());
        Assert.Equal(typeof(BaseOrder), audit.FindOwnership()!.PrincipalEntityType.ClrType);
    }

    [Fact]
    public static void Unmapped_collection_of_an_interface_of_the_owner_does_not_stop_the_type_being_embedded()
    {
        using var db = new ModelDbContext(mb => mb.Entity<InterfaceOrder>());

        Assert.NotNull(db.Model.FindEntityType(typeof(InterfaceHistory))!.FindOwnership());
    }

    [Fact]
    public static void Unmapped_collection_of_an_explicit_root_does_not_make_the_type_a_root()
    {
        // Doc.Things is [NotMapped]: nothing in Doc's document refers to a Thing, so Doc stays embedded in its holder.
        using var db = new ModelDbContext(mb =>
        {
            mb.Entity<UnmappedThing>();
            mb.Entity<UnmappedHolder>();
        });

        var doc = db.Model.FindEntityType(typeof(UnmappedDoc))!;
        Assert.NotNull(doc.FindOwnership());
        Assert.Empty(db.Model.FindEntityType(typeof(UnmappedHolder))!.GetForeignKeys());
    }

    class BaseEntity
    {
        public ObjectId _id { get; set; }
    }

    class BaseOrder : BaseEntity
    {
        public BaseAudit Audit { get; set; }
    }

    class BaseAudit
    {
        public string Who { get; set; }

        [NotMapped]
        public List<BaseEntity> Touched { get; set; }
    }

    interface IAggregate;

    class InterfaceOrder : IAggregate
    {
        public ObjectId _id { get; set; }
        public InterfaceHistory History { get; set; }
    }

    class InterfaceHistory
    {
        public string Who { get; set; }

        [NotMapped]
        public List<IAggregate> Related { get; set; }
    }

    class UnmappedHolder
    {
        public ObjectId _id { get; set; }
        public UnmappedDoc Doc { get; set; }
    }

    class UnmappedDoc
    {
        public string Name { get; set; }

        [NotMapped]
        public List<UnmappedThing> Things { get; set; }
    }

    class UnmappedThing
    {
        public ObjectId _id { get; set; }
    }

    // ---- A type that holds a collection of its would-be owner is a document of its own ----

    [Fact]
    public static void Type_that_holds_a_collection_of_its_would_be_owner_is_a_root()
    {
        // Doc.Recent is a mapped collection of Holder, which would own Doc. The collection makes Doc the principal of a
        // one-to-many relationship with Holder, so the two are related documents, not parent and embedded child.
        using var db = new ModelDbContext(mb => mb.Entity<RecentHolder>());

        var doc = db.Model.FindEntityType(typeof(RecentDoc))!;
        Assert.Null(doc.FindOwnership());
        Assert.True(doc.IsDocumentRoot());
        var holder = db.Model.FindEntityType(typeof(RecentHolder))!;
        Assert.Same(doc, Assert.Single(holder.GetForeignKeys()).PrincipalEntityType);
    }

    class RecentHolder
    {
        public ObjectId _id { get; set; }
        public RecentDoc Doc { get; set; }
    }

    class RecentDoc
    {
        public ObjectId _id { get; set; }
        public List<RecentHolder> Recent { get; set; }
    }

    // ---- A navigation that is not mapped is not a reference ----

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static void Unmapped_navigation_to_an_embedded_type_is_not_an_outside_reference(bool ownerFirst)
    {
        // Referrer.Link is [NotMapped], so nothing outside Owner's document refers to Link even though Link refers to Referrer.
        using var db = new ModelDbContext(mb =>
        {
            if (ownerFirst)
            {
                mb.Entity<HiddenOwner>();
                mb.Entity<HiddenReferrer>();
            }
            else
            {
                mb.Entity<HiddenReferrer>();
                mb.Entity<HiddenOwner>();
            }
        });

        Assert.NotNull(db.Model.FindEntityType(typeof(HiddenLink))!.FindOwnership());
    }

    class HiddenOwner
    {
        public ObjectId _id { get; set; }
        public HiddenLink Link { get; set; }
    }

    class HiddenLink
    {
        public string Name { get; set; }
        public HiddenReferrer Back { get; set; }
    }

    class HiddenReferrer
    {
        public ObjectId _id { get; set; }

        [NotMapped]
        public HiddenLink Link { get; set; }
    }

    // ---- Explicit configuration is never undone ----

    [Fact]
    public static void Explicit_OwnsOne_is_not_undone_by_an_outside_reference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new ModelDbContext(mb =>
            {
                mb.Entity<ExplicitHolder>().OwnsOne(h => h.Game);
                mb.Entity<ExplicitLevel>();
            });
            _ = db.Model;
        });

        Assert.Contains("Unable to determine the relationship", exception.Message);
    }

    [Fact]
    public static void Owned_attribute_is_not_undone_by_an_outside_reference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new ModelDbContext(mb =>
            {
                mb.Entity<AttributeHolder>();
                mb.Entity<AttributeLevel>();
            });
            _ = db.Model;
        });

        Assert.Contains("Unable to determine the relationship", exception.Message);
    }

    [Fact]
    public static void Owned_type_configured_with_Owned_of_T_is_not_undone_by_an_outside_reference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new ModelDbContext(mb =>
            {
                mb.Owned<ConfiguredGame>();
                mb.Entity<ConfiguredHolder>();
                mb.Entity<ConfiguredLevel>();
            });
            _ = db.Model;
        });

        Assert.Contains("Unable to determine the relationship", exception.Message);
    }

    class ExplicitHolder
    {
        public ObjectId _id { get; set; }
        public ExplicitGame Game { get; set; }
    }

    class ExplicitGame
    {
        public ObjectId _id { get; set; }
        public List<ExplicitLevel> Levels { get; set; }
    }

    class ExplicitLevel
    {
        public ObjectId _id { get; set; }
        public ExplicitGame Game { get; set; }
    }

    class AttributeHolder
    {
        public ObjectId _id { get; set; }
        public AttributeGame Game { get; set; }
    }

    [Microsoft.EntityFrameworkCore.Owned]
    class AttributeGame
    {
        public ObjectId _id { get; set; }
        public List<AttributeLevel> Levels { get; set; }
    }

    class AttributeLevel
    {
        public ObjectId _id { get; set; }
        public AttributeGame Game { get; set; }
    }

    class ConfiguredHolder
    {
        public ObjectId _id { get; set; }
        public ConfiguredGame Game { get; set; }
    }

    class ConfiguredGame
    {
        public ObjectId _id { get; set; }
        public List<ConfiguredLevel> Levels { get; set; }
    }

    class ConfiguredLevel
    {
        public ObjectId _id { get; set; }
        public ConfiguredGame Game { get; set; }
    }

    // ---- Types that derive from a type that stops being embedded follow it ----

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static void Derived_type_of_a_type_that_stops_being_embedded_joins_its_hierarchy(bool separateHolders)
    {
        using var db = new ModelDbContext(mb =>
        {
            if (separateHolders)
            {
                mb.Entity<SpecialHolder>();
                mb.Entity<GameHolder>();
            }
            else
            {
                mb.Entity<BothHolder>();
            }

            mb.Entity<GameLevel>();
        });

        var game = db.Model.FindEntityType(typeof(PlayedGame))!;
        var special = db.Model.FindEntityType(typeof(SpecialGame))!;
        Assert.Null(game.FindOwnership());
        Assert.Null(special.FindOwnership());
        Assert.Same(game, special.BaseType);
    }

    class BothHolder
    {
        public ObjectId _id { get; set; }
        public PlayedGame Game { get; set; }
        public SpecialGame Special { get; set; }
    }

    class GameHolder
    {
        public ObjectId _id { get; set; }
        public PlayedGame Game { get; set; }
    }

    class SpecialHolder
    {
        public ObjectId _id { get; set; }
        public SpecialGame Special { get; set; }
    }

    class PlayedGame
    {
        public ObjectId _id { get; set; }
        public List<GameLevel> Levels { get; set; }
    }

    class SpecialGame : PlayedGame
    {
        public string Extra { get; set; }
    }

    class GameLevel
    {
        public ObjectId _id { get; set; }
        public PlayedGame Game { get; set; }
    }

    // ---- A type that stops being embedded must not keep the key it was given as an embedded type ----

    [Fact]
    public static void Type_that_stops_being_embedded_uses_its_own_key_property()
    {
        using var db = new ModelDbContext(mb =>
        {
            mb.Entity<KeyedHolder>();
            mb.Entity<KeyedLevel>();
        });

        var game = db.Model.FindEntityType(typeof(KeyedGame))!;
        Assert.Null(game.FindOwnership());
        var key = Assert.Single(game.GetKeys());
        var property = Assert.Single(key.Properties);
        Assert.Equal(nameof(KeyedGame.Id), property.Name);
        Assert.False(property.IsShadowProperty());
    }

    [Fact]
    public static void Type_that_stops_being_embedded_and_has_no_key_is_rejected_with_a_missing_key_error()
    {
        // Game was embedded in a collection, which gave it a shadow key made of its owner's key and an ordinal. That key means
        // nothing for a document of its own, so the model must be rejected instead of silently keeping it.
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var db = new ModelDbContext(mb =>
            {
                mb.Entity<KeylessHolder>();
                mb.Entity<KeylessLevel>();
            });
            _ = db.Model;
        });

        Assert.Contains("primary key", exception.Message);
        Assert.Contains(nameof(KeylessGame), exception.Message);
    }

    class KeyedHolder
    {
        public ObjectId _id { get; set; }
        public List<KeyedGame> Games { get; set; }
    }

    class KeyedGame
    {
        public int Id { get; set; }
        public List<KeyedLevel> Levels { get; set; }
    }

    class KeyedLevel
    {
        public ObjectId _id { get; set; }
        public KeyedGame Game { get; set; }
    }

    class KeylessHolder
    {
        public ObjectId _id { get; set; }
        public List<KeylessGame> Games { get; set; }
    }

    class KeylessGame
    {
        public string Name { get; set; }
        public List<KeylessLevel> Levels { get; set; }
    }

    class KeylessLevel
    {
        public ObjectId _id { get; set; }
        public KeylessGame Game { get; set; }
    }

    // ---- The result does not depend on the order in which types are registered ----

    [Fact]
    public static void Derived_type_joins_the_hierarchy_when_its_root_is_registered_after_the_holder()
    {
        using var db = new ModelDbContext(mb =>
        {
            mb.Entity<OrderHolder>();
            mb.Entity<OrderBlog>();
        });

        var featured = db.Model.FindEntityType(typeof(OrderFeaturedBlog))!;
        Assert.Null(featured.FindOwnership());
        Assert.Same(db.Model.FindEntityType(typeof(OrderBlog)), featured.BaseType);
    }

    [Fact]
    public static void Derived_type_joins_the_hierarchy_when_its_root_is_registered_before_the_holder()
    {
        using var db = new ModelDbContext(mb =>
        {
            mb.Entity<OrderBlog>();
            mb.Entity<OrderHolder>();
        });

        var featured = db.Model.FindEntityType(typeof(OrderFeaturedBlog))!;
        Assert.Null(featured.FindOwnership());
        Assert.Same(db.Model.FindEntityType(typeof(OrderBlog)), featured.BaseType);
    }

    class OrderBlog
    {
        public ObjectId _id { get; set; }
        public string Title { get; set; }
    }

    class OrderFeaturedBlog : OrderBlog
    {
        public string Extra { get; set; }
    }

    class OrderHolder
    {
        public ObjectId _id { get; set; }
        public OrderFeaturedBlog Featured { get; set; }
    }

    sealed class ModelDbContext(Action<ModelBuilder> configure) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, NoModelCache>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => configure(modelBuilder);
    }

    sealed class NoModelCache : Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime) => new object();
    }
}
