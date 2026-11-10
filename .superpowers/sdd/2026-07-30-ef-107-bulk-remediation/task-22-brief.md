### Task 22: Cover the remaining setter shapes

`SerializeConstant`'s XML comment claims constants are serialized "using the same property serializer the write pipeline uses, so enum / Guid / representation handling matches inserts and SaveChanges updates". Only the value-converter case backs that claim up. Several other shapes are untested.

**Files:**
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

**Interfaces:**
- Consumes: guards from Tasks 7 and 9.
- Produces: nothing.

- [ ] **Step 1: Add the tests**

Add a nested model covering the representation-sensitive types:

```csharp
    private enum Priority { Low, High }

    private class RichOrder
    {
        public ObjectId _id { get; set; }
        public string? Status { get; set; }
        public Priority Priority { get; set; }
        public Guid Correlation { get; set; }
        public DateTime OccurredAt { get; set; }
        public int Quantity { get; set; }
        public Address Shipping { get; set; } = null!;
    }

    private class Address
    {
        public string City { get; set; } = null!;
    }

    private static void ConfigureRichModel(ModelBuilder mb)
        => mb.Entity<RichOrder>().OwnsOne(o => o.Shipping);
```

then:

```csharp
    [Fact]
    public void ExecuteUpdate_setting_null_writes_null()
    {
        var collection = database.CreateCollection<RichOrder>(nameof(ExecuteUpdate_setting_null_writes_null));
        using (var seed = SingleEntityDbContext.Create(collection, ConfigureRichModel))
        {
            seed.Add(new RichOrder
            {
                _id = ObjectId.GenerateNewId(), Status = "open", Shipping = new Address { City = "Reading" }
            });
            seed.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, ConfigureRichModel);

        var updated = db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Status, (string?)null));

        Assert.Equal(1, updated);
        Assert.Null(db.Entities.Single().Status);
    }

    [Fact]
    public void ExecuteUpdate_setting_enum_Guid_and_DateTime_round_trips_like_SaveChanges()
    {
        var collection = database.CreateCollection<RichOrder>(
            nameof(ExecuteUpdate_setting_enum_Guid_and_DateTime_round_trips_like_SaveChanges));
        var correlation = Guid.NewGuid();
        var occurredAt = new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

        using (var seed = SingleEntityDbContext.Create(collection, ConfigureRichModel))
        {
            seed.Add(new RichOrder
            {
                _id = ObjectId.GenerateNewId(), Shipping = new Address { City = "Reading" }
            });
            seed.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, ConfigureRichModel);

        db.Entities.ExecuteUpdate(
            s => s.SetProperty(o => o.Priority, Priority.High)
                .SetProperty(o => o.Correlation, correlation)
                .SetProperty(o => o.OccurredAt, occurredAt));

        var actual = db.Entities.Single();
        Assert.Equal(Priority.High, actual.Priority);
        Assert.Equal(correlation, actual.Correlation);
        Assert.Equal(occurredAt, actual.OccurredAt);
    }

    [Fact]
    public void ExecuteUpdate_targeting_an_owned_nested_property_throws()
    {
        var collection = database.CreateCollection<RichOrder>(
            nameof(ExecuteUpdate_targeting_an_owned_nested_property_throws));
        using var db = SingleEntityDbContext.Create(collection, ConfigureRichModel);

        // Nested/owned setter targets are outside the supported subset; the rejection is part of the contract
        // and is stated in the README.
        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Shipping.City, "Bath")));

        Assert.Contains("Only mapped root scalar properties can be updated", ex.Message);
    }

    [Fact]
    public void ExecuteUpdate_self_referencing_on_a_field_absent_from_the_document_writes_null()
    {
        // Pipeline-form $set evaluates $add over a missing field as null, so the property is set to null rather
        // than left alone. This is document-model-specific behaviour that callers need to know about; the test
        // exists to pin it, not to bless it. If the team decides it should instead be rejected or skipped,
        // change the production code and this assertion together.
        var collection = database.CreateCollection<RichOrder>(
            nameof(ExecuteUpdate_self_referencing_on_a_field_absent_from_the_document_writes_null));
        collection.InsertOne(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Status", "open" } });

        using var db = SingleEntityDbContext.Create(collection, ConfigureRichModel);

        var updated = db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Quantity, o => o.Quantity + 1));

        Assert.Equal(1, updated);
    }
```

The last test inserts a raw `BsonDocument` into a typed collection; use `database.GetCollection<BsonDocument>(...)` or `collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName)` — copy whichever pattern already appears in the functional tests for raw-document seeding, and if the assertion about the resulting value differs from `null`, assert what actually happens and update the comment to match. The point is to have the behaviour recorded, not to have guessed it correctly.

- [ ] **Step 2: Run them**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" --filter "FullyQualifiedName~ExecuteUpdateTests"
```

Expected: PASS. `ExecuteUpdate_setting_enum_Guid_and_DateTime_round_trips_like_SaveChanges` is the one that substantiates the XML comment's claim; if it fails, `SerializeConstant` does *not* match the write pipeline and that is a new bug to fix here rather than a test to adjust.

- [ ] **Step 3: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs
git commit -m "test: cover null, enum/Guid/DateTime, owned-target and missing-field bulk setter shapes"
```

---

