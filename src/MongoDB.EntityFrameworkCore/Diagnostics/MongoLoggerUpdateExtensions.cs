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

using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MongoDB.EntityFrameworkCore.Diagnostics;

using BulkWriteEventDefinition = EventDefinition<string, CollectionNamespace, long, long, long>;
#if !EF8
using BulkExecutingEventDefinition = EventDefinition<string, CollectionNamespace, string, string>;
using BulkExecutedDeleteEventDefinition = EventDefinition<string, CollectionNamespace, long>;
using BulkExecutedUpdateEventDefinition = EventDefinition<string, CollectionNamespace, long, long>;
#endif

/// <summary>
/// MongoDB-specific logging extensions for operations in the Update category.
/// </summary>
internal static class MongoLoggerUpdateExtensions
{
    /// <summary>
    /// Logs for the <see cref="MongoEventId.ExecutingBulkWrite" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The amount of time the operation took.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this query is using.</param>
    /// <param name="insertCount">The number of documents to insert.</param>
    /// <param name="deleteCount">The number of documents to delete.</param>
    /// <param name="modifyCount">The number of documents to modify.</param>
    public static void ExecutingBulkWrite(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        long insertCount,
        long deleteCount,
        long modifyCount)
    {
        var definition = LogExecutingBulkWrite(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics,
                duration.TotalMilliseconds.ToString(),
                collectionNamespace,
                insertCount,
                deleteCount,
                modifyCount);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new MongoBulkWriteEventData(
                definition,
                ExecutingBulkWriteMessage,
                duration,
                collectionNamespace,
                insertCount,
                deleteCount,
                modifyCount);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string ExecutingBulkWriteMessage(EventDefinitionBase definition, EventData payload)
    {
        var d = (BulkWriteEventDefinition)definition;
        var p = (MongoBulkWriteEventData)payload;
        return d.GenerateMessage(
            p.Elapsed.TotalMilliseconds.ToString(),
            p.CollectionNamespace,
            p.InsertCount,
            p.DeleteCount,
            p.ModifyCount);
    }

    private static BulkWriteEventDefinition LogExecutingBulkWrite(IDiagnosticsLogger logger)
        => (BulkWriteEventDefinition)
            (((MongoLoggingDefinitions)logger.Definitions).LogExecutingBulkWrite ?? NonCapturingLazyInitializer.EnsureInitialized(
                ref ((MongoLoggingDefinitions)logger.Definitions).LogExecutingBulkWrite,
                logger,
                static logger => new BulkWriteEventDefinition(
                    logger.Options,
                    MongoEventId.ExecutingBulkWrite,
                    LogLevel.Information,
                    "MongoEventId.ExecutingBulkWrite",
                    level => LoggerMessage.Define<string, CollectionNamespace, long, long, long>(
                        level,
                        MongoEventId.ExecutingBulkWrite,
                        "Executing Bulk Write ({elapsed} ms) Collection='{collectionNamespace}', Insertions={inserted}, Deletions={deleted}, Modifications={modified}"))));

    /// <summary>
    /// Logs for the <see cref="MongoEventId.ExecutedBulkWrite" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The amount of time the operation took.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this query is using.</param>
    /// <param name="insertCount">The number of documents inserted.</param>
    /// <param name="deleteCount">The number of documents deleted.</param>
    /// <param name="modifyCount">The number of documents modified.</param>
    public static void ExecutedBulkWrite(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        long insertCount,
        long deleteCount,
        long modifyCount)
    {
        var definition = LogExecutedBulkWrite(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics,
                duration.TotalMilliseconds.ToString(),
                collectionNamespace,
                insertCount,
                deleteCount,
                modifyCount);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new MongoBulkWriteEventData(
                definition,
                ExecutedBulkWrite,
                duration,
                collectionNamespace,
                insertCount,
                deleteCount,
                modifyCount);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string ExecutedBulkWrite(EventDefinitionBase definition, EventData payload)
    {
        var d = (BulkWriteEventDefinition)definition;
        var p = (MongoBulkWriteEventData)payload;
        return d.GenerateMessage(
            p.Elapsed.TotalMilliseconds.ToString(),
            p.CollectionNamespace,
            p.InsertCount,
            p.DeleteCount,
            p.ModifyCount);
    }

    private static BulkWriteEventDefinition LogExecutedBulkWrite(IDiagnosticsLogger logger)
        => (BulkWriteEventDefinition)
            (((MongoLoggingDefinitions)logger.Definitions).LogExecutedBulkWrite ?? NonCapturingLazyInitializer.EnsureInitialized(
                ref ((MongoLoggingDefinitions)logger.Definitions).LogExecutedBulkWrite,
                logger,
                static logger => new BulkWriteEventDefinition(
                    logger.Options,
                    MongoEventId.ExecutedBulkWrite,
                    LogLevel.Information,
                    "MongoEventId.ExecutedBulkWrite",
                    level => LoggerMessage.Define<string, CollectionNamespace, long, long, long>(
                        level,
                        MongoEventId.ExecutedBulkWrite,
                        "Executed Bulk Write ({elapsed} ms) Collection='{collectionNamespace}', Inserted={inserted}, Deleted={deleted}, Modified={modified}"))));

#if !EF8
    /// <summary>
    /// Logs for the <see cref="MongoEventId.ExecutingBulkDelete" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The amount of time the operation took.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="strategy">The execution strategy: <c>SingleCommand</c> or <c>TwoPhase</c>.</param>
    /// <param name="targetCount">
    /// For a two-phase operation, the number of target document ids collected in phase 1;
    /// <see langword="null"/> for a single-command operation.
    /// </param>
    public static void ExecutingBulkDelete(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        string strategy,
        long? targetCount)
    {
        var definition = LogExecutingBulkDelete(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics,
                duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
                collectionNamespace,
                strategy,
                FormatTargetCount(targetCount));
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new MongoBulkDeleteEventData(
                definition,
                ExecutingBulkDeleteMessage,
                duration,
                collectionNamespace,
                deleteCount: 0,
                targetCount: targetCount,
                strategy: strategy);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string ExecutingBulkDeleteMessage(EventDefinitionBase definition, EventData payload)
    {
        var d = (BulkExecutingEventDefinition)definition;
        var p = (MongoBulkDeleteEventData)payload;
        return d.GenerateMessage(
            p.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
            p.CollectionNamespace,
            p.Strategy,
            FormatTargetCount(p.TargetCount));
    }

    private static BulkExecutingEventDefinition LogExecutingBulkDelete(IDiagnosticsLogger logger)
        => (BulkExecutingEventDefinition)
            (((MongoLoggingDefinitions)logger.Definitions).LogExecutingBulkDelete ?? NonCapturingLazyInitializer.EnsureInitialized(
                ref ((MongoLoggingDefinitions)logger.Definitions).LogExecutingBulkDelete,
                logger,
                static logger => new BulkExecutingEventDefinition(
                    logger.Options,
                    MongoEventId.ExecutingBulkDelete,
                    LogLevel.Information,
                    "MongoEventId.ExecutingBulkDelete",
                    level => LoggerMessage.Define<string, CollectionNamespace, string, string>(
                        level,
                        MongoEventId.ExecutingBulkDelete,
                        "Executing Bulk Delete ({elapsed} ms) Collection='{collectionNamespace}', Strategy={strategy}, Targets={targetCount}"))));

    /// <summary>
    /// Logs for the <see cref="MongoEventId.ExecutedBulkDelete" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The amount of time the operation took.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="deleteCount">The number of documents deleted.</param>
    /// <param name="strategy">The execution strategy: <c>SingleCommand</c> or <c>TwoPhase</c>.</param>
    public static void ExecutedBulkDelete(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        long deleteCount,
        string strategy)
    {
        var definition = LogExecutedBulkDelete(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics, duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture), collectionNamespace, deleteCount);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new MongoBulkDeleteEventData(
                definition,
                ExecutedBulkDeleteMessage,
                duration,
                collectionNamespace,
                deleteCount: deleteCount,
                targetCount: null,
                strategy: strategy);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string ExecutedBulkDeleteMessage(EventDefinitionBase definition, EventData payload)
    {
        var d = (BulkExecutedDeleteEventDefinition)definition;
        var p = (MongoBulkDeleteEventData)payload;
        return d.GenerateMessage(
            p.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture), p.CollectionNamespace, p.DeleteCount);
    }

    private static BulkExecutedDeleteEventDefinition LogExecutedBulkDelete(IDiagnosticsLogger logger)
        => (BulkExecutedDeleteEventDefinition)
            (((MongoLoggingDefinitions)logger.Definitions).LogExecutedBulkDelete ?? NonCapturingLazyInitializer.EnsureInitialized(
                ref ((MongoLoggingDefinitions)logger.Definitions).LogExecutedBulkDelete,
                logger,
                static logger => new BulkExecutedDeleteEventDefinition(
                    logger.Options,
                    MongoEventId.ExecutedBulkDelete,
                    LogLevel.Information,
                    "MongoEventId.ExecutedBulkDelete",
                    level => LoggerMessage.Define<string, CollectionNamespace, long>(
                        level,
                        MongoEventId.ExecutedBulkDelete,
                        "Executed Bulk Delete ({elapsed} ms) Collection='{collectionNamespace}', Deleted={deleted}"))));

    /// <summary>
    /// Logs for the <see cref="MongoEventId.ExecutingBulkUpdate" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The amount of time the operation took.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="strategy">The execution strategy: <c>SingleCommand</c> or <c>TwoPhase</c>.</param>
    /// <param name="targetCount">
    /// For a two-phase operation, the number of target document ids collected in phase 1;
    /// <see langword="null"/> for a single-command operation.
    /// </param>
    public static void ExecutingBulkUpdate(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        string strategy,
        long? targetCount)
    {
        var definition = LogExecutingBulkUpdate(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics,
                duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
                collectionNamespace,
                strategy,
                FormatTargetCount(targetCount));
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new MongoBulkUpdateEventData(
                definition,
                ExecutingBulkUpdateMessage,
                duration,
                collectionNamespace,
                modifyCount: 0,
                matchedCount: 0,
                targetCount: targetCount,
                strategy: strategy);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string ExecutingBulkUpdateMessage(EventDefinitionBase definition, EventData payload)
    {
        var d = (BulkExecutingEventDefinition)definition;
        var p = (MongoBulkUpdateEventData)payload;
        return d.GenerateMessage(
            p.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
            p.CollectionNamespace,
            p.Strategy,
            FormatTargetCount(p.TargetCount));
    }

    private static BulkExecutingEventDefinition LogExecutingBulkUpdate(IDiagnosticsLogger logger)
        => (BulkExecutingEventDefinition)
            (((MongoLoggingDefinitions)logger.Definitions).LogExecutingBulkUpdate ?? NonCapturingLazyInitializer.EnsureInitialized(
                ref ((MongoLoggingDefinitions)logger.Definitions).LogExecutingBulkUpdate,
                logger,
                static logger => new BulkExecutingEventDefinition(
                    logger.Options,
                    MongoEventId.ExecutingBulkUpdate,
                    LogLevel.Information,
                    "MongoEventId.ExecutingBulkUpdate",
                    level => LoggerMessage.Define<string, CollectionNamespace, string, string>(
                        level,
                        MongoEventId.ExecutingBulkUpdate,
                        "Executing Bulk Update ({elapsed} ms) Collection='{collectionNamespace}', Strategy={strategy}, Targets={targetCount}"))));

    /// <summary>
    /// Logs for the <see cref="MongoEventId.ExecutedBulkUpdate" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The amount of time the operation took.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="matchedCount">The number of documents matched — the value returned to the caller.</param>
    /// <param name="modifyCount">The number of documents whose stored values actually changed.</param>
    /// <param name="strategy">The execution strategy: <c>SingleCommand</c> or <c>TwoPhase</c>.</param>
    public static void ExecutedBulkUpdate(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        long matchedCount,
        long modifyCount,
        string strategy)
    {
        var definition = LogExecutedBulkUpdate(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics,
                duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
                collectionNamespace,
                matchedCount,
                modifyCount);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new MongoBulkUpdateEventData(
                definition,
                ExecutedBulkUpdateMessage,
                duration,
                collectionNamespace,
                modifyCount: modifyCount,
                matchedCount: matchedCount,
                targetCount: null,
                strategy: strategy);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string ExecutedBulkUpdateMessage(EventDefinitionBase definition, EventData payload)
    {
        var d = (BulkExecutedUpdateEventDefinition)definition;
        var p = (MongoBulkUpdateEventData)payload;
        return d.GenerateMessage(
            p.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
            p.CollectionNamespace,
            p.MatchedCount,
            p.ModifyCount);
    }

    private static BulkExecutedUpdateEventDefinition LogExecutedBulkUpdate(IDiagnosticsLogger logger)
        => (BulkExecutedUpdateEventDefinition)
            (((MongoLoggingDefinitions)logger.Definitions).LogExecutedBulkUpdate ?? NonCapturingLazyInitializer.EnsureInitialized(
                ref ((MongoLoggingDefinitions)logger.Definitions).LogExecutedBulkUpdate,
                logger,
                static logger => new BulkExecutedUpdateEventDefinition(
                    logger.Options,
                    MongoEventId.ExecutedBulkUpdate,
                    LogLevel.Information,
                    "MongoEventId.ExecutedBulkUpdate",
                    level => LoggerMessage.Define<string, CollectionNamespace, long, long>(
                        level,
                        MongoEventId.ExecutedBulkUpdate,
                        "Executed Bulk Update ({elapsed} ms) Collection='{collectionNamespace}', Matched={matched}, Modified={modified}"))));

    private static string FormatTargetCount(long? targetCount)
        => targetCount?.ToString(CultureInfo.InvariantCulture) ?? "-";
#endif
}
