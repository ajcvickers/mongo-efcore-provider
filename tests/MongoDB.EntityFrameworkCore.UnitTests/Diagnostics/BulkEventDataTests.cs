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

// Server-free coverage of the public contract of the bulk diagnostics events: the event data constructors that existed before the Phase
// property was added must keep working (and infer it), and the new event ids must be distinct ids in the Update category.

#if !EF8

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;

namespace MongoDB.EntityFrameworkCore.UnitTests.Diagnostics;

public static class BulkEventDataTests
{
    private static readonly CollectionNamespace Namespace = new("db", "coll");

    // The event definition is only read when the message is rendered, which these tests never do.
    private static string NoMessage(EventDefinitionBase definition, EventData data) => "";

    [Theory]
    [InlineData(null, "SingleCommand", "Command")]
    [InlineData(3L, "TwoPhase", "WriteTargets")]
    public static void Delete_event_data_built_by_the_oldest_constructor_infers_strategy_and_phase(
        long? targetCount, string expectedStrategy, string expectedPhase)
    {
        var data = new MongoBulkDeleteEventData(null!, NoMessage, TimeSpan.FromSeconds(1), Namespace, deleteCount: 2, targetCount);

        Assert.Equal(expectedStrategy, data.Strategy);
        Assert.Equal(expectedPhase, data.Phase);
        Assert.Equal(targetCount, data.TargetCount);
        Assert.Equal(2, data.DeleteCount);
        Assert.Equal(TimeSpan.FromSeconds(1), data.Elapsed);
        Assert.Equal(Namespace, data.CollectionNamespace);
        Assert.Null(data.Command);
        Assert.False(data.LogSensitiveData);
    }

    [Theory]
    [InlineData("SingleCommand", "Command")]
    [InlineData("TwoPhase", "WriteTargets")]
    public static void Delete_event_data_built_without_a_phase_infers_it_from_the_strategy(string strategy, string expectedPhase)
    {
        var data = new MongoBulkDeleteEventData(
            null!, NoMessage, TimeSpan.Zero, Namespace, deleteCount: 2, targetCount: 5, strategy, command: "?", logSensitiveData: true);

        Assert.Equal(strategy, data.Strategy);
        Assert.Equal(expectedPhase, data.Phase);
        Assert.Equal("?", data.Command);
        Assert.True(data.LogSensitiveData);
        Assert.Equal(5, data.TargetCount);
    }

    [Fact]
    public static void Delete_event_data_carries_an_explicit_phase()
    {
        var data = new MongoBulkDeleteEventData(
            null!, NoMessage, TimeSpan.Zero, Namespace, deleteCount: 0, targetCount: 4, "TwoPhase", "ReadTargets", command: null,
            logSensitiveData: false);

        Assert.Equal("ReadTargets", data.Phase);
        Assert.Equal("TwoPhase", data.Strategy);
        Assert.Equal(4, data.TargetCount);
    }

    [Theory]
    [InlineData(null, "SingleCommand", "Command")]
    [InlineData(3L, "TwoPhase", "WriteTargets")]
    public static void Update_event_data_built_by_the_oldest_constructor_infers_strategy_and_phase(
        long? targetCount, string expectedStrategy, string expectedPhase)
    {
        var data = new MongoBulkUpdateEventData(null!, NoMessage, TimeSpan.FromSeconds(1), Namespace, modifyCount: 2, targetCount);

        Assert.Equal(expectedStrategy, data.Strategy);
        Assert.Equal(expectedPhase, data.Phase);
        Assert.Equal(targetCount, data.TargetCount);
        Assert.Equal(2, data.ModifyCount);
        Assert.Equal(2, data.MatchedCount);
        Assert.Equal(TimeSpan.FromSeconds(1), data.Elapsed);
        Assert.Equal(Namespace, data.CollectionNamespace);
        Assert.Null(data.Command);
        Assert.False(data.LogSensitiveData);
    }

    [Theory]
    [InlineData("SingleCommand", "Command")]
    [InlineData("TwoPhase", "WriteTargets")]
    public static void Update_event_data_built_without_a_phase_infers_it_from_the_strategy(string strategy, string expectedPhase)
    {
        var data = new MongoBulkUpdateEventData(
            null!, NoMessage, TimeSpan.Zero, Namespace, modifyCount: 1, matchedCount: 2, targetCount: 5, strategy, command: "?",
            logSensitiveData: true);

        Assert.Equal(strategy, data.Strategy);
        Assert.Equal(expectedPhase, data.Phase);
        Assert.Equal(1, data.ModifyCount);
        Assert.Equal(2, data.MatchedCount);
        Assert.Equal("?", data.Command);
        Assert.True(data.LogSensitiveData);
    }

    [Fact]
    public static void Update_event_data_carries_an_explicit_phase()
    {
        var data = new MongoBulkUpdateEventData(
            null!, NoMessage, TimeSpan.Zero, Namespace, modifyCount: 0, matchedCount: 0, targetCount: 4, "TwoPhase", "ReadTargets",
            command: null, logSensitiveData: false);

        Assert.Equal("ReadTargets", data.Phase);
        Assert.Equal("TwoPhase", data.Strategy);
        Assert.Equal(4, data.TargetCount);
    }

    [Fact]
    public static void Failure_event_data_exposes_what_it_was_built_with()
    {
        var exception = new InvalidOperationException("boom");

        var data = new MongoBulkOperationErrorEventData(
            null!, NoMessage, TimeSpan.FromSeconds(2), Namespace, "TwoPhase", "WriteTargets", targetCount: 7, exception,
            logSensitiveData: true);

        Assert.IsAssignableFrom<IErrorEventData>(data);
        Assert.Same(exception, data.Exception);
        Assert.Equal("TwoPhase", data.Strategy);
        Assert.Equal("WriteTargets", data.Phase);
        Assert.Equal(7, data.TargetCount);
        Assert.Equal(TimeSpan.FromSeconds(2), data.Elapsed);
        Assert.Equal(Namespace, data.CollectionNamespace);
        Assert.True(data.LogSensitiveData);
    }
}

public static class BulkEventIdTests
{
    [Fact]
    public static void The_failure_events_are_named_in_the_Update_category()
    {
        Assert.Equal($"{DbLoggerCategory.Update.Name}.BulkDeleteFailed", MongoEventId.BulkDeleteFailed.Name);
        Assert.Equal($"{DbLoggerCategory.Update.Name}.BulkUpdateFailed", MongoEventId.BulkUpdateFailed.Name);
    }

    // Event ids are part of the public contract (subscribers and filters match on them), so the new ones must not have renumbered any
    // existing id: they are appended after the last existing bulk event.
    [Fact]
    public static void The_failure_events_are_appended_after_the_existing_bulk_events_and_every_event_id_is_distinct()
    {
        Assert.True(MongoEventId.BulkDeleteFailed.Id > MongoEventId.ExecutedBulkUpdate.Id);
        Assert.True(MongoEventId.BulkUpdateFailed.Id > MongoEventId.BulkDeleteFailed.Id);

        var ids = typeof(MongoEventId)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(EventId))
            .Select(f => ((EventId)f.GetValue(null)!).Id)
            .ToList();

        Assert.Contains(MongoEventId.BulkDeleteFailed.Id, ids);
        Assert.Contains(MongoEventId.BulkUpdateFailed.Id, ids);
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}

#endif
