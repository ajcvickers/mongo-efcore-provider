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

#if !EF8 && !EF9
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Update;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Optional complex properties and complex collections (EF10 only; EF8/EF9 reject optional complex properties and
/// have no <c>ComplexCollection</c>): what is stored on insert, what EF reports as changed, what the provider sends
/// in the <c>update</c> command's <c>$set</c>, and what ends up stored.
/// </summary>
/// <remarks>
/// Every tracked entity here is Added and saved in the same context and then mutated; stored state is asserted on the
/// raw <see cref="BsonDocument"/>. Loading by query and then mutating is covered by <c>ComplexTypeMaterializationTests</c>.
/// </remarks>
[XUnitCollection("UpdateTests")]
public class ComplexTypeOptionalAndCollectionWriteTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private BsonDocument ReadSingleRaw<T>(IMongoCollection<T> collection)
        => database.GetCollection<BsonDocument>(collection.CollectionNamespace)
            .Find(FilterDefinition<BsonDocument>.Empty).Single();

    private long CountRaw<T>(IMongoCollection<T> collection)
        => database.GetCollection<BsonDocument>(collection.CollectionNamespace)
            .CountDocuments(FilterDefinition<BsonDocument>.Empty);

    private static IUpdateEntry UpdateEntry(DbContext db, object entity)
        => (IUpdateEntry)db.Entry(entity).GetInfrastructure();

    // Writer order: scalar leaves in model (ordinal-name) order, then nested complex properties.
    private static BsonDocument Address(string street, string city, double lat = 0, double lon = 0)
        => new()
        {
            { "City", city },
            { "Street", street },
            { "Location", new BsonDocument { { "Lat", lat }, { "Lon", lon } } }
        };

    private static ComplexAddress NewAddress(string street, string city, double lat = 0, double lon = 0)
        => new() { Street = street, City = city, Location = new GeoPoint { Lat = lat, Lon = lon } };

    // ----------------------------------------------------------------------------------------------------------------
    // Optional complex property
    // ----------------------------------------------------------------------------------------------------------------

    private static void ConfigureOptional(ModelBuilder mb)
        => mb.Entity<CustomerWithOptionalAddress>().ComplexProperty(c => c.Address, a => a.ComplexProperty(x => x.Location));

    [Fact]
    public void Optional_complex_property_null_is_stored_as_bson_null()
    {
        var collection = database.CreateCollection<CustomerWithOptionalAddress>();

        using (var db = SingleEntityDbContext.Create(collection, ConfigureOptional))
        {
            Assert.True(db.Model.FindEntityType(typeof(CustomerWithOptionalAddress))!
                .FindComplexProperty(nameof(CustomerWithOptionalAddress.Address))!.IsNullable);
            db.Entities.Add(new CustomerWithOptionalAddress { Name = "N", Address = null });
            db.SaveChanges();
        }

        // Same as a null owned reference (WriteOwnedEntities writes null): the element is present and null, not missing.
        var raw = ReadSingleRaw(collection);
        Assert.Equal(new[] { "_id", "Name", "Address" }, raw.Names.ToArray());
        Assert.Equal(BsonNull.Value, raw["Address"]);
    }

    [Fact]
    public void Optional_complex_property_with_value_is_stored_as_subdocument()
    {
        var collection = database.CreateCollection<CustomerWithOptionalAddress>();

        using (var db = SingleEntityDbContext.Create(collection, ConfigureOptional))
        {
            db.Entities.Add(new CustomerWithOptionalAddress { Name = "N", Address = NewAddress("S", "C", 1, 2) });
            db.SaveChanges();
        }

        Assert.Equal(Address("S", "C", 1, 2), ReadSingleRaw(collection)["Address"].AsBsonDocument);
    }

    [Fact]
    public void Optional_complex_property_value_to_null_sets_null()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithOptionalAddress>());
        var customer = new CustomerWithOptionalAddress { Name = "N", Address = NewAddress("S", "C") };

        using var db = SingleEntityDbContext.Create(collection, ConfigureOptional);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        customer.Address = null;
        db.ChangeTracker.DetectChanges();
        var entry = UpdateEntry(db, customer);
        Assert.Equal(EntityState.Modified, entry.EntityState);
        Assert.True(entry.IsModified(entry.EntityType.FindComplexProperty(nameof(CustomerWithOptionalAddress.Address))!));

        Assert.Equal(1, db.SaveChanges());

        var set = capture.SingleSet();
        Assert.Equal(new[] { "_id", "Address" }, set.Names.ToArray());
        Assert.Equal(BsonNull.Value, set["Address"]);
        var raw = ReadSingleRaw(collection);
        Assert.Equal(BsonNull.Value, raw["Address"]);
        Assert.Equal("N", raw["Name"].AsString);
    }

    [Fact]
    public void Optional_complex_property_null_to_value_sets_whole_subdocument()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithOptionalAddress>());
        var customer = new CustomerWithOptionalAddress { Name = "N", Address = null };

        using var db = SingleEntityDbContext.Create(collection, ConfigureOptional);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        customer.Address = NewAddress("S", "C", 3, 4);
        Assert.Equal(1, db.SaveChanges());

        var set = capture.SingleSet();
        Assert.Equal(new[] { "_id", "Address" }, set.Names.ToArray());
        Assert.Equal(Address("S", "C", 3, 4), set["Address"].AsBsonDocument);
        Assert.Equal(Address("S", "C", 3, 4), ReadSingleRaw(collection)["Address"].AsBsonDocument);
    }

    [Fact]
    public void Optional_complex_property_null_to_value_with_all_default_leaves_is_written()
    {
        // A value whose leaves equal the CLR defaults must still count as a change from null.
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithOptionalAddress>());
        var customer = new CustomerWithOptionalAddress { Name = "N", Address = null };

        using var db = SingleEntityDbContext.Create(collection, ConfigureOptional);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        customer.Address = new ComplexAddress { Street = null!, City = null! };
        Assert.Equal(1, db.SaveChanges());

        var expected = new BsonDocument
        {
            { "City", BsonNull.Value }, { "Street", BsonNull.Value }, { "Location", new BsonDocument { { "Lat", 0.0 }, { "Lon", 0.0 } } }
        };
        Assert.Equal(expected, capture.SingleSet()["Address"].AsBsonDocument);
        Assert.Equal(expected, ReadSingleRaw(collection)["Address"].AsBsonDocument);
    }

    [Fact]
    public void Optional_complex_property_null_to_null_sends_no_update()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithOptionalAddress>());
        var customer = new CustomerWithOptionalAddress { Name = "N", Address = null };

        using var db = SingleEntityDbContext.Create(collection, ConfigureOptional);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        customer.Address = null;
        db.ChangeTracker.DetectChanges();
        Assert.Equal(EntityState.Unchanged, db.Entry(customer).State);
        Assert.Equal(0, db.SaveChanges());
        Assert.Empty(capture.Named("update"));
    }

    [Fact]
    public void Optional_complex_property_scalar_update_does_not_touch_null_subdocument()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithOptionalAddress>());
        var customer = new CustomerWithOptionalAddress { Name = "N", Address = null };

        using var db = SingleEntityDbContext.Create(collection, ConfigureOptional);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        customer.Name = "M";
        db.SaveChanges();

        Assert.Equal(new[] { "_id", "Name" }, capture.SingleSet().Names.ToArray());
        Assert.Equal(BsonNull.Value, ReadSingleRaw(collection)["Address"]);
    }

    public class Inner
    {
        public string Value { get; set; } = null!;
    }

    public class OuterWithOptional
    {
        public string Label { get; set; } = null!;
        public Inner? Extra { get; set; }
    }

    public class EntityWithNestedOptional
    {
        public ObjectId Id { get; set; }
        public OuterWithOptional Outer { get; set; } = null!;
        public List<OuterWithOptional> Items { get; set; } = [];
    }

    private static void ConfigureNestedOptional(ModelBuilder mb)
        => mb.Entity<EntityWithNestedOptional>(b =>
        {
            b.ComplexProperty(e => e.Outer, o => o.ComplexProperty(x => x.Extra));
            b.ComplexCollection(e => e.Items, o => o.ComplexProperty(x => x.Extra));
        });

    [Fact]
    public void Nested_optional_complex_property_round_trips_null_and_value_through_updates()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<EntityWithNestedOptional>());
        var entity = new EntityWithNestedOptional { Outer = new OuterWithOptional { Label = "L", Extra = null } };

        using var db = SingleEntityDbContext.Create(collection, ConfigureNestedOptional);
        db.Entities.Add(entity);
        db.SaveChanges();
        Assert.Equal(new BsonDocument { { "Label", "L" }, { "Extra", BsonNull.Value } }, ReadSingleRaw(collection)["Outer"].AsBsonDocument);

        // null -> value: the whole top-level Outer is rewritten.
        capture.Clear();
        entity.Outer.Extra = new Inner { Value = "v" };
        Assert.Equal(1, db.SaveChanges());
        var expected = new BsonDocument { { "Label", "L" }, { "Extra", new BsonDocument("Value", "v") } };
        Assert.Equal(new[] { "_id", "Outer" }, capture.SingleSet().Names.ToArray());
        Assert.Equal(expected, capture.SingleSet()["Outer"].AsBsonDocument);
        Assert.Equal(expected, ReadSingleRaw(collection)["Outer"].AsBsonDocument);

        // value -> null.
        capture.Clear();
        entity.Outer.Extra = null;
        Assert.Equal(1, db.SaveChanges());
        expected = new BsonDocument { { "Label", "L" }, { "Extra", BsonNull.Value } };
        Assert.Equal(expected, capture.SingleSet()["Outer"].AsBsonDocument);
        Assert.Equal(expected, ReadSingleRaw(collection)["Outer"].AsBsonDocument);
    }

    [Fact]
    public void Optional_complex_property_inside_collection_element_writes_null_or_subdocument()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<EntityWithNestedOptional>());
        var entity = new EntityWithNestedOptional
        {
            Outer = new OuterWithOptional { Label = "O" },
            Items =
            [
                new OuterWithOptional { Label = "a", Extra = new Inner { Value = "x" } },
                new OuterWithOptional { Label = "b", Extra = null }
            ]
        };

        using var db = SingleEntityDbContext.Create(collection, ConfigureNestedOptional);
        db.Entities.Add(entity);
        db.SaveChanges();

        Assert.Equal(
            new BsonArray
            {
                new BsonDocument { { "Label", "a" }, { "Extra", new BsonDocument("Value", "x") } },
                new BsonDocument { { "Label", "b" }, { "Extra", BsonNull.Value } }
            },
            ReadSingleRaw(collection)["Items"].AsBsonArray);

        capture.Clear();
        entity.Items[0].Extra = null;
        entity.Items[1].Extra = new Inner { Value = "y" };
        Assert.Equal(1, db.SaveChanges());

        var expected = new BsonArray
        {
            new BsonDocument { { "Label", "a" }, { "Extra", BsonNull.Value } },
            new BsonDocument { { "Label", "b" }, { "Extra", new BsonDocument("Value", "y") } }
        };
        Assert.Equal(new[] { "_id", "Items" }, capture.SingleSet().Names.ToArray());
        Assert.Equal(expected, capture.SingleSet()["Items"].AsBsonArray);
        Assert.Equal(expected, ReadSingleRaw(collection)["Items"].AsBsonArray);
    }

    // ----------------------------------------------------------------------------------------------------------------
    // Complex collections
    // ----------------------------------------------------------------------------------------------------------------

    private static void ConfigureList(ModelBuilder mb)
        => mb.Entity<CustomerWithAddressList>().ComplexCollection(c => c.Addresses, a => a.ComplexProperty(x => x.Location));

    private static CustomerWithAddressList NewListCustomer()
        => new()
        {
            Name = "Carol",
            Addresses = [NewAddress("A", "X", 1, 2), NewAddress("B", "Y", 3, 4), NewAddress("C", "Z", 5, 6)]
        };

    private static BsonArray StoredList(params BsonDocument[] elements)
        => new(elements);

    [Fact]
    public void Collection_of_N_elements_is_stored_as_array_of_N_subdocuments_in_list_order()
    {
        var collection = database.CreateCollection<CustomerWithAddressList>();

        using (var db = SingleEntityDbContext.Create(collection, ConfigureList))
        {
            db.Entities.Add(NewListCustomer());
            db.SaveChanges();
        }

        var addresses = ReadSingleRaw(collection)["Addresses"].AsBsonArray;
        Assert.Equal(StoredList(Address("A", "X", 1, 2), Address("B", "Y", 3, 4), Address("C", "Z", 5, 6)), addresses);
        Assert.All(addresses, a => Assert.False(a.AsBsonDocument.Contains("_id")));
    }

    [Fact]
    public void Required_null_complex_collection_is_rejected_by_EF_on_save()
    {
        var collection = database.CreateCollection<CustomerWithAddressList>();

        using var db = SingleEntityDbContext.Create(collection, ConfigureList);
        db.Entities.Add(new CustomerWithAddressList { Name = "N", Addresses = null! });

        var ex = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Contains("required", ex.Message);
        Assert.Equal(0, CountRaw(collection));
    }

    [Fact]
    public void Null_element_in_complex_collection_is_stored_as_bson_null()
    {
        var collection = database.CreateCollection<CustomerWithAddressList>();

        using (var db = SingleEntityDbContext.Create(collection, ConfigureList))
        {
            db.Entities.Add(new CustomerWithAddressList { Name = "N", Addresses = [NewAddress("A", "X"), null!] });
            db.SaveChanges();
        }

        Assert.Equal(new BsonArray { Address("A", "X"), BsonNull.Value }, ReadSingleRaw(collection)["Addresses"].AsBsonArray);
    }

    public class CustomerWithOptionalAddressList
    {
        public ObjectId Id { get; set; }
        public List<ComplexAddress>? Addresses { get; set; }
    }

    [Fact]
    public void Optional_complex_collection_null_is_stored_as_null_and_round_trips_through_updates()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithOptionalAddressList>());
        var entity = new CustomerWithOptionalAddressList { Addresses = null };

        using var db = SingleEntityDbContext.Create(collection,
            mb => mb.Entity<CustomerWithOptionalAddressList>().ComplexCollection(c => c.Addresses, a =>
            {
                a.IsRequired(false);
                a.ComplexProperty(x => x.Location);
            }));
        db.Entities.Add(entity);
        db.SaveChanges();
        Assert.Equal(BsonNull.Value, ReadSingleRaw(collection)["Addresses"]);

        capture.Clear();
        entity.Addresses = [NewAddress("A", "X")];
        Assert.Equal(1, db.SaveChanges());
        Assert.Equal(StoredList(Address("A", "X")), capture.SingleSet()["Addresses"].AsBsonArray);
        Assert.Equal(StoredList(Address("A", "X")), ReadSingleRaw(collection)["Addresses"].AsBsonArray);

        capture.Clear();
        entity.Addresses = null;
        Assert.Equal(1, db.SaveChanges());
        Assert.Equal(new[] { "_id", "Addresses" }, capture.SingleSet().Names.ToArray());
        Assert.Equal(BsonNull.Value, capture.SingleSet()["Addresses"]);
        Assert.Equal(BsonNull.Value, ReadSingleRaw(collection)["Addresses"]);
    }

    [Fact]
    public void Editing_only_an_element_leaf_promotes_root_and_rewrites_whole_array()
    {
        // Carry-forward risk: a change only inside a complex collection must not leave the root Unchanged (which
        // would silently drop the change). Measured: EF10 moves the root to Modified and reports the collection as
        // modified through IUpdateEntry.IsModified(IComplexProperty).
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddressList>());
        var customer = NewListCustomer();

        using var db = SingleEntityDbContext.Create(collection, ConfigureList);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        customer.Addresses[1].City = "Changed";
        db.ChangeTracker.DetectChanges();

        var entry = db.Entry(customer);
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.False(entry.Property(c => c.Name).IsModified);
        var addresses = entry.ComplexCollection(c => c.Addresses);
        Assert.True(addresses.IsModified);
        Assert.Equal(EntityState.Unchanged, addresses[0].State);
        Assert.Equal(EntityState.Modified, addresses[1].State);
        Assert.True(addresses[1].Property(a => a.City).IsModified);
        var updateEntry = UpdateEntry(db, customer);
        Assert.True(updateEntry.IsModified(updateEntry.EntityType.FindComplexProperty(nameof(CustomerWithAddressList.Addresses))!));

        Assert.Equal(1, db.SaveChanges());

        var expected = StoredList(Address("A", "X", 1, 2), Address("B", "Changed", 3, 4), Address("C", "Z", 5, 6));
        var set = capture.SingleSet();
        Assert.Equal(new[] { "_id", "Addresses" }, set.Names.ToArray());
        Assert.Equal(expected, set["Addresses"].AsBsonArray);
        var raw = ReadSingleRaw(collection);
        Assert.Equal(expected, raw["Addresses"].AsBsonArray);
        Assert.Equal("Carol", raw["Name"].AsString);

        // Accepted after save: nothing further is sent.
        capture.Clear();
        Assert.False(addresses.IsModified);
        Assert.Equal(EntityState.Unchanged, entry.State);
        Assert.Equal(0, db.SaveChanges());
        Assert.Empty(capture.Named("update"));
    }

    [Fact]
    public void Editing_a_nested_struct_leaf_inside_an_element_rewrites_whole_array()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddressList>());
        var customer = NewListCustomer();

        using var db = SingleEntityDbContext.Create(collection, ConfigureList);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        customer.Addresses[2].Location = new GeoPoint { Lat = 50, Lon = 6 };
        Assert.Equal(1, db.SaveChanges());

        var expected = StoredList(Address("A", "X", 1, 2), Address("B", "Y", 3, 4), Address("C", "Z", 50, 6));
        Assert.Equal(expected, capture.SingleSet()["Addresses"].AsBsonArray);
        Assert.Equal(expected, ReadSingleRaw(collection)["Addresses"].AsBsonArray);
    }

    public static TheoryData<string> CollectionMutations
        => new() { "add", "remove", "reorder", "clear", "replace-instance", "set-element-null" };

    [Theory]
    [MemberData(nameof(CollectionMutations))]
    public void Collection_structure_change_rewrites_whole_array(string mutation)
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddressList>(nameof(Collection_structure_change_rewrites_whole_array), mutation));
        var customer = NewListCustomer();

        using var db = SingleEntityDbContext.Create(collection, ConfigureList);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        BsonArray expected;
        switch (mutation)
        {
            case "add":
                customer.Addresses.Add(NewAddress("D", "W", 7, 8));
                expected = StoredList(Address("A", "X", 1, 2), Address("B", "Y", 3, 4), Address("C", "Z", 5, 6), Address("D", "W", 7, 8));
                break;
            case "remove":
                customer.Addresses.RemoveAt(0);
                expected = StoredList(Address("B", "Y", 3, 4), Address("C", "Z", 5, 6));
                break;
            case "reorder":
                customer.Addresses.Reverse();
                expected = StoredList(Address("C", "Z", 5, 6), Address("B", "Y", 3, 4), Address("A", "X", 1, 2));
                break;
            case "clear":
                customer.Addresses.Clear();
                expected = [];
                break;
            case "replace-instance":
                customer.Addresses = [NewAddress("Q", "R")];
                expected = StoredList(Address("Q", "R"));
                break;
            case "set-element-null":
                customer.Addresses[0] = null!;
                expected = new BsonArray { BsonNull.Value, Address("B", "Y", 3, 4), Address("C", "Z", 5, 6) };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation));
        }

        db.ChangeTracker.DetectChanges();
        Assert.Equal(EntityState.Modified, db.Entry(customer).State);
        Assert.True(db.Entry(customer).ComplexCollection(c => c.Addresses).IsModified);

        Assert.Equal(1, db.SaveChanges());

        var set = capture.SingleSet();
        Assert.Equal(new[] { "_id", "Addresses" }, set.Names.ToArray());
        Assert.Equal(expected, set["Addresses"].AsBsonArray);
        Assert.Equal(expected, ReadSingleRaw(collection)["Addresses"].AsBsonArray);
    }

    [Fact]
    public void Replacing_collection_with_equal_elements_is_not_a_change()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddressList>());
        var customer = NewListCustomer();

        using var db = SingleEntityDbContext.Create(collection, ConfigureList);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        customer.Addresses = customer.Addresses
            .Select(a => NewAddress(a.Street, a.City, a.Location.Lat, a.Location.Lon)).ToList();
        db.ChangeTracker.DetectChanges();

        Assert.Equal(EntityState.Unchanged, db.Entry(customer).State);
        Assert.Equal(0, db.SaveChanges());
        Assert.Empty(capture.Named("update"));
    }

    [Fact]
    public void Scalar_update_on_entity_with_complex_collection_does_not_rewrite_array()
    {
        // Replaces the Task 4 guard that rejected every update of an entity with a complex collection.
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddressList>());
        var customer = NewListCustomer();

        using var db = SingleEntityDbContext.Create(collection, ConfigureList);
        db.Entities.Add(customer);
        db.SaveChanges();
        capture.Clear();

        customer.Name = "Caroline";
        db.ChangeTracker.DetectChanges();
        Assert.False(db.Entry(customer).ComplexCollection(c => c.Addresses).IsModified);
        Assert.Equal(1, db.SaveChanges());

        Assert.Equal(new[] { "_id", "Name" }, capture.SingleSet().Names.ToArray());
        var raw = ReadSingleRaw(collection);
        Assert.Equal("Caroline", raw["Name"].AsString);
        Assert.Equal(3, raw["Addresses"].AsBsonArray.Count);
    }

    [Fact]
    public void Insert_update_and_delete_of_entity_with_complex_collection()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithAddressList>());
        var customer = NewListCustomer();
        var other = new CustomerWithAddressList { Name = "Other", Addresses = [NewAddress("O", "O")] };

        using var db = SingleEntityDbContext.Create(collection, ConfigureList);
        db.Entities.AddRange(customer, other);
        db.SaveChanges();
        Assert.Equal(2, CountRaw(collection));

        customer.Addresses.Add(NewAddress("D", "W"));
        customer.Name = "Carla";
        Assert.Equal(1, db.SaveChanges());
        var stored = database.GetCollection<BsonDocument>(collection.CollectionNamespace)
            .Find(Builders<BsonDocument>.Filter.Eq("_id", customer.Id)).Single();
        Assert.Equal("Carla", stored["Name"].AsString);
        Assert.Equal(
            StoredList(Address("A", "X", 1, 2), Address("B", "Y", 3, 4), Address("C", "Z", 5, 6), Address("D", "W")),
            stored["Addresses"].AsBsonArray);

        capture.Clear();
        customer.Addresses[0].City = "mutated before delete";
        db.Entities.Remove(customer);
        Assert.Equal(1, db.SaveChanges());

        Assert.Empty(capture.Named("update"));
        var delete = Assert.Single(capture.Named("delete"));
        var statement = Assert.Single(delete["deletes"].AsBsonArray).AsBsonDocument;
        Assert.Equal(new BsonDocument("_id", customer.Id), statement["q"].AsBsonDocument);

        var remaining = ReadSingleRaw(collection);
        Assert.Equal(other.Id, remaining["_id"].AsObjectId);
        Assert.Equal(StoredList(Address("O", "O")), remaining["Addresses"].AsBsonArray);
    }

    // ----------------------------------------------------------------------------------------------------------------
    // Disconnected (whole-entry Modified) updates: no query load, a fresh context marks the entity Modified.
    // ----------------------------------------------------------------------------------------------------------------

    public static TheoryData<string> WholeEntryModifiedShapes
        => new() { "Update", "Attach+State" };

    private static void MarkWholeEntryModified<T>(SingleEntityDbContext<T> db, T entity, string shape) where T : class
    {
        switch (shape)
        {
            case "Update":
                db.Entities.Update(entity);
                break;
            case "Attach+State":
                db.Entities.Attach(entity).State = EntityState.Modified;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape));
        }
    }

    [Theory]
    [MemberData(nameof(WholeEntryModifiedShapes))]
    public void Whole_entry_modified_entity_writes_its_complex_collection(string shape)
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(
            database.CreateCollection<CustomerWithAddressList>(nameof(Whole_entry_modified_entity_writes_its_complex_collection), shape));
        var customer = NewListCustomer();

        using (var seed = SingleEntityDbContext.Create(collection, ConfigureList))
        {
            seed.Entities.Add(customer);
            seed.SaveChanges();
        }

        capture.Clear();
        using var db = SingleEntityDbContext.Create(collection, ConfigureList);
        var detached = new CustomerWithAddressList
        {
            Id = customer.Id, Name = "Carol", Addresses = [NewAddress("N", "M", 9, 9), NewAddress("A", "X", 1, 2)]
        };
        MarkWholeEntryModified(db, detached, shape);

        // Measured shape (EF10): the root is Modified and the collection reports modified as a whole.
        var entry = db.Entry(detached);
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.True(entry.ComplexCollection(c => c.Addresses).IsModified);
        var updateEntry = UpdateEntry(db, detached);
        Assert.True(updateEntry.IsModified(updateEntry.EntityType.FindComplexProperty(nameof(CustomerWithAddressList.Addresses))!));

        Assert.Equal(1, db.SaveChanges());

        var expected = StoredList(Address("N", "M", 9, 9), Address("A", "X", 1, 2));
        var set = capture.SingleSet();
        Assert.Equal(new[] { "_id", "Name", "Addresses" }, set.Names.ToArray());
        Assert.Equal(expected, set["Addresses"].AsBsonArray);
        Assert.Equal(expected, ReadSingleRaw(collection)["Addresses"].AsBsonArray);
    }

    [Theory]
    [MemberData(nameof(WholeEntryModifiedShapes))]
    public void Whole_entry_modified_entity_writes_optional_complex_property_value_and_null(string shape)
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<CustomerWithOptionalAddress>(
            nameof(Whole_entry_modified_entity_writes_optional_complex_property_value_and_null), shape));
        var customer = new CustomerWithOptionalAddress { Name = "N", Address = NewAddress("S", "C", 1, 2) };

        using (var seed = SingleEntityDbContext.Create(collection, ConfigureOptional))
        {
            seed.Entities.Add(customer);
            seed.SaveChanges();
        }

        // Different value.
        capture.Clear();
        using (var db = SingleEntityDbContext.Create(collection, ConfigureOptional))
        {
            var detached = new CustomerWithOptionalAddress { Id = customer.Id, Name = "N", Address = NewAddress("T", "D", 3, 4) };
            MarkWholeEntryModified(db, detached, shape);
            Assert.Equal(1, db.SaveChanges());
        }

        Assert.Equal(new[] { "_id", "Name", "Address" }, capture.SingleSet().Names.ToArray());
        Assert.Equal(Address("T", "D", 3, 4), capture.SingleSet()["Address"].AsBsonDocument);
        Assert.Equal(Address("T", "D", 3, 4), ReadSingleRaw(collection)["Address"].AsBsonDocument);

        // Null.
        capture.Clear();
        using (var db = SingleEntityDbContext.Create(collection, ConfigureOptional))
        {
            var detached = new CustomerWithOptionalAddress { Id = customer.Id, Name = "N", Address = null };
            MarkWholeEntryModified(db, detached, shape);
            Assert.Equal(1, db.SaveChanges());
        }

        Assert.Equal(new[] { "_id", "Name", "Address" }, capture.SingleSet().Names.ToArray());
        Assert.Equal(BsonNull.Value, capture.SingleSet()["Address"]);
        Assert.Equal(BsonNull.Value, ReadSingleRaw(collection)["Address"]);
    }

    public class Wrapper
    {
        public string Label { get; set; } = null!;
        public List<Inner> Inners { get; set; } = [];
    }

    public class EntityWithNestedCollections
    {
        public ObjectId Id { get; set; }
        public Wrapper Wrap { get; set; } = null!;
        public List<Wrapper> Wraps { get; set; } = [];
    }

    private static void ConfigureNestedCollections(ModelBuilder mb)
        => mb.Entity<EntityWithNestedCollections>(b =>
        {
            b.ComplexProperty(e => e.Wrap, w => w.ComplexCollection(x => x.Inners));
            b.ComplexCollection(e => e.Wraps, w => w.ComplexCollection(x => x.Inners));
        });

    private static BsonDocument StoredWrapper(string label, params string[] inners)
        => new()
        {
            { "Label", label }, { "Inners", new BsonArray(inners.Select(v => new BsonDocument("Value", v))) }
        };

    [Fact]
    public void Collection_nested_in_complex_property_change_rewrites_parent_subdocument_only()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<EntityWithNestedCollections>());
        var entity = new EntityWithNestedCollections
        {
            Wrap = new Wrapper { Label = "w", Inners = [new Inner { Value = "1" }] },
            Wraps = [new Wrapper { Label = "x", Inners = [new Inner { Value = "2" }] }]
        };

        using var db = SingleEntityDbContext.Create(collection, ConfigureNestedCollections);
        db.Entities.Add(entity);
        db.SaveChanges();
        Assert.Equal(StoredWrapper("w", "1"), ReadSingleRaw(collection)["Wrap"].AsBsonDocument);
        Assert.Equal(new BsonArray { StoredWrapper("x", "2") }, ReadSingleRaw(collection)["Wraps"].AsBsonArray);

        // A leaf inside the collection nested in a (non-collection) complex property: no leaf of Wrap itself is
        // modified, so only the nested collection's own modified flag can trigger the rewrite.
        capture.Clear();
        entity.Wrap.Inners[0].Value = "1b";
        entity.Wrap.Inners.Add(new Inner { Value = "1c" });
        Assert.Equal(1, db.SaveChanges());
        Assert.Equal(new[] { "_id", "Wrap" }, capture.SingleSet().Names.ToArray());
        Assert.Equal(StoredWrapper("w", "1b", "1c"), capture.SingleSet()["Wrap"].AsBsonDocument);
        Assert.Equal(StoredWrapper("w", "1b", "1c"), ReadSingleRaw(collection)["Wrap"].AsBsonDocument);
    }

    [Fact]
    public void Collection_nested_in_collection_element_change_rewrites_whole_outer_array()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<EntityWithNestedCollections>());
        var entity = new EntityWithNestedCollections
        {
            Wrap = new Wrapper { Label = "w" },
            Wraps =
            [
                new Wrapper { Label = "x", Inners = [new Inner { Value = "2" }] },
                new Wrapper { Label = "y", Inners = [new Inner { Value = "3" }] }
            ]
        };

        using var db = SingleEntityDbContext.Create(collection, ConfigureNestedCollections);
        db.Entities.Add(entity);
        db.SaveChanges();
        capture.Clear();

        entity.Wraps[1].Inners[0].Value = "3b";
        Assert.Equal(1, db.SaveChanges());

        var expected = new BsonArray { StoredWrapper("x", "2"), StoredWrapper("y", "3b") };
        Assert.Equal(new[] { "_id", "Wraps" }, capture.SingleSet().Names.ToArray());
        Assert.Equal(expected, capture.SingleSet()["Wraps"].AsBsonArray);
        Assert.Equal(expected, ReadSingleRaw(collection)["Wraps"].AsBsonArray);
    }

    // OwnedNavigationBuilder has no ComplexCollection overload; [ComplexType] lets a List<T> on an owned type be
    // discovered as a complex collection.
    [System.ComponentModel.DataAnnotations.Schema.ComplexType]
    public class OwnedElement
    {
        public string Value { get; set; } = null!;
    }

    public class OwnedWithCollection
    {
        public string Note { get; set; } = null!;
        public List<OwnedElement> Elements { get; set; } = [];
    }

    public class EntityWithOwnedHoldingCollection
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public OwnedWithCollection Owned { get; set; } = null!;
    }

    [Fact]
    public void Complex_collection_on_owned_entity_is_written_whole_inside_owned_subdocument()
    {
        using var capture = new CommandCapture(database);
        var collection = capture.Collection(database.CreateCollection<EntityWithOwnedHoldingCollection>());
        var entity = new EntityWithOwnedHoldingCollection
        {
            Name = "E",
            Owned = new OwnedWithCollection { Note = "n", Elements = [new OwnedElement { Value = "a" }, new OwnedElement { Value = "b" }] }
        };

        using var db = SingleEntityDbContext.Create(collection, mb => mb.Entity<EntityWithOwnedHoldingCollection>().OwnsOne(e => e.Owned));
        Assert.True(db.Model.FindEntityType(typeof(OwnedWithCollection))!
            .FindComplexProperty(nameof(OwnedWithCollection.Elements))!.IsCollection);
        db.Entities.Add(entity);
        db.SaveChanges();

        BsonDocument OwnedDoc(params string[] values)
            => new() { { "Note", "n" }, { "Elements", new BsonArray(values.Select(v => new BsonDocument("Value", v))) } };

        Assert.Equal(OwnedDoc("a", "b"), ReadSingleRaw(collection)["Owned"].AsBsonDocument);

        // Change only inside the owned entity's complex collection: the owned entry is Modified, the root is
        // Unchanged until the provider promotes it; the owned subdocument (with the whole array) is rewritten.
        capture.Clear();
        entity.Owned.Elements[1].Value = "b2";
        db.ChangeTracker.DetectChanges();
        Assert.Equal(EntityState.Modified, db.Entry(entity.Owned).State);
        Assert.Equal(EntityState.Unchanged, db.Entry(entity).State);
        Assert.Equal(1, db.SaveChanges());

        Assert.Equal(new[] { "_id", "Owned" }, capture.SingleSet().Names.ToArray());
        Assert.Equal(OwnedDoc("a", "b2"), capture.SingleSet()["Owned"].AsBsonDocument);
        Assert.Equal(OwnedDoc("a", "b2"), ReadSingleRaw(collection)["Owned"].AsBsonDocument);

        // A root scalar change also rewrites the owned subdocument (existing owned behavior); the array stays whole.
        capture.Clear();
        entity.Name = "E2";
        Assert.Equal(1, db.SaveChanges());
        Assert.Equal(OwnedDoc("a", "b2"), capture.SingleSet()["Owned"].AsBsonDocument);
        Assert.Equal("E2", ReadSingleRaw(collection)["Name"].AsString);
    }
}
#endif
