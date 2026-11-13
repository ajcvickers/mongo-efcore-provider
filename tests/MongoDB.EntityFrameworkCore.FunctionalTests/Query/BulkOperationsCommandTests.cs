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

// Pins the command the bulk path actually sends. Counts and before/after state cannot distinguish a correct $set from one written
// to the wrong element, which is what these assertions are for. Command content is sensitive, so it appears only when
// EnableSensitiveDataLogging is on: every call site (single command and by-id, sync and async, delete and update) is checked both
// ways.

#if !EF8

using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

public class BulkOperationsCommandTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Order
    {
        public ObjectId _id { get; set; }
        public string Status { get; set; } = null!;
        public int Quantity { get; set; }
    }

    private IMongoCollection<Order> SeedCollection(string name, bool async)
    {
        var collection = database.CreateCollection<Order>(name, async);
        collection.InsertMany(
        [
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 10 },
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 20 },
            new Order { _id = ObjectId.GenerateNewId(), Status = "closed", Quantity = 30 }
        ]);
        return collection;
    }

    private static SingleEntityDbContext<Order> CreateContext(
        IMongoCollection<Order> collection, Microsoft.Extensions.Logging.LoggerFactory loggerFactory, bool sensitive)
        => SingleEntityDbContext.Create(
            collection, loggerFactory, optionsBuilderAction: sensitive ? o => o.EnableSensitiveDataLogging() : null);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Single_command_delete_logs_its_filter_only_with_sensitive_logging(bool async)
    {
        foreach (var sensitive in new[] { false, true })
        {
            var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
            var collection = SeedCollection($"{nameof(Single_command_delete_logs_its_filter_only_with_sensitive_logging)}_{sensitive}", async);
            using var db = CreateContext(collection, loggerFactory, sensitive);

            var deleted = async
                ? await db.Entities.Where(o => o.Status == "open").ExecuteDeleteAsync()
                : db.Entities.Where(o => o.Status == "open").ExecuteDelete();

            Assert.Equal(2, deleted);
            AssertCommand(spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkDelete), "SingleCommand", sensitive, "open");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Single_command_update_logs_its_filter_and_set_document_only_with_sensitive_logging(bool async)
    {
        foreach (var sensitive in new[] { false, true })
        {
            var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
            var collection = SeedCollection(
                $"{nameof(Single_command_update_logs_its_filter_and_set_document_only_with_sensitive_logging)}_{sensitive}", async);
            using var db = CreateContext(collection, loggerFactory, sensitive);

            var updated = async
                ? await db.Entities.Where(o => o.Status == "open")
                    .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "shipped"))
                : db.Entities.Where(o => o.Status == "open")
                    .ExecuteUpdate(s => s.SetProperty(o => o.Status, "shipped"));

            Assert.Equal(2, updated);
            var executing = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutingBulkUpdate);
            AssertCommand(executing, "SingleCommand", sensitive, "open");
            if (sensitive)
            {
                Assert.Contains("$set", executing);
                Assert.Contains("shipped", executing);
            }
            else
            {
                Assert.DoesNotContain("shipped", executing);
                Assert.DoesNotContain("$set", executing);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Two_phase_delete_logs_its_by_id_filter_only_with_sensitive_logging(bool async)
    {
        foreach (var sensitive in new[] { false, true })
        {
            var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
            var collection = SeedCollection($"{nameof(Two_phase_delete_logs_its_by_id_filter_only_with_sensitive_logging)}_{sensitive}", async);
            using var db = CreateContext(collection, loggerFactory, sensitive);

            var deleted = async
                ? await db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDeleteAsync()
                : db.Entities.OrderBy(o => o.Quantity).Take(2).ExecuteDelete();

            Assert.Equal(2, deleted);
            var executing = spyLogger.GetBulkLogMessageByEventId(MongoEventId.ExecutingBulkDelete, "WriteTargets");
            Assert.Contains("Strategy=TwoPhase", executing);
            Assert.Contains("Targets=2", executing);
            AssertByIdCommand(executing, sensitive);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Two_phase_update_logs_its_by_id_filter_and_set_document_only_with_sensitive_logging(bool async)
    {
        foreach (var sensitive in new[] { false, true })
        {
            var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
            var collection = SeedCollection(
                $"{nameof(Two_phase_update_logs_its_by_id_filter_and_set_document_only_with_sensitive_logging)}_{sensitive}", async);
            using var db = CreateContext(collection, loggerFactory, sensitive);

            var updated = async
                ? await db.Entities.OrderBy(o => o.Quantity).Take(2)
                    .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "shipped"))
                : db.Entities.OrderBy(o => o.Quantity).Take(2)
                    .ExecuteUpdate(s => s.SetProperty(o => o.Status, "shipped"));

            Assert.Equal(2, updated);
            var executing = spyLogger.GetBulkLogMessageByEventId(MongoEventId.ExecutingBulkUpdate, "WriteTargets");
            Assert.Contains("Strategy=TwoPhase", executing);
            Assert.Contains("Targets=2", executing);
            AssertByIdCommand(executing, sensitive);
            if (sensitive)
            {
                Assert.Contains("$set", executing);
                Assert.Contains("shipped", executing);
            }
            else
            {
                Assert.DoesNotContain("shipped", executing);
            }
        }
    }

    // The server's error quotes the offending key ("dup key: { Quantity: 5 }"): like the command, it is logged only with sensitive logging on.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_bulk_update_logs_the_server_error_message_only_with_sensitive_logging(bool async)
    {
        foreach (var sensitive in new[] { false, true })
        {
            var (loggerFactory, spyLogger) = SpyLoggerProvider.Create();
            var collection = SeedCollection(
                $"{nameof(Failed_bulk_update_logs_the_server_error_message_only_with_sensitive_logging)}_{sensitive}", async);
            collection.Indexes.CreateOne(
                new CreateIndexModel<Order>(Builders<Order>.IndexKeys.Ascending(o => o.Quantity), new CreateIndexOptions { Unique = true }));
            using var db = CreateContext(collection, loggerFactory, sensitive);

            await Assert.ThrowsAnyAsync<Exception>(
                async () => _ = async
                    ? await db.Entities.ExecuteUpdateAsync(s => s.SetProperty(o => o.Quantity, 5))
                    : db.Entities.ExecuteUpdate(s => s.SetProperty(o => o.Quantity, 5)));

            var failed = spyLogger.GetLogMessageByEventId(MongoEventId.BulkUpdateFailed);
            Assert.Contains("Bulk Update failed", failed);
            Assert.Contains("Phase=Command", failed);
            if (sensitive)
            {
                Assert.Contains("dup key", failed);
            }
            else
            {
                Assert.DoesNotContain("dup key", failed);
                Assert.DoesNotContain("Quantity", failed);
            }
        }
    }

    // The by-id write renders { _id: { $in: [...] } }: the ids harvested in phase 1, not the original predicate.
    private static void AssertByIdCommand(string executing, bool sensitive)
    {
        if (sensitive)
        {
            Assert.DoesNotContain("Command=?", executing);
            Assert.Contains("$in", executing);
            Assert.Contains("_id", executing);

            // Phase 1 is part of the command and reads only _id: the projection is the LAST stage of the pipeline the aggregate carried
            // (appended after the sort and limit, so it cannot change which documents are selected).
            Assert.Contains("phase1Pipeline", executing);
            var command = BsonDocument.Parse(executing[(executing.IndexOf("Command=", StringComparison.Ordinal) + "Command=".Length)..].Trim());
            Assert.Equal(new BsonDocument("$project", new BsonDocument("_id", 1)), command["phase1Pipeline"].AsBsonArray.Last());
        }
        else
        {
            Assert.Contains("Command=?", executing);
            Assert.DoesNotContain("$project", executing);
            Assert.DoesNotContain("$in", executing);
            Assert.DoesNotContain("ObjectId", executing);
        }
    }

    private static void AssertCommand(string executing, string strategy, bool sensitive, string filterValue)
    {
        Assert.Contains($"Strategy={strategy}", executing);
        Assert.Contains("Targets=-", executing);
        if (sensitive)
        {
            Assert.DoesNotContain("Command=?", executing);
            Assert.Contains("\"filter\"", executing);
            Assert.Contains(filterValue, executing);
        }
        else
        {
            Assert.Contains("Command=?", executing);
            Assert.DoesNotContain(filterValue, executing);
            Assert.DoesNotContain("\"filter\"", executing);
        }
    }
}

#endif
