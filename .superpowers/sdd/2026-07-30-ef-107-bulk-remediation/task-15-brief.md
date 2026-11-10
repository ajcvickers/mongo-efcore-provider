### Task 15: Support `OfType` and `Cast` by routing them through the two-phase read path

`OfType<T>()` is the idiomatic way to scope a polymorphic query in this provider — it is what `DiscriminatorTests` actually uses (`Returns_correct_entity_with_OfType_query`, `OfType_does_not_break_entity_serializer_association`), it is a first-class read-path operator (`TranslateOfType`), and the driver generates the `{_t: …}` filter for it via `MongoEFDiscriminator`. The classifier rejects it, so users must rewrite `db.Entities.OfType<Customer>().ExecuteDelete()` as `Where(e => e is Customer)`.

Classify it as **two-phase** rather than single-command: phase 1 reuses the read-path translation, so the discriminator filter comes for free. Making it single-command would require `BuildFilter` to synthesize `_t` itself — more work, easy to get wrong, and getting it wrong on a delete destroys the wrong documents.

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs`
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsInheritanceTests.cs`

**Interfaces:**
- Consumes: the classifier shape from Tasks 13 and 14.
- Produces: `ClassifyBulkSource` returns `TwoPhase` for any chain containing `QueryableMethods.OfType` or `QueryableMethods.Cast`.

- [ ] **Step 1: Write the failing tests**

Create `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsInheritanceTests.cs` with the standard licence header, then:

```csharp
// Bulk operations against a TPH hierarchy. This is the case where a classification mistake destroys the
// wrong documents rather than returning the wrong rows, so it is covered explicitly.

#if !EF8

using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

[XUnitCollection("QueryTests")]
public class BulkOperationsInheritanceTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private abstract class Animal
    {
        public ObjectId _id { get; set; }
        public string Kind { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    private class Cat : Animal;

    private class Dog : Animal;

    private static void Configure(ModelBuilder mb)
        => mb.Entity<Animal>()
            .HasDiscriminator(a => a.Kind)
            .HasValue<Cat>("cat")
            .HasValue<Dog>("dog");

    private SingleEntityDbContext<Animal> CreateSeeded(string name)
    {
        var collection = database.CreateCollection<Animal>(name);
        using (var seed = SingleEntityDbContext.Create(collection, Configure))
        {
            seed.AddRange(
                new Cat { _id = ObjectId.GenerateNewId(), Name = "Tom" },
                new Cat { _id = ObjectId.GenerateNewId(), Name = "Felix" },
                new Dog { _id = ObjectId.GenerateNewId(), Name = "Rex" });
            seed.SaveChanges();
        }

        return SingleEntityDbContext.Create(collection, Configure);
    }

    [Fact]
    public void ExecuteDelete_with_OfType_deletes_only_that_type()
    {
        using var db = CreateSeeded(nameof(ExecuteDelete_with_OfType_deletes_only_that_type));

        var deleted = db.Entities.OfType<Cat>().ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Single(db.Entities);
        Assert.IsType<Dog>(db.Entities.Single());
    }

    [Fact]
    public void ExecuteUpdate_with_OfType_updates_only_that_type()
    {
        using var db = CreateSeeded(nameof(ExecuteUpdate_with_OfType_updates_only_that_type));

        var updated = db.Entities.OfType<Cat>().ExecuteUpdate(s => s.SetProperty(a => a.Name, "renamed"));

        Assert.Equal(2, updated);
        Assert.Equal("Rex", db.Entities.OfType<Dog>().Single().Name);
    }

    [Fact]
    public void ExecuteDelete_with_an_is_predicate_deletes_only_that_type_on_the_single_command_path()
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Animal>(
            nameof(ExecuteDelete_with_an_is_predicate_deletes_only_that_type_on_the_single_command_path));
        using (var seed = SingleEntityDbContext.Create(collection, Configure))
        {
            seed.AddRange(
                new Cat { _id = ObjectId.GenerateNewId(), Name = "Tom" },
                new Dog { _id = ObjectId.GenerateNewId(), Name = "Rex" });
            seed.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, Configure, loggerFactory: loggerFactory);

        var deleted = db.Entities.Where(a => a is Cat).ExecuteDelete();

        Assert.Equal(1, deleted);
        Assert.IsType<Dog>(db.Entities.Single());
        Assert.Contains("Strategy=SingleCommand", spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete));
    }
}

#endif
```

Match the `SingleEntityDbContext.Create` overloads that exist — copy the call shapes used in `DiscriminatorTests.cs` and adapt. The `SingleEntityDbContext<Animal>` return type must match whatever `Create` returns in this repo; read it and use the real type.

- [ ] **Step 2: Run them to verify the `OfType` ones fail**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsInheritanceTests"
```

Expected: the two `OfType` tests FAIL with "The 'OfType' operator is not supported in a bulk delete or update"; the `is`-predicate test PASSES.

- [ ] **Step 3: Admit `OfType` and `Cast` as two-phase**

In `ClassifyBulkSource`, add a branch before the `else` that throws:

```csharp
            else if (definition == QueryableMethods.OfType || definition == QueryableMethods.Cast)
            {
                // Type scoping is expressed by the driver as a { _t: ... } filter, which only the read path
                // knows how to build. Route through two-phase so phase 1 reuses that translation rather than
                // reimplementing discriminator filtering in BuildFilter — getting that wrong on a delete would
                // destroy documents of the wrong type.
                requiresTwoPhase = true;
            }
```

If `QueryableMethods.Cast` does not exist on the pinned EF version, resolve it the same way as the ordering operators (reflection over `typeof(Queryable)` for the single-parameter generic `Cast`), and note in a comment which versions needed the fallback.

- [ ] **Step 4: Run the tests, including the discriminator regression suite**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsInheritanceTests|FullyQualifiedName~DiscriminatorTests"
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF9" \
  --filter "FullyQualifiedName~BulkOperationsInheritanceTests"
```

Expected: PASS. If phase 1 does not in fact emit the `_t` filter — the `OfType` delete removes the `Dog` too — **stop and do not weaken the test**. That would mean the read path's discriminator filtering does not survive the `CreateQuery<BsonDocument>` reinterpretation in `BuildIdDocumentQuery`, which is a much more serious finding than this task; revert the classifier change so `OfType` stays rejected, keep the tests as `Assert.Throws` documenting the limitation, and raise it separately.

- [ ] **Step 5: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoQueryableMethodTranslatingExpressionVisitor.cs \
        tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsInheritanceTests.cs
git commit -m "feat: support OfType/Cast in bulk operations via the two-phase read path"
```

---

## Phase 5 — Performance

