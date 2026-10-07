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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Update;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Change tracking, update and delete of entities with complex properties: what EF reports as modified, what the
/// provider sends in the <c>update</c> command's <c>$set</c>, and what ends up stored.
/// </summary>
/// <remarks>
/// Reading complex properties back (materialization) is not implemented yet, so every tracked entity here is
/// Added and saved in the same context, then mutated; nothing is loaded by query.
/// </remarks>
[XUnitCollection("UpdateTests")]
public class ComplexTypeTrackingTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private static void ConfigureCustomer(ModelBuilder mb)
        => mb.Entity<CustomerWithAddress>().ComplexProperty(c => c.Address, a => a.ComplexProperty(x => x.Location));

    private static CustomerWithAddress NewCustomer()
        => new()
        {
            Name = "Alice",
            Address = new ComplexAddress
            {
                Street = "1 Main St", City = "Springfield", Location = new GeoPoint { Lat = 12.5, Lon = -3.25 }
            }
        };

    private BsonDocument ReadSingleRaw<T>(IMongoCollection<T> collection)
        => database.GetCollection<BsonDocument>(collection.CollectionNamespace)
            .Find(FilterDefinition<BsonDocument>.Empty).Single();

    private static IUpdateEntry UpdateEntry(DbContext db, object entity)
        => (IUpdateEntry)db.Entry(entity).GetInfrastructure();

    private static BsonDocument ExpectedAddress(string street, string city, double lat, double lon)
        => new()
        {
            // Writer order: scalar leaves in model (ordinal-name) order, then nested complex properties.
            { "City", city },
            { "Street", street },
            { "Location", new BsonDocument { { "Lat", lat }, { "Lon", lon } } }
        };

    [Fact]
    public void Mutating_complex_leaf_rewrites_whole_subdocument_and_leaves_other_fields()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddress>());
        var customer = NewCustomer();

        using var db = SingleEntityDbContext.Create(collection, ConfigureCustomer);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        customer.Address.City = "Shelbyville";
        db.ChangeTracker.DetectChanges();

        // (a) the leaf is modified on the root entry, its siblings are not, and the root goes to Modified.
        var entry = db.Entry(customer);
        Assert.Equal(EntityState.Modified, entry.State);
        var address = entry.ComplexProperty(c => c.Address);
        Assert.True(address.Property(a => a.City).IsModified);
        Assert.False(address.Property(a => a.Street).IsModified);
        Assert.False(address.ComplexProperty(a => a.Location).Property(l => l.Lat).IsModified);
        Assert.False(entry.Property(c => c.Name).IsModified);

        // The update pipeline sees the same per-leaf state through IUpdateEntry.IsModified(IProperty).
        var updateEntry = UpdateEntry(db, customer);
        var addressType = updateEntry.EntityType.FindComplexProperty(nameof(CustomerWithAddress.Address))!.ComplexType;
        Assert.True(updateEntry.IsModified(addressType.FindProperty(nameof(ComplexAddress.City))!));
        Assert.False(updateEntry.IsModified(addressType.FindProperty(nameof(ComplexAddress.Street))!));

        Assert.Equal(1, db.SaveChanges());

        // The whole subdocument is rewritten (never a partial one), and nothing else is.
        var set = capture.SingleSet();
        Assert.Equal(new[] { "_id", "Address" }, set.Names.ToArray());
        Assert.Equal(ExpectedAddress("1 Main St", "Shelbyville", 12.5, -3.25), set["Address"].AsBsonDocument);

        var raw = ReadSingleRaw(collection);
        Assert.Equal("Alice", raw["Name"].AsString);
        Assert.Equal(ExpectedAddress("1 Main St", "Shelbyville", 12.5, -3.25), raw["Address"].AsBsonDocument);
    }

    [Fact]
    public void Leaf_IsModified_reflects_change_via_complex_property_entry()
    {
        var collection = database.CreateCollection<CustomerWithAddress>();
        var customer = NewCustomer();

        using var db = SingleEntityDbContext.Create(collection, ConfigureCustomer);
        db.Entities.Add(customer);
        db.SaveChanges();

        var city = db.Entry(customer).ComplexProperty(c => c.Address).Property(a => a.City);
        Assert.False(city.IsModified);

        customer.Address.City = "Capital City";
        db.ChangeTracker.DetectChanges();
        Assert.True(city.IsModified);
        Assert.Equal("Springfield", city.OriginalValue);
        Assert.Equal("Capital City", city.CurrentValue);

        db.SaveChanges();
        Assert.False(city.IsModified);
        Assert.Equal("Capital City", city.OriginalValue);
        Assert.Equal(EntityState.Unchanged, db.Entry(customer).State);
    }

    [Fact]
    public void Replacing_complex_instance_marks_changed_leaves_and_rewrites_subdocument()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddress>());
        var customer = NewCustomer();

        using var db = SingleEntityDbContext.Create(collection, ConfigureCustomer);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        // Same Location and Street values, new City: only City differs from the snapshot.
        customer.Address = new ComplexAddress
        {
            Street = "1 Main St", City = "Ogdenville", Location = new GeoPoint { Lat = 12.5, Lon = -3.25 }
        };
        db.ChangeTracker.DetectChanges();

        var address = db.Entry(customer).ComplexProperty(c => c.Address);
        Assert.Equal(EntityState.Modified, db.Entry(customer).State);
        Assert.True(address.Property(a => a.City).IsModified);
        Assert.False(address.Property(a => a.Street).IsModified);

        Assert.Equal(1, db.SaveChanges());

        var set = capture.SingleSet();
        Assert.Equal(new[] { "_id", "Address" }, set.Names.ToArray());
        Assert.Equal(ExpectedAddress("1 Main St", "Ogdenville", 12.5, -3.25), ReadSingleRaw(collection)["Address"].AsBsonDocument);
    }

    [Fact]
    public void Replacing_complex_instance_with_equal_values_is_not_a_change()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddress>());
        var customer = NewCustomer();

        using var db = SingleEntityDbContext.Create(collection, ConfigureCustomer);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        customer.Address = new ComplexAddress
        {
            Street = "1 Main St", City = "Springfield", Location = new GeoPoint { Lat = 12.5, Lon = -3.25 }
        };
        db.ChangeTracker.DetectChanges();

        Assert.Equal(EntityState.Unchanged, db.Entry(customer).State);
        Assert.Equal(0, db.SaveChanges());
        Assert.Empty(capture.Named("update"));
    }

    [Fact]
    public void Mutating_nested_struct_leaf_rewrites_whole_parent_subdocument()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddress>());
        var customer = NewCustomer();

        using var db = SingleEntityDbContext.Create(collection, ConfigureCustomer);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        // A struct property returns a copy, so an in-place change is copy, mutate, assign back.
        var location = customer.Address.Location;
        location.Lat = 40.75;
        customer.Address.Location = location;
        db.ChangeTracker.DetectChanges();

        var locationEntry = db.Entry(customer).ComplexProperty(c => c.Address).ComplexProperty(a => a.Location);
        Assert.Equal(EntityState.Modified, db.Entry(customer).State);
        Assert.True(locationEntry.Property(l => l.Lat).IsModified);
        Assert.False(locationEntry.Property(l => l.Lon).IsModified);
        Assert.False(db.Entry(customer).ComplexProperty(c => c.Address).Property(a => a.City).IsModified);

        Assert.Equal(1, db.SaveChanges());

        var set = capture.SingleSet();
        Assert.Equal(new[] { "_id", "Address" }, set.Names.ToArray());
        var expected = ExpectedAddress("1 Main St", "Springfield", 40.75, -3.25);
        Assert.Equal(expected, set["Address"].AsBsonDocument);
        Assert.Equal(expected, ReadSingleRaw(collection)["Address"].AsBsonDocument);
        Assert.Equal("Alice", ReadSingleRaw(collection)["Name"].AsString);
    }

    [Fact]
    public void DetectChanges_picks_up_nested_leaf_change_on_tracked_entity()
    {
        var collection = database.CreateCollection<CustomerWithAddress>();
        var customer = NewCustomer();

        using var db = SingleEntityDbContext.Create(collection, ConfigureCustomer);
        db.Entities.Add(customer);
        db.SaveChanges();

        var location = customer.Address.Location;
        location.Lon = 100;
        customer.Address.Location = location;

        Assert.True(db.ChangeTracker.HasChanges()); // HasChanges runs DetectChanges
        var lon = db.Entry(customer).ComplexProperty(c => c.Address).ComplexProperty(a => a.Location).Property(l => l.Lon);
        Assert.True(lon.IsModified);
        Assert.Equal(-3.25, lon.OriginalValue);
    }

    [Fact]
    public void Second_SaveChanges_without_changes_issues_no_update()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddress>());
        var customer = NewCustomer();

        using var db = SingleEntityDbContext.Create(collection, ConfigureCustomer);
        db.Entities.Add(customer);
        db.SaveChanges();

        customer.Address.City = "North Haverbrook";
        Assert.Equal(1, db.SaveChanges());
        capture.Clear();

        Assert.Equal(0, db.SaveChanges());
        Assert.Empty(capture.Named("update"));
        Assert.Empty(capture.Named("insert"));
        Assert.Empty(capture.Named("delete"));
    }

    [Fact]
    public void Delete_removes_document_with_complex_property()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddress>());
        var customer = NewCustomer();
        var other = NewCustomer();
        other.Name = "Bob";

        using var db = SingleEntityDbContext.Create(collection, ConfigureCustomer);
        db.Entities.AddRange(customer, other);
        db.SaveChanges();
        capture.Clear();

        customer.Address.City = "Changed before delete";
        db.Entities.Remove(customer);
        Assert.Equal(1, db.SaveChanges());

        Assert.Empty(capture.Named("update"));
        var delete = Assert.Single(capture.Named("delete"));
        Assert.Equal(new BsonDocument("_id", customer.Id), Assert.Single(delete["deletes"].AsBsonArray)["q"].AsBsonDocument);

        var remaining = ReadSingleRaw(collection);
        Assert.Equal(other.Id, remaining["_id"].AsObjectId);
        Assert.Equal("Springfield", remaining["Address"]["City"].AsString);
    }

    [Fact]
    public void Attached_entity_marked_modified_writes_complex_property_whole()
    {
        // Attach + Entry.State = Modified marks every property, including complex leaves, as modified.
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddress>());
        var customer = NewCustomer();

        using (var seed = SingleEntityDbContext.Create(collection, ConfigureCustomer))
        {
            seed.Entities.Add(customer);
            seed.SaveChanges();
        }

        capture.Clear();
        using var db = SingleEntityDbContext.Create(collection, ConfigureCustomer);
        var detached = new CustomerWithAddress
        {
            Id = customer.Id,
            Name = "Alice",
            Address = new ComplexAddress { Street = "2 Side St", City = "Springfield", Location = new GeoPoint { Lat = 1, Lon = 2 } }
        };
        db.Entities.Attach(detached).State = EntityState.Modified;
        Assert.True(db.Entry(detached).ComplexProperty(c => c.Address).Property(a => a.City).IsModified);

        Assert.Equal(1, db.SaveChanges());
        var set = capture.SingleSet();
        Assert.Equal(new[] { "_id", "Name", "Address" }, set.Names.ToArray());
        Assert.Equal(ExpectedAddress("2 Side St", "Springfield", 1, 2), ReadSingleRaw(collection)["Address"].AsBsonDocument);
    }

    [Fact]
    public void Mutating_complex_leaf_on_owned_entity_rewrites_owned_subdocument()
    {
        var collection = database.CreateCollection<ComplexTypeWriteTests.EntityWithOwnedHoldingComplex>();
        var entity = new ComplexTypeWriteTests.EntityWithOwnedHoldingComplex
        {
            Owned = new ComplexTypeWriteTests.OwnedWithComplex
            {
                Note = "n", Extra = new ComplexTypeWriteTests.OwnedExtra { Value = "x" }
            }
        };

        using var db = SingleEntityDbContext.Create(collection,
            mb => mb.Entity<ComplexTypeWriteTests.EntityWithOwnedHoldingComplex>().OwnsOne(e => e.Owned));
        db.Entities.Add(entity);
        db.SaveChanges();

        entity.Owned.Extra.Value = "y";
        Assert.Equal(1, db.SaveChanges());

        var owned = ReadSingleRaw(collection)["Owned"].AsBsonDocument;
        Assert.Equal("n", owned["Note"].AsString);
        Assert.Equal(new BsonDocument("Value", "y"), owned["Extra"].AsBsonDocument);
    }

    public class ComplexWithShadow
    {
        public string Value { get; set; } = null!;
    }

    public class EntityWithShadowedComplex
    {
        public ObjectId Id { get; set; }
        public ComplexWithShadow Details { get; set; } = null!;
    }

    [Fact]
    public void Shadow_property_on_complex_type()
    {
        // (d) EF10 rejects shadow properties on complex types at model building; EF8/EF9 accept them. The writer
        // reads leaves off the CLR instance, so on EF8/EF9 the shadow leaf's value comes from the entry instead.
        var collection = database.CreateCollection<EntityWithShadowedComplex>();
        var entity = new EntityWithShadowedComplex { Details = new ComplexWithShadow { Value = "v" } };

        using var db = SingleEntityDbContext.Create(collection,
            mb => mb.Entity<EntityWithShadowedComplex>().ComplexProperty(e => e.Details, d => d.Property<string>("Hidden")));

#if EF8 || EF9
        db.Entities.Add(entity);
        var hidden = db.Entry(entity).ComplexProperty(e => e.Details).Property<string>("Hidden");
        hidden.CurrentValue = "h1";
        db.SaveChanges();

        Assert.Equal(new BsonDocument { { "Hidden", "h1" }, { "Value", "v" } }, ReadSingleRaw(collection)["Details"].AsBsonDocument);

        hidden.CurrentValue = "h2";
        Assert.True(hidden.IsModified);
        Assert.Equal(1, db.SaveChanges());
        Assert.Equal(new BsonDocument { { "Hidden", "h2" }, { "Value", "v" } }, ReadSingleRaw(collection)["Details"].AsBsonDocument);
#else
        var ex = Assert.Throws<InvalidOperationException>(() => db.Model);
        Assert.Contains("Shadow properties are not supported on complex types", ex.Message);
#endif
    }

    public class EntityWithEmptyNamedComplexLeaf
    {
        public ObjectId Id { get; set; }
        public ComplexWithShadow Details { get; set; } = null!;
        public string Other { get; set; } = null!;
    }

    [Fact]
    public void Complex_leaf_with_empty_element_name_is_not_written()
    {
        // Consistent with top-level scalars (WriteNonKeyProperties): an empty element name means "not stored".
        var collection = database.CreateCollection<EntityWithEmptyNamedComplexLeaf>();

        using var db = SingleEntityDbContext.Create(collection,
            mb => mb.Entity<EntityWithEmptyNamedComplexLeaf>().ComplexProperty(e => e.Details,
                d => d.Property(x => x.Value).Metadata.SetElementName("")));
        db.Entities.Add(new EntityWithEmptyNamedComplexLeaf { Other = "o", Details = new ComplexWithShadow { Value = "v" } });
        db.SaveChanges();

        Assert.Equal(new BsonDocument(), ReadSingleRaw(collection)["Details"].AsBsonDocument);
    }
}
