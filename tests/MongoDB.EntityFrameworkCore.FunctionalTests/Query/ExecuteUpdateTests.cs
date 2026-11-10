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

#if !EF8

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

public class ExecuteUpdateTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Order
    {
        public ObjectId _id { get; set; }
        public string Status { get; set; } = null!; // mapped to "state" via HasElementName
        public int Quantity { get; set; }
        [NotMapped] public string Unmapped { get; set; } = null!;
    }

    private class OrderWithConverter
    {
        public ObjectId _id { get; set; }
        public int Quantity { get; set; } // stored as string via HasConversion
    }

    private static readonly Action<ModelBuilder> ConfigureConverterModel = mb =>
    {
        mb.Entity<OrderWithConverter>().Property(e => e.Quantity).HasConversion<string>();
    };

    private static readonly Action<ModelBuilder> ConfigureModel = mb =>
    {
        mb.Entity<Order>().Property(e => e.Status).HasElementName("state");
    };

    private class VersionedOrder
    {
        public ObjectId _id { get; set; }
        public string Status { get; set; } = null!;
        public byte[] Version { get; set; } = null!;
    }

    private static readonly Action<ModelBuilder> ConfigureVersionedModel = mb =>
    {
        mb.Entity<VersionedOrder>().Property(e => e.Version).IsRowVersion();
    };

    private abstract class Animal
    {
        public ObjectId _id { get; set; }
        public string Kind { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    private class Cat : Animal;

    private class Dog : Animal;

    private static readonly Action<ModelBuilder> ConfigureHierarchyModel = mb =>
    {
        mb.Entity<Animal>().HasDiscriminator(a => a.Kind).HasValue<Cat>("cat").HasValue<Dog>("dog");
        mb.Entity<Cat>();
        mb.Entity<Dog>();
    };

    // A self-referencing one-to-many within one collection (same shape as StaffMember in CrossCollectionIncludeTests).
    private class Employee
    {
        public ObjectId _id { get; set; }
        public int Rank { get; set; }
        public ObjectId? ManagerId { get; set; }
        public Employee? Manager { get; set; }
        public List<Employee> DirectReports { get; set; } = [];
    }

    private static readonly Action<ModelBuilder> ConfigureEmployeeModel = mb =>
    {
        mb.Entity<Employee>().HasOne(e => e.Manager).WithMany(e => e.DirectReports).HasForeignKey(e => e.ManagerId);
    };

    private SingleEntityDbContext<Order> CreateSeededContext([CallerMemberName] string? collectionName = null)
    {
        var collection = database.CreateCollection<Order>(collectionName);

        using (var seedDb = SingleEntityDbContext.Create(collection, ConfigureModel))
        {
            seedDb.AddRange(
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 10 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 20 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "closed", Quantity = 30 });
            seedDb.SaveChanges();
        }

        return SingleEntityDbContext.Create(collection, ConfigureModel);
    }

    [Fact]
    public void ExecuteUpdate_constant_set_updates_matching_and_returns_count()
    {
        using var db = CreateSeededContext();

        var updated = db.Entities
            .Where(o => o.Status == "open")
            .ExecuteUpdate(s => s.SetProperty(o => o.Status, "shipped"));

        Assert.Equal(2, updated);
        Assert.Equal(2, db.Entities.Count(o => o.Status == "shipped"));
        Assert.Equal(1, db.Entities.Count(o => o.Status == "closed"));
    }

    [Fact]
    public void ExecuteUpdate_returns_matched_count_even_when_values_already_match()
    {
        using var db = CreateSeededContext();

        // Both matched docs already have Status "open", so the server reports ModifiedCount 0;
        // ExecuteUpdate must still return the matched-row count (2), matching EF Core's contract
        // (relational providers count a row even when SET writes its existing value).
        var updated = db.Entities
            .Where(o => o.Status == "open")
            .ExecuteUpdate(s => s.SetProperty(o => o.Status, "open"));

        Assert.Equal(2, updated);
    }

    [Fact]
    public void ExecuteUpdate_with_ef_property_setter_updates_matching()
    {
        using var db = CreateSeededContext();

        // The setter target is EF.Property<string>(o, "Status") rather than a direct member access.
        var updated = db.Entities
            .Where(o => o.Status == "open")
            .ExecuteUpdate(s => s.SetProperty(o => EF.Property<string>(o, nameof(Order.Status)), "shipped"));

        Assert.Equal(2, updated);
        Assert.Equal(2, db.Entities.Count(o => o.Status == "shipped"));
    }

    [Fact]
    public void ExecuteUpdate_with_captured_variable_set_value()
    {
        using var db = CreateSeededContext();

        var newStatus = "processing";
        var updated = db.Entities
            .Where(o => o.Quantity >= 20)
            .ExecuteUpdate(s => s.SetProperty(o => o.Status, newStatus));

        Assert.Equal(2, updated);
        Assert.Equal(2, db.Entities.Count(o => o.Status == "processing"));
    }

    [Fact]
    public void ExecuteUpdate_self_referencing_increments_matched_docs()
    {
        using var db = CreateSeededContext();

        var updated = db.Entities
            .Where(o => o.Status == "open")
            .ExecuteUpdate(s => s.SetProperty(o => o.Quantity, o => o.Quantity + 1));

        Assert.Equal(2, updated);
        var quantities = db.Entities.OrderBy(o => o.Quantity).Select(o => o.Quantity).ToList();
        // open: 10 -> 11, 20 -> 21; closed unchanged: 30
        Assert.Equal(new[] { 11, 21, 30 }, quantities);
    }

    [Fact]
    public async Task ExecuteUpdateAsync_self_referencing_increments_matched_docs()
    {
        using var db = CreateSeededContext();

        var updated = await db.Entities
            .Where(o => o.Quantity >= 20)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Quantity, o => o.Quantity * 2));

        Assert.Equal(2, updated);
        var quantities = db.Entities.OrderBy(o => o.Quantity).Select(o => o.Quantity).ToList();
        // 10 unchanged; 20 -> 40; 30 -> 60
        Assert.Equal(new[] { 10, 40, 60 }, quantities);
    }

    [Fact]
    public void ExecuteUpdate_mixed_constant_and_self_referencing_in_one_call()
    {
        using var db = CreateSeededContext();

        var updated = db.Entities
            .Where(o => o.Status == "open")
            .ExecuteUpdate(s => s
                .SetProperty(o => o.Status, "bumped")
                .SetProperty(o => o.Quantity, o => o.Quantity + 5));

        Assert.Equal(2, updated);
        var bumped = db.Entities.Where(o => o.Status == "bumped").OrderBy(o => o.Quantity).Select(o => o.Quantity).ToList();
        Assert.Equal(new[] { 15, 25 }, bumped);
        Assert.Equal(1, db.Entities.Count(o => o.Status == "closed"));
    }

    [Fact]
    public void ExecuteUpdate_without_predicate_updates_everything()
    {
        using var db = CreateSeededContext();

        var updated = db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Status, "all"));

        Assert.Equal(3, updated);
        Assert.Equal(3, db.Entities.Count(o => o.Status == "all"));
    }

    [Fact]
    public void ExecuteUpdate_with_OrderBy_Take_updates_only_targeted_rows()
    {
        using var db = CreateSeededContext();

        // Two-phase: phase 1 reads the top-2 _ids (ordered by Quantity), phase 2 applies the update.
        var updated = db.Entities.OrderBy(o => o.Quantity).Take(2)
            .ExecuteUpdate(s => s.SetProperty(o => o.Status, "archived"));

        Assert.Equal(2, updated);
        Assert.Equal(2, db.Entities.Count(o => o.Status == "archived"));
        // The highest-quantity row must be untouched.
        Assert.Equal(1, db.Entities.Count(o => o.Status == "closed"));
    }

    [Fact]
    public void ExecuteUpdate_two_phase_self_referencing_setter()
    {
        using var db = CreateSeededContext();

        // Two-phase: targets the single lowest-quantity document and increments its Quantity by 5.
        var updated = db.Entities.OrderBy(o => o.Quantity).Take(1)
            .ExecuteUpdate(s => s.SetProperty(o => o.Quantity, o => o.Quantity + 5));

        Assert.Equal(1, updated);
        // Lowest seeded Quantity is 10; +5 = 15.
        Assert.Contains(db.Entities.ToList(), o => o.Quantity == 15);
    }

    [Fact]
    public void ExecuteUpdate_two_phase_rolls_back_inside_user_transaction()
    {
        using var db = CreateSeededContext();

        using (var tx = db.Database.BeginTransaction())
        {
            db.Entities.OrderBy(o => o.Quantity).Take(2)
                .ExecuteUpdate(s => s.SetProperty(o => o.Status, "archived"));
            tx.Rollback();
        }

        Assert.Equal(0, db.Entities.Count(o => o.Status == "archived"));
    }

    [Fact]
    public void ExecuteUpdate_two_phase_with_AutoTransactionBehavior_Never_throws_and_changes_nothing()
    {
        using var db = CreateSeededContext();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.OrderBy(o => o.Quantity).Take(2)
                .ExecuteUpdate(s => s.SetProperty(o => o.Status, "archived")));

        Assert.Contains("AutoTransactionBehavior", AllMessages(ex));
        Assert.Equal(0, db.Entities.Count(o => o.Status == "archived")); // nothing updated
    }

#if EF9
    // EF10 changed the setter parameter to an Action and validates "at least one SetProperty" upstream,
    // so the empty-setter guard only applies to the EF9 translation path (where the raw lambda reaches us).
    [Fact]
    public void ExecuteUpdate_with_no_set_property_throws()
    {
        using var db = CreateSeededContext();

        // A setter lambda with no SetProperty call must be rejected, not run as a no-op updateMany.
        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s));
        Assert.Contains("SetProperty", AllMessages(ex));
    }
#endif

    [Fact]
    public void ExecuteUpdate_targeting_unmapped_property_throws()
    {
        using var db = CreateSeededContext();

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Unmapped, "x")));
        Assert.Contains("Only mapped root scalar properties can be updated", AllMessages(ex));
    }

    // EF11 reports the translation failure details as the inner exception rather than in the message.
    private static string AllMessages(Exception exception)
        => string.Join(" ", exception.InnerException is null
            ? [exception.Message]
            : [exception.Message, AllMessages(exception.InnerException)]);

    private static int Foo(int quantity) => quantity + 1;

    [Fact]
    public void ExecuteUpdate_unsupported_setter_method_reports_the_expression_not_a_reflection_wrapper()
    {
        using var db = CreateSeededContext();

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Quantity, o => Foo(o.Quantity))));

        Assert.DoesNotContain("target of an invocation", AllMessages(ex));
        Assert.Contains("Foo", AllMessages(ex));
        Assert.Equal(3, db.Entities.Count(o => o.Quantity > 0)); // nothing updated
    }

    [Fact]
    public void ExecuteUpdate_unsupported_setter_GetHashCode_reports_the_expression_not_a_reflection_wrapper()
    {
        using var db = CreateSeededContext();

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Quantity, o => o.Status.GetHashCode())));

        Assert.DoesNotContain("target of an invocation", AllMessages(ex));
        Assert.Contains("GetHashCode", AllMessages(ex));
    }

    [Fact]
    public void ExecuteUpdate_self_referencing_value_converter_property_throws()
    {
        var collection = database.CreateCollection<OrderWithConverter>();
        using var db = SingleEntityDbContext.Create(collection, ConfigureConverterModel);

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Quantity, o => o.Quantity + 1)));
        Assert.Contains("Self-referencing ExecuteUpdate on property", AllMessages(ex));
        Assert.Contains("value converter", AllMessages(ex));
    }

    [Fact]
    public void ExecuteUpdate_constant_set_on_value_converter_property_persists_converted_shape()
    {
        var collection = database.CreateCollection<OrderWithConverter>();
        using (var seedDb = SingleEntityDbContext.Create(collection, ConfigureConverterModel))
        {
            seedDb.AddRange(
                new OrderWithConverter { _id = ObjectId.GenerateNewId(), Quantity = 1 },
                new OrderWithConverter { _id = ObjectId.GenerateNewId(), Quantity = 2 });
            seedDb.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, ConfigureConverterModel);
        var updated = db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Quantity, 99));
        Assert.Equal(2, updated);

        // The constant must be serialized through the converter-aware property path, so it persists
        // as a BSON string ("99"), not a raw int — matching how SaveChanges writes the property.
        var raw = collection.Database
            .GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName)
            .Find(FilterDefinition<BsonDocument>.Empty).ToList();
        Assert.Equal(2, raw.Count);
        Assert.All(raw, d => Assert.Equal(BsonType.String, d["Quantity"].BsonType));
        Assert.All(raw, d => Assert.Equal("99", d["Quantity"].AsString));

        // And it round-trips back through EF as the original CLR int.
        Assert.All(db.Entities.ToList(), o => Assert.Equal(99, o.Quantity));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteUpdate_logs_ExecutedBulkUpdate_event_with_count(bool async)
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(nameof(ExecuteUpdate_logs_ExecutedBulkUpdate_event_with_count), async);

        using (var seedDb = SingleEntityDbContext.Create(collection, ConfigureModel))
        {
            seedDb.AddRange(
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 10 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 20 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "closed", Quantity = 30 });
            seedDb.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, loggerFactory, ConfigureModel);

        var updated = async
            ? await db.Entities.Where(o => o.Status == "open")
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "shipped"))
            : db.Entities.Where(o => o.Status == "open")
                .ExecuteUpdate(s => s.SetProperty(o => o.Status, "shipped"));

        Assert.Equal(2, updated);

        var executingMessage = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkUpdate);
        Assert.Contains("Executing Bulk Update", executingMessage);
        Assert.Contains($"Collection='{collection.CollectionNamespace}'", executingMessage);

        var executedMessage = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutedBulkUpdate);
        Assert.Contains("Executed Bulk Update", executedMessage);
        Assert.Contains($"Collection='{collection.CollectionNamespace}'", executedMessage);
        Assert.Contains("Matched=2", executedMessage);
        Assert.Contains("Modified=2", executedMessage);
        Assert.Contains("Strategy=SingleCommand", executingMessage);
        Assert.Contains("Targets=-", executingMessage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteUpdate_event_reports_both_matched_and_modified_counts(bool async)
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(
            nameof(ExecuteUpdate_event_reports_both_matched_and_modified_counts), async);

        using (var seedDb = SingleEntityDbContext.Create(collection, ConfigureModel))
        {
            seedDb.AddRange(
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 10 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 20 });
            seedDb.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, loggerFactory, ConfigureModel);

        // Both documents match, but the write changes nothing: matched is 2, modified is 0.
        var updated = async
            ? await db.Entities.Where(o => o.Status == "open")
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "open"))
            : db.Entities.Where(o => o.Status == "open")
                .ExecuteUpdate(s => s.SetProperty(o => o.Status, "open"));

        Assert.Equal(2, updated);

        var executedMessage = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutedBulkUpdate);
        Assert.Contains("Matched=2", executedMessage);
        Assert.Contains("Modified=0", executedMessage);
    }

    [Fact]
    public void ExecuteUpdate_two_phase_empty_target_returns_zero()
    {
        using var db = CreateSeededContext();

        // Two-phase phase 1 finds no targets, so phase 2 is skipped and nothing is written.
        var updated = db.Entities.Where(o => o.Status == "nonexistent").OrderBy(o => o.Quantity).Take(5)
            .ExecuteUpdate(s => s.SetProperty(o => o.Status, "x"));

        Assert.Equal(0, updated);
        Assert.Equal(0, db.Entities.Count(o => o.Status == "x"));
    }

    [Fact]
    public void ExecuteUpdate_two_phase_returns_matched_count_even_when_values_already_match()
    {
        using var db = CreateSeededContext();

        // Two-phase (OrderBy + Take) targeting the two "open" docs and setting Status to its existing value:
        // the server reports ModifiedCount 0, but ExecuteUpdate must still return the matched-row count (2)
        // through the { _id: { $in: ... } } UpdateMany path, matching EF Core's contract.
        var updated = db.Entities.Where(o => o.Status == "open").OrderBy(o => o.Quantity).Take(2)
            .ExecuteUpdate(s => s.SetProperty(o => o.Status, "open"));

        Assert.Equal(2, updated);
    }

    [Fact]
    public void ExecuteUpdate_with_Where_and_Skip_Take_scopes_to_window()
    {
        using var db = CreateSeededContext();

        // "open" docs are Quantity 10 and 20; Skip(1).Take(1) over ascending Quantity targets only the 20.
        var updated = db.Entities.Where(o => o.Status == "open").OrderBy(o => o.Quantity).Skip(1).Take(1)
            .ExecuteUpdate(s => s.SetProperty(o => o.Status, "windowed"));

        Assert.Equal(1, updated);
        Assert.Equal(1, db.Entities.Count(o => o.Status == "windowed"));
        Assert.Contains(db.Entities.ToList(), o => o.Status == "windowed" && o.Quantity == 20);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteUpdate_two_phase_logs_ExecutingBulkUpdate_with_target_count(bool async)
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(
            nameof(ExecuteUpdate_two_phase_logs_ExecutingBulkUpdate_with_target_count), async);

        using (var seedDb = SingleEntityDbContext.Create(collection, ConfigureModel))
        {
            seedDb.AddRange(
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 10 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 20 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "closed", Quantity = 30 });
            seedDb.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, loggerFactory, ConfigureModel);

        // Two-phase path (OrderBy + Take), exercised both sync and async.
        var updated = async
            ? await db.Entities.OrderBy(o => o.Quantity).Take(2)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "archived"))
            : db.Entities.OrderBy(o => o.Quantity).Take(2)
                .ExecuteUpdate(s => s.SetProperty(o => o.Status, "archived"));

        Assert.Equal(2, updated);

        var executingMessage = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkUpdate);
        Assert.Contains("Executing Bulk Update", executingMessage);
        Assert.Contains($"Collection='{collection.CollectionNamespace}'", executingMessage);
        Assert.Contains("Strategy=TwoPhase", executingMessage);
        Assert.Contains("Targets=2", executingMessage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteUpdate_two_phase_auto_transaction_logs_transaction_lifecycle(bool async)
    {
        var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
        var collection = database.CreateCollection<Order>(
            nameof(ExecuteUpdate_two_phase_auto_transaction_logs_transaction_lifecycle), async);

        using (var seedDb = SingleEntityDbContext.Create(collection, ConfigureModel))
        {
            seedDb.AddRange(
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 10 },
                new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 20 });
            seedDb.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, loggerFactory, ConfigureModel);

        // No explicit BeginTransaction: the two-phase path (OrderBy + Take) auto-starts one
        // and must log both its begin and commit lifecycle events.
        var updated = async
            ? await db.Entities.OrderBy(o => o.Quantity).Take(1)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "archived"))
            : db.Entities.OrderBy(o => o.Quantity).Take(1)
                .ExecuteUpdate(s => s.SetProperty(o => o.Status, "archived"));

        Assert.Equal(1, updated);

        var startedMessage = spyLogger.GetLogMessageByEventId(MongoEventId.TransactionStarted);
        Assert.Contains("Began transaction", startedMessage);
        var committedMessage = spyLogger.GetLogMessageByEventId(MongoEventId.TransactionCommitted);
        Assert.Contains("Committed transaction", committedMessage);
    }

    [Fact]
    public void ExecuteUpdate_targeting_the_primary_key_throws_a_translation_failure()
    {
        using var db = CreateSeededContext();

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(o => o._id, ObjectId.GenerateNewId())));

        Assert.Contains("primary key", ex.Message);
        Assert.Equal(3, db.Entities.Count());
    }

    [Fact]
    public void ExecuteUpdate_targeting_the_discriminator_throws_a_translation_failure_and_writes_nothing()
    {
        var collection = database.CreateCollection<Animal>();
        using (var seed = SingleEntityDbContext.Create(collection, ConfigureHierarchyModel))
        {
            seed.AddRange(new Cat { _id = ObjectId.GenerateNewId(), Name = "Tom" }, new Dog { _id = ObjectId.GenerateNewId(), Name = "Rex" });
            seed.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, ConfigureHierarchyModel);

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.ExecuteUpdate(s => s.SetProperty(a => a.Kind, "dog")));
        Assert.Contains("discriminator", ex.Message);

        // A query rooted on a derived type is guarded the same way.
        var derived = Assert.Throws<InvalidOperationException>(
            () => db.Entities.Where(a => a is Cat).ExecuteUpdate(s => s.SetProperty(a => a.Kind, "dog")));
        Assert.Contains("discriminator", derived.Message);

        var raw = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        Assert.Equal(1, raw.CountDocuments(new BsonDocument("_t", "cat"))); // the discriminator is stored as _t
        Assert.Equal(1, raw.CountDocuments(new BsonDocument("_t", "dog")));
    }

    [Fact]
    public void ExecuteUpdate_can_set_a_concurrency_token()
    {
        // A bulk update neither checks nor protects concurrency tokens, so setting one explicitly is a supported use.
        var collection = database.CreateCollection<VersionedOrder>();
        using (var seed = SingleEntityDbContext.Create(collection, ConfigureVersionedModel))
        {
            seed.Add(new VersionedOrder { _id = ObjectId.GenerateNewId(), Status = "open" });
            seed.SaveChanges();
        }

        var newVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        using (var db = SingleEntityDbContext.Create(collection, ConfigureVersionedModel))
        {
            Assert.Equal(1, db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Version, newVersion)));
        }

        var raw = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName)
            .Find(FilterDefinition<BsonDocument>.Empty).Single();
        Assert.Equal(newVersion, raw["Version"].AsBsonBinaryData.Bytes);
    }

    // A self-referencing navigation inside a setter value gives the value body two Employee-typed parameters (the setter's own `e`
    // and the nested lambda's `r`). If that ever reached the renderer's type-based parameter rebinding, the two would collapse onto
    // one and the update would degenerate to a silent no-op (Rank set to its own value). It is rejected loudly instead: today by the
    // translator ("could not be translated"), and, should that layer start accepting the shape, by the
    // MongoBulkPlanBuilder.RebindSetterValueParameters guard (unit-tested in BulkSetterRebindingTests).
    [Fact]
    public void ExecuteUpdate_self_referencing_setter_with_nested_entity_typed_parameter_is_rejected_not_silently_wrong()
    {
        var collection = database.CreateCollection<Employee>();
        var managerId = ObjectId.GenerateNewId();
        using (var seed = SingleEntityDbContext.Create(collection, ConfigureEmployeeModel))
        {
            seed.AddRange(
                new Employee { _id = managerId, Rank = 1 },
                new Employee { _id = ObjectId.GenerateNewId(), Rank = 5, ManagerId = managerId },
                new Employee { _id = ObjectId.GenerateNewId(), Rank = 9, ManagerId = managerId });
            seed.SaveChanges();
        }

        using var db = SingleEntityDbContext.Create(collection, ConfigureEmployeeModel);

        var ex = Assert.Throws<InvalidOperationException>(
            () => db.Entities.Where(e => e._id == managerId)
                .ExecuteUpdate(s => s.SetProperty(e => e.Rank, e => e.DirectReports.Max(r => r.Rank))));

        Assert.True(
            ex.Message.Contains("could not be translated") || ex.Message.Contains("more than one"),
            $"Expected a loud rejection of the shape, got: {ex.Message}");

        // And nothing was written.
        using var check = SingleEntityDbContext.Create(collection, ConfigureEmployeeModel);
        Assert.Equal(1, check.Entities.Single(e => e._id == managerId).Rank);
    }
}

#endif
