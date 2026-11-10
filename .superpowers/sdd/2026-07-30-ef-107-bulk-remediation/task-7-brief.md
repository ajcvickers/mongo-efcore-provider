### Task 7: Guard provider-owned setter targets, and derive the `$set` key from the serialization info

`BuildSetter` accepts any mapped root scalar, so a setter can target the discriminator (element `_t` — silently rewriting a document's type so the model can no longer materialize it), the primary key (element `_id` — rejected by the server as an immutable-field violation, surfacing as a raw driver exception), or a concurrency token (defeating optimistic concurrency for subsequent `SaveChanges`).

Separately, `BuildUpdate` keys `$set` with `property.GetElementName()`, while `BsonSerializerFactory.GetPropertySerializationInfo` maps composite-primary-key parts to the path `["_id", elementName]`. The two disagree. The guard above closes today's only reachable instance of that disagreement (composite-key parts are primary-key properties), so this step is defence-in-depth: it makes one function authoritative for "where does this property live in the document" so a future property kind with a path cannot silently write to the wrong field.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Modify: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `private static string GetSetElementKey(IProperty property)` in `MongoShapedQueryCompilingExpressionVisitor`, used by both branches of `BuildUpdate`. `BuildSetter` gains rejections; its signature is unchanged.

**Judgement call to confirm with the team before merging:** rejecting a setter on a concurrency token / row version is a behaviour decision, not a bug fix. Relational EF permits it. The argument for rejecting is that bulk operations already skip concurrency *checking* (documented in the README), so permitting a bulk *write* to the token silently disarms optimistic concurrency for every later `SaveChanges` on that document. If the team prefers to permit it, delete that one guard and its test and record the decision in the README bullet instead.

- [ ] **Step 1: Write the failing tests**

In `ExecuteUpdateTests.cs` add three tests. `Order` in this file already has an `ObjectId _id`; the discriminator and row-version cases need a small dedicated model, declared as nested types next to the existing test models in the file:

```csharp
    private class VersionedOrder
    {
        public ObjectId _id { get; set; }
        public string Status { get; set; } = null!;
        public byte[] Version { get; set; } = null!;
    }

    private static void ConfigureVersionedModel(ModelBuilder mb)
        => mb.Entity<VersionedOrder>().Property(e => e.Version).IsRowVersion();

    private abstract class Animal
    {
        public ObjectId _id { get; set; }
        public string Kind { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    private class Cat : Animal;

    private class Dog : Animal;

    private static void ConfigureHierarchyModel(ModelBuilder mb)
        => mb.Entity<Animal>()
            .HasDiscriminator(a => a.Kind)
            .HasValue<Cat>("cat")
            .HasValue<Dog>("dog");
```

```csharp
    [Fact]
    public void ExecuteUpdate_targeting_the_primary_key_throws()
    {
        using var db = CreateSeededContext();

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(o => o._id, ObjectId.GenerateNewId())));

        Assert.Contains("primary key", ex.Message);
    }

    [Fact]
    public void ExecuteUpdate_targeting_the_discriminator_throws()
    {
        var collection = database.CreateCollection<Animal>();
        using var db = SingleEntityDbContext.Create(collection, ConfigureHierarchyModel);

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(a => a.Kind, "dog")));

        Assert.Contains("discriminator", ex.Message);
    }

    [Fact]
    public void ExecuteUpdate_targeting_a_concurrency_token_throws()
    {
        var collection = database.CreateCollection<VersionedOrder>();
        using var db = SingleEntityDbContext.Create(collection, ConfigureVersionedModel);

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Version, new byte[8])));

        Assert.Contains("concurrency token", ex.Message);
    }
```

- [ ] **Step 2: Run them to verify they fail**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdate_targeting_the_primary_key_throws|FullyQualifiedName~ExecuteUpdate_targeting_the_discriminator_throws|FullyQualifiedName~ExecuteUpdate_targeting_a_concurrency_token_throws"
```

Expected: all three FAIL. The primary-key one fails with a `MongoCommandException` about the immutable `_id` field rather than an `InvalidOperationException`; the other two fail because no exception is thrown at all.

- [ ] **Step 3: Add the guards**

In `MongoQueryableMethodTranslatingExpressionVisitor.BuildSetter`, immediately after the `if (property == null)` block:

```csharp
        // Reject setters on fields the provider owns. Without these guards a bulk update can rewrite a
        // document's type (the discriminator maps to _t), attempt an immutable-_id write that only fails at
        // the server, or silently disarm optimistic concurrency for every later SaveChanges on the document.
        if (property.IsPrimaryKey())
        {
            RejectSetterTarget(
                mongoQueryExpression, property,
                "it is part of the primary key, which maps to the immutable '_id' element");
        }

        if (property == entityType.FindDiscriminatorProperty())
        {
            RejectSetterTarget(
                mongoQueryExpression, property,
                "it is the type discriminator; changing it would change the entity type of the stored document");
        }

        if (property.IsConcurrencyToken)
        {
            RejectSetterTarget(
                mongoQueryExpression, property,
                "it is a concurrency token. Bulk operations do not check concurrency tokens, so writing one "
                + "would disable optimistic concurrency for later SaveChanges calls on the same document");
        }
```

and add the helper next to `ThrowBulkSourceNotSupported`:

```csharp
    [DoesNotReturn]
    private void RejectSetterTarget(MongoQueryExpression mongoQueryExpression, IProperty property, string reason)
    {
        AddTranslationErrorDetails(
            $"The property '{property.DeclaringType.DisplayName()}.{property.Name}' cannot be updated by a bulk "
            + $"update because {reason}.");
        throw new InvalidOperationException(
            CoreStrings.NonQueryTranslationFailedWithDetails(
                mongoQueryExpression.CapturedExpression?.Print(), TranslationErrorDetails));
    }
```

- [ ] **Step 4: Make the `$set` key authoritative**

In `MongoShapedQueryCompilingExpressionVisitor.cs`, add next to `SerializeConstant`:

```csharp
    /// <summary>
    /// Returns the dotted document path a property is stored at, so a <c>$set</c> key always agrees with where
    /// the serializer reads the value from. Most properties are a single element name, but
    /// <see cref="BsonSerializerFactory.GetPropertySerializationInfo"/> maps some — composite-primary-key parts —
    /// to a path such as <c>_id.OrderID</c>. Using <c>GetElementName()</c> alone would create a new top-level
    /// field instead of updating the stored one.
    /// </summary>
    private static string GetSetElementKey(IProperty property)
    {
        var info = BsonSerializerFactory.GetPropertySerializationInfo(property);
        return info.ElementPath is { Count: > 0 } path ? string.Join('.', path) : info.ElementName;
    }
```

Then replace all four uses of `setter.Property.GetElementName()` in `BuildUpdate` with `GetSetElementKey(setter.Property)`.

If `BsonSerializationInfo` on the pinned driver has no `ElementPath` property, use whichever member `CreateWithPath` populates — check with `grep -rn "ElementPath\|CreateWithPath" ~/.nuget/packages/mongodb.driver/*/lib/**/*.xml` or by inspecting the type in the IDE — and keep the same fallback shape.

- [ ] **Step 5: Run the new tests and the whole bulk suite**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~ExecuteUpdateTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
```

Expected: PASS. The conformance suite is the one at risk here — if a `NorthwindBulkUpdates` case sets a key or discriminator property it will now fail. If that happens, that case moves from `base.*` to `AssertTranslationFailed` with a `// Fails: bulk setter targets a provider-owned property EF-X016` comment, and `docs/failing-spec-tests.md` is updated in Task 25.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/ \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs
git commit -m "fix: reject bulk setters on key, discriminator and concurrency-token properties"
```

---

