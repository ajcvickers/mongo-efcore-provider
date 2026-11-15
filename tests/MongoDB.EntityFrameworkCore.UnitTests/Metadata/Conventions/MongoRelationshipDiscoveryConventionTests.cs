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
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.UnitTests.Metadata.Conventions;

public static class MongoRelationshipDiscoveryConventionTests
{
    [Fact]
    public static void Navigation_to_type_without_DbSet_is_owned()
    {
        using var db = SingleEntityDbContext.Create<OrderWithAddress>();

        var orderType = db.Model.FindEntityType(typeof(OrderWithAddress));
        Assert.NotNull(orderType);

        var addressType = db.Model.FindEntityType(typeof(Address));
        Assert.NotNull(addressType);
        Assert.NotNull(addressType.FindOwnership());
    }

    [Fact]
    public static void Navigation_to_type_with_DbSet_is_not_owned()
    {
        using var db = new OrderCustomerDbContext();

        var orderType = db.Model.FindEntityType(typeof(Order));
        Assert.NotNull(orderType);

        var customerType = db.Model.FindEntityType(typeof(Customer));
        Assert.NotNull(customerType);
        Assert.Null(customerType.FindOwnership());
        Assert.True(customerType.IsDocumentRoot());
    }

    [Fact]
    public static void Navigation_to_type_with_DbSet_creates_foreign_key()
    {
        using var db = new OrderCustomerDbContext();

        var orderType = db.Model.FindEntityType(typeof(Order));
        Assert.NotNull(orderType);

        var navigation = orderType.FindNavigation(nameof(Order.Customer));
        Assert.NotNull(navigation);
        Assert.NotNull(navigation.ForeignKey);
    }

    [Fact]
    public static void Collection_navigation_to_type_with_DbSet_is_not_owned()
    {
        using var db = new OrderCustomerDbContext();

        var customerType = db.Model.FindEntityType(typeof(Customer));
        Assert.NotNull(customerType);

        var orderType = db.Model.FindEntityType(typeof(Order));
        Assert.NotNull(orderType);
        Assert.Null(orderType.FindOwnership());

        var navigation = customerType.FindNavigation(nameof(Customer.Orders));
        Assert.NotNull(navigation);
    }

    [Fact]
    public static void Embedded_type_without_DbSet_remains_owned()
    {
        using var db = new OrderCustomerDbContext();

        var orderType = db.Model.FindEntityType(typeof(Order));
        Assert.NotNull(orderType);

        var shippingAddressType = db.Model.FindEntityType(typeof(Address));
        Assert.NotNull(shippingAddressType);
        Assert.NotNull(shippingAddressType.FindOwnership());
    }

    [Fact]
    public static void Explicit_OwnsOne_of_type_with_DbSet_stays_owned()
    {
        using var db = new ExplicitOwnsOneDbContext();

        var blogType = db.Model.FindEntityType(typeof(Blog));
        Assert.NotNull(blogType);

        var navigation = blogType.FindNavigation(nameof(Blog.Meta));
        Assert.NotNull(navigation);

        // An explicit OwnsOne wins even though the target type also has its own DbSet:
        // the navigation's target stays an owned (embedded) entity type.
        Assert.NotNull(navigation.TargetEntityType.FindOwnership());
    }

    [Fact]
    public static void Derived_type_of_root_reached_through_navigation_is_in_the_hierarchy_not_owned()
    {
        // The base type is a root entity (it has a DbSet), so a type deriving from it is part of that hierarchy
        // even though it has no DbSet of its own and is only reached through a navigation (#65, EF-X538).
        using var db = new HierarchyDbContext();

        var baseType = db.Model.FindEntityType(typeof(HierBase));
        var derivedType = db.Model.FindEntityType(typeof(HierDerived));
        Assert.NotNull(baseType);
        Assert.NotNull(derivedType);
        Assert.Null(derivedType.FindOwnership());
        Assert.False(derivedType.IsOwned());
        Assert.Same(baseType, derivedType.BaseType);
        Assert.True(derivedType.IsDocumentRoot());
    }

    [Fact]
    public static void Derived_type_reached_through_navigation_can_be_added_through_the_base_DbSet()
    {
        using var db = new HierarchyDbContext();

        // Throws "cannot be tracked as the entity type ... because the two types are not in the same hierarchy"
        // when the derived type has been made an owned type unrelated to its base.
        var entry = db.Bases.Add(new HierDerived());

        Assert.Equal(typeof(HierDerived), entry.Entity.GetType());
        Assert.False(entry.Metadata.IsOwned());
    }

    [Fact]
    public static void Explicit_OwnsOne_of_derived_type_of_root_stays_owned()
    {
        using var db = new ExplicitOwnedDerivedDbContext();

        var navigation = db.Model.FindEntityType(typeof(HierBase))!.FindNavigation(nameof(HierBase.Ref));
        Assert.NotNull(navigation);
        Assert.NotNull(navigation.TargetEntityType.FindOwnership());
    }

    [Fact]
    public static void Type_embedded_by_convention_but_navigated_to_from_another_root_is_a_root()
    {
        // GameEntity.Game would embed InboundGame, but InboundLevel (a root, registered explicitly) navigates to it too.
        // Something outside the owning document references InboundGame, so it cannot be embedded (#65, EF-X102).
        using var db = new InboundReferenceDbContext();

        var game = db.Model.FindEntityType(typeof(InboundGame));
        Assert.NotNull(game);
        Assert.Null(game.FindOwnership());
        Assert.True(game.IsDocumentRoot());

        var level = db.Model.FindEntityType(typeof(InboundLevel));
        Assert.NotNull(level);
        Assert.Null(level.FindOwnership());
        Assert.NotNull(level.FindNavigation(nameof(InboundLevel.Game))?.ForeignKey);
        Assert.NotNull(game.FindNavigation(nameof(InboundGame.Levels)));
    }

    [Fact]
    public static void Type_with_a_navigation_back_to_its_owner_stays_owned()
    {
        // A back-reference to the owner is part of the embedding, not an outside reference.
        using var db = new BackReferenceDbContext();

        var child = db.Model.FindEntityType(typeof(BackRefChild));
        Assert.NotNull(child);
        Assert.NotNull(child.FindOwnership());
    }

    [Fact]
    public static void Inbound_reference_makes_the_type_a_root_whichever_root_is_registered_first()
    {
        // Same model as above, but the root that navigates back is registered before the one that would embed the type.
        using var db = new InboundReferenceReversedDbContext();

        var game = db.Model.FindEntityType(typeof(InboundGame));
        Assert.NotNull(game);
        Assert.Null(game.FindOwnership());
        Assert.True(game.IsDocumentRoot());
        Assert.Single(db.Model.GetEntityTypes(), e => e.ClrType == typeof(InboundGame));
    }

    [Fact]
    public static void Type_reached_from_two_roots_without_navigating_back_is_embedded_in_each()
    {
        // One-way navigations are not references: each owner gets its own embedded copy (two owned entity types).
        using var db = new TwoOwnersDbContext();

        var copies = db.Model.GetEntityTypes().Where(e => e.ClrType == typeof(SharedValue)).ToList();
        Assert.Equal(2, copies.Count);
        Assert.All(copies, c => Assert.NotNull(c.FindOwnership()));
    }

    class TwoOwnersFirst
    {
        public ObjectId _id { get; set; }
        public SharedValue Value { get; set; }
    }

    class TwoOwnersSecond
    {
        public ObjectId _id { get; set; }
        public SharedValue Value { get; set; }
    }

    class SharedValue
    {
        public string Text { get; set; }
    }

    class TwoOwnersDbContext : DbContext
    {
        public DbSet<TwoOwnersFirst> Firsts { get; set; }
        public DbSet<TwoOwnersSecond> Seconds { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
    }

    class InboundReferenceReversedDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InboundLevel>();
            modelBuilder.Entity<InboundHolder>();
        }
    }

    [Fact]
    public static void Join_entity_type_of_a_skip_navigation_is_a_root_even_when_another_type_holds_a_collection_of_it()
    {
        // JoinRow is the explicit join entity of JoinOne.Twos <-> JoinTwo.Ones, so two roots depend on it. JoinExtra holds a
        // collection of JoinRow that would otherwise embed it (#65, EF-X539).
        using var db = new JoinEntityDbContext();

        var join = db.Model.FindEntityType(typeof(JoinRow));
        Assert.NotNull(join);
        Assert.Null(join.FindOwnership());
        Assert.True(join.IsDocumentRoot());
        Assert.Equal(2, join.GetForeignKeys().Count(fk => fk.PrincipalEntityType.ClrType == typeof(JoinOne) || fk.PrincipalEntityType.ClrType == typeof(JoinTwo)));
        Assert.NotNull(db.Model.FindEntityType(typeof(JoinExtra))!.FindNavigation(nameof(JoinExtra.Rows))?.ForeignKey);
    }

    class JoinOne
    {
        public int Id { get; set; }
        public List<JoinTwo> Twos { get; set; } = new();
    }

    class JoinTwo
    {
        public int Id { get; set; }
        public List<JoinOne> Ones { get; set; } = new();
    }

    class JoinRow
    {
        public int OneId { get; set; }
        public int TwoId { get; set; }
        public JoinOne One { get; set; }
        public JoinTwo Two { get; set; }
    }

    class JoinExtra
    {
        public int Id { get; set; }
        public ICollection<JoinRow> Rows { get; set; }
    }

    class JoinEntityDbContext : DbContext
    {
        public DbSet<JoinOne> Ones { get; set; }
        public DbSet<JoinTwo> Twos { get; set; }
        public DbSet<JoinExtra> Extras { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<JoinOne>().HasMany(e => e.Twos).WithMany(e => e.Ones).UsingEntity<JoinRow>();
    }

    [Fact]
    public static void Type_holding_a_collection_of_a_root_is_a_root_when_the_root_is_registered_first()
    {
        // ExtraHolder.Extra would embed ExtraDoc, but ExtraDoc holds a collection of ExtraThing, a root. ExtraThing's documents
        // refer to ExtraDoc by foreign key, so something outside ExtraHolder's document references ExtraDoc (#65, EF-X539).
        using var db = new CollectionOfRootDbContext(thingFirst: true);
        AssertExtraDocIsRoot(db);
    }

    [Fact]
    public static void Type_holding_a_collection_of_a_root_is_a_root_when_the_root_is_registered_last()
    {
        using var db = new CollectionOfRootDbContext(thingFirst: false);
        AssertExtraDocIsRoot(db);
    }

    private static void AssertExtraDocIsRoot(DbContext db)
    {
        var doc = db.Model.FindEntityType(typeof(ExtraDoc));
        Assert.NotNull(doc);
        Assert.Null(doc.FindOwnership());
        Assert.True(doc.IsDocumentRoot());
        Assert.NotNull(db.Model.FindEntityType(typeof(ExtraThing))!.GetForeignKeys().SingleOrDefault(fk => fk.PrincipalEntityType == doc));
    }

    class ExtraHolder
    {
        public ObjectId _id { get; set; }
        public ExtraDoc Extra { get; set; }
    }

    class ExtraDoc
    {
        public ObjectId _id { get; set; }
        public ICollection<ExtraThing> Things { get; set; }
    }

    class ExtraThing
    {
        public ObjectId _id { get; set; }
    }

    class CollectionOfRootDbContext(bool thingFirst) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (thingFirst)
            {
                modelBuilder.Entity<ExtraThing>();
                modelBuilder.Entity<ExtraHolder>();
            }
            else
            {
                modelBuilder.Entity<ExtraHolder>();
                modelBuilder.Entity<ExtraThing>();
            }
        }
    }

    class InboundHolder
    {
        public ObjectId _id { get; set; }
        public InboundGame Game { get; set; }
    }

    class InboundGame
    {
        public ObjectId _id { get; set; }
        public List<InboundLevel> Levels { get; set; }
    }

    class InboundLevel
    {
        public ObjectId _id { get; set; }
        public InboundGame Game { get; set; }
    }

    class InboundReferenceDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InboundHolder>();
            modelBuilder.Entity<InboundLevel>();
        }
    }

    class BackRefParent
    {
        public ObjectId _id { get; set; }
        public List<BackRefChild> Kids { get; set; }
    }

    class BackRefChild
    {
        public string Name { get; set; }
        public BackRefParent Parent { get; set; }
    }

    class BackReferenceDbContext : DbContext
    {
        public DbSet<BackRefParent> Parents { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
    }

    [Fact]
    public static void Explicitly_owned_types_with_navigations_to_each_other_stay_owned()
    {
        // The same Leaf CLR type is embedded by SideHolder and by SideBranch, and Leaf has a back-reference to SideBranch.
        // The convention may guess ownership before the nested OwnsOne/OwnsMany arrive and see an outside reference; the explicit
        // configuration then wins. Building the model must not loop between the two (#65).
        using var db = new SideEmbeddingsDbContext();

        Assert.All(db.Model.GetEntityTypes().Where(e => e.ClrType != typeof(SideRoot) && e.ClrType != typeof(SideHolder)), e => Assert.NotNull(e.FindOwnership()));
        Assert.Equal(2, db.Model.GetEntityTypes().Count(e => e.ClrType == typeof(SideLeaf)));
    }

    class SideRoot
    {
        public ObjectId _id { get; set; }
        public SideBranch Branch { get; set; }
    }

    class SideHolder
    {
        public ObjectId _id { get; set; }
        public List<SideLeaf> Leaves { get; set; }
    }

    class SideBranch
    {
        public string Name { get; set; }
        public SideLeaf Leaf { get; set; }
    }

    class SideLeaf
    {
        public string Text { get; set; }
        public SideBranch Parent { get; set; }
    }

    class SideEmbeddingsDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SideRoot>().OwnsOne(x => x.Branch, b => b.OwnsOne(x => x.Leaf));
            modelBuilder.Entity<SideHolder>().OwnsMany(x => x.Leaves);
        }
    }

    [Fact]
    public static void Derived_type_of_root_marked_with_Owned_attribute_stays_owned()
    {
        using var db = new OwnedAttributeDerivedDbContext();

        var navigation = db.Model.FindEntityType(typeof(AttrBase))!.FindNavigation(nameof(AttrBase.Ref));
        Assert.NotNull(navigation);
        Assert.NotNull(navigation.TargetEntityType.FindOwnership());
    }

    class AttrBase
    {
        public ObjectId _id { get; set; }
        public AttrDerived Ref { get; set; }
    }

    [Owned]
    class AttrDerived : AttrBase
    {
    }

    class OwnedAttributeDerivedDbContext : DbContext
    {
        public DbSet<AttrBase> Bases { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
    }

    class HierBase
    {
        public ObjectId _id { get; set; }
        public HierDerived Ref { get; set; }
    }

    class HierDerived : HierBase
    {
        public string Extra { get; set; }
    }

    class HierarchyDbContext : DbContext
    {
        public DbSet<HierBase> Bases { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
    }

    class ExplicitOwnedDerivedDbContext : DbContext
    {
        public DbSet<HierBase> Bases { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<HierBase>().OwnsOne(b => b.Ref);
    }

    class Order
    {
        public ObjectId _id { get; set; }
        public string Description { get; set; }
        public Customer Customer { get; set; }
        public Address ShippingAddress { get; set; }
    }

    class Customer
    {
        public ObjectId _id { get; set; }
        public string Name { get; set; }
        public List<Order> Orders { get; set; }
    }

    class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
    }

    class OrderWithAddress
    {
        public ObjectId _id { get; set; }
        public Address ShippingAddress { get; set; }
    }

    class Blog
    {
        public ObjectId _id { get; set; }
        public BlogMeta Meta { get; set; }
    }

    class BlogMeta
    {
        public ObjectId _id { get; set; }
        public string Summary { get; set; }
    }

    class ExplicitOwnsOneDbContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<BlogMeta> Metas { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Blog>().OwnsOne(b => b.Meta);
    }

    class OrderCustomerDbContext : DbContext
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<Customer> Customers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
    }
}
