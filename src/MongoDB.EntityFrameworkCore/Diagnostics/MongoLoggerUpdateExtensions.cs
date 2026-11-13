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
// elapsed, collection, strategy, phase, targets, command
using BulkExecutingEventDefinition = EventDefinition<string, CollectionNamespace, string, string, string, string>;
// elapsed, collection, phase, targets, deleted
using BulkExecutedDeleteEventDefinition = EventDefinition<string, CollectionNamespace, string, string, long>;
// elapsed, collection, phase, targets, matched, modified
using BulkExecutedUpdateEventDefinition = EventDefinition<string, CollectionNamespace, string, string, long, long>;
// elapsed, collection, strategy, phase, targets, error
using BulkFailedEventDefinition = EventDefinition<string, CollectionNamespace, string, string, string, string>;
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
    /// <param name="duration">The amount of time the round trip has taken (effectively zero: it has just begun).</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="strategy">The execution strategy: <c>SingleCommand</c> or <c>TwoPhase</c>.</param>
    /// <param name="phase">The round trip: <c>Command</c>, <c>ReadTargets</c> or <c>WriteTargets</c>.</param>
    /// <param name="targetCount">
    /// For a <c>WriteTargets</c> round trip, the number of ids in the <c>$in</c> filter of this batch (a large operation is written in
    /// batches, each a round trip with its own <c>Executing</c> event); <see langword="null"/> otherwise.
    /// </param>
    /// <param name="renderCommand">
    /// Renders the command (already redacted to <c>"?"</c> when sensitive-data logging is off), or <see langword="null"/> when the round
    /// trip has no command to report yet. Only invoked when something is listening to this event, and then once.
    /// </param>
    public static void ExecutingBulkDelete(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        string strategy,
        string phase,
        long? targetCount,
        Func<string>? renderCommand)
    {
        var definition = LogExecutingBulkDelete(diagnostics);

        var shouldLog = diagnostics.ShouldLog(definition);
        var needsEventData = diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled);
        if (!shouldLog && !needsEventData)
        {
            // Nothing is listening: don't render the command (a two-phase filter can hold every harvested id).
            return;
        }

        var command = renderCommand?.Invoke();

        if (shouldLog)
        {
            definition.Log(
                diagnostics,
                duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
                collectionNamespace,
                strategy,
                phase,
                FormatTargetCount(targetCount),
                command ?? NoCommand);
        }

        if (needsEventData)
        {
            var eventData = new MongoBulkDeleteEventData(
                definition,
                ExecutingBulkDeleteMessage,
                duration,
                collectionNamespace,
                deleteCount: 0,
                targetCount: targetCount,
                strategy: strategy,
                phase: phase,
                command: command,
                logSensitiveData: diagnostics.ShouldLogSensitiveData());

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
            p.Phase,
            FormatTargetCount(p.TargetCount),
            p.Command ?? NoCommand);
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
                    level => LoggerMessage.Define<string, CollectionNamespace, string, string, string, string>(
                        level,
                        MongoEventId.ExecutingBulkDelete,
                        "Executing Bulk Delete ({elapsed} ms) Collection='{collectionNamespace}', Strategy={strategy}, Phase={phase}, "
                        + "Targets={targetCount}, Command={command}"))));

    /// <summary>
    /// Logs for the <see cref="MongoEventId.ExecutedBulkDelete" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The amount of time the round trip took.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="deleteCount">The number of documents deleted by this round trip.</param>
    /// <param name="strategy">The execution strategy: <c>SingleCommand</c> or <c>TwoPhase</c>.</param>
    /// <param name="phase">The round trip: <c>Command</c>, <c>ReadTargets</c> or <c>WriteTargets</c>.</param>
    /// <param name="targetCount">
    /// The number of ids found by a <c>ReadTargets</c> round trip, or in the batch a <c>WriteTargets</c> round trip wrote;
    /// <see langword="null"/> for a single command.
    /// </param>
    public static void ExecutedBulkDelete(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        long deleteCount,
        string strategy,
        string phase,
        long? targetCount)
    {
        var definition = LogExecutedBulkDelete(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics,
                duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
                collectionNamespace,
                phase,
                FormatTargetCount(targetCount),
                deleteCount);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new MongoBulkDeleteEventData(
                definition,
                ExecutedBulkDeleteMessage,
                duration,
                collectionNamespace,
                deleteCount: deleteCount,
                targetCount: targetCount,
                strategy: strategy,
                phase: phase,
                command: null,
                logSensitiveData: false);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string ExecutedBulkDeleteMessage(EventDefinitionBase definition, EventData payload)
    {
        var d = (BulkExecutedDeleteEventDefinition)definition;
        var p = (MongoBulkDeleteEventData)payload;
        return d.GenerateMessage(
            p.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
            p.CollectionNamespace,
            p.Phase,
            FormatTargetCount(p.TargetCount),
            p.DeleteCount);
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
                    level => LoggerMessage.Define<string, CollectionNamespace, string, string, long>(
                        level,
                        MongoEventId.ExecutedBulkDelete,
                        "Executed Bulk Delete ({elapsed} ms) Collection='{collectionNamespace}', Phase={phase}, "
                        + "Targets={targetCount}, Deleted={deleted}"))));

    /// <summary>
    /// Logs for the <see cref="MongoEventId.BulkDeleteFailed" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The amount of time from the start of the round trip to the failure.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="strategy">The execution strategy: <c>SingleCommand</c> or <c>TwoPhase</c>.</param>
    /// <param name="phase">The round trip that failed: <c>Command</c>, <c>ReadTargets</c> or <c>WriteTargets</c>.</param>
    /// <param name="targetCount">The number of ids in the batch that failed, for a <c>WriteTargets</c> round trip; otherwise <see langword="null"/>.</param>
    /// <param name="exception">The exception that was thrown.</param>
    public static void BulkDeleteFailed(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        string strategy,
        string phase,
        long? targetCount,
        Exception exception)
    {
        var definition = LogBulkDeleteFailed(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics,
                duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
                collectionNamespace,
                strategy,
                phase,
                FormatTargetCount(targetCount),
                FailureText(exception, diagnostics.ShouldLogSensitiveData()));
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new MongoBulkOperationErrorEventData(
                definition, BulkFailedMessage, duration, collectionNamespace, strategy, phase, targetCount, exception,
                diagnostics.ShouldLogSensitiveData());

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static BulkFailedEventDefinition LogBulkDeleteFailed(IDiagnosticsLogger logger)
        => (BulkFailedEventDefinition)
            (((MongoLoggingDefinitions)logger.Definitions).LogBulkDeleteFailed ?? NonCapturingLazyInitializer.EnsureInitialized(
                ref ((MongoLoggingDefinitions)logger.Definitions).LogBulkDeleteFailed,
                logger,
                static logger => new BulkFailedEventDefinition(
                    logger.Options,
                    MongoEventId.BulkDeleteFailed,
                    LogLevel.Error,
                    "MongoEventId.BulkDeleteFailed",
                    level => LoggerMessage.Define<string, CollectionNamespace, string, string, string, string>(
                        level,
                        MongoEventId.BulkDeleteFailed,
                        "Bulk Delete failed ({elapsed} ms) Collection='{collectionNamespace}', Strategy={strategy}, Phase={phase}, "
                        + "Targets={targetCount}: {error}"))));

    /// <summary>
    /// Logs for the <see cref="MongoEventId.ExecutingBulkUpdate" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The amount of time the round trip has taken (effectively zero: it has just begun).</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="strategy">The execution strategy: <c>SingleCommand</c> or <c>TwoPhase</c>.</param>
    /// <param name="phase">The round trip: <c>Command</c>, <c>ReadTargets</c> or <c>WriteTargets</c>.</param>
    /// <param name="targetCount">
    /// For a <c>WriteTargets</c> round trip, the number of ids in the <c>$in</c> filter of this batch (a large operation is written in
    /// batches, each a round trip with its own <c>Executing</c> event); <see langword="null"/> otherwise.
    /// </param>
    /// <param name="renderCommand">
    /// Renders the command (already redacted to <c>"?"</c> when sensitive-data logging is off), or <see langword="null"/> when the round
    /// trip has no command to report yet. Only invoked when something is listening to this event, and then once.
    /// </param>
    public static void ExecutingBulkUpdate(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        string strategy,
        string phase,
        long? targetCount,
        Func<string>? renderCommand)
    {
        var definition = LogExecutingBulkUpdate(diagnostics);

        var shouldLog = diagnostics.ShouldLog(definition);
        var needsEventData = diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled);
        if (!shouldLog && !needsEventData)
        {
            // Nothing is listening: don't render the command (a two-phase filter can hold every harvested id).
            return;
        }

        var command = renderCommand?.Invoke();

        if (shouldLog)
        {
            definition.Log(
                diagnostics,
                duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
                collectionNamespace,
                strategy,
                phase,
                FormatTargetCount(targetCount),
                command ?? NoCommand);
        }

        if (needsEventData)
        {
            var eventData = new MongoBulkUpdateEventData(
                definition,
                ExecutingBulkUpdateMessage,
                duration,
                collectionNamespace,
                modifyCount: 0,
                matchedCount: 0,
                targetCount: targetCount,
                strategy: strategy,
                phase: phase,
                command: command,
                logSensitiveData: diagnostics.ShouldLogSensitiveData());

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
            p.Phase,
            FormatTargetCount(p.TargetCount),
            p.Command ?? NoCommand);
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
                    level => LoggerMessage.Define<string, CollectionNamespace, string, string, string, string>(
                        level,
                        MongoEventId.ExecutingBulkUpdate,
                        "Executing Bulk Update ({elapsed} ms) Collection='{collectionNamespace}', Strategy={strategy}, Phase={phase}, "
                        + "Targets={targetCount}, Command={command}"))));

    /// <summary>
    /// Logs for the <see cref="MongoEventId.ExecutedBulkUpdate" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The amount of time the round trip took.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="matchedCount">The number of documents matched by this round trip.</param>
    /// <param name="modifyCount">The number of documents whose stored values actually changed in this round trip.</param>
    /// <param name="strategy">The execution strategy: <c>SingleCommand</c> or <c>TwoPhase</c>.</param>
    /// <param name="phase">The round trip: <c>Command</c>, <c>ReadTargets</c> or <c>WriteTargets</c>.</param>
    /// <param name="targetCount">
    /// The number of ids found by a <c>ReadTargets</c> round trip, or in the batch a <c>WriteTargets</c> round trip wrote;
    /// <see langword="null"/> for a single command.
    /// </param>
    public static void ExecutedBulkUpdate(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        long matchedCount,
        long modifyCount,
        string strategy,
        string phase,
        long? targetCount)
    {
        var definition = LogExecutedBulkUpdate(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics,
                duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
                collectionNamespace,
                phase,
                FormatTargetCount(targetCount),
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
                targetCount: targetCount,
                strategy: strategy,
                phase: phase,
                command: null,
                logSensitiveData: false);

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
            p.Phase,
            FormatTargetCount(p.TargetCount),
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
                    level => LoggerMessage.Define<string, CollectionNamespace, string, string, long, long>(
                        level,
                        MongoEventId.ExecutedBulkUpdate,
                        "Executed Bulk Update ({elapsed} ms) Collection='{collectionNamespace}', Phase={phase}, "
                        + "Targets={targetCount}, Matched={matched}, Modified={modified}"))));

    /// <summary>
    /// Logs for the <see cref="MongoEventId.BulkUpdateFailed" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="duration">The amount of time from the start of the round trip to the failure.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> this operation is using.</param>
    /// <param name="strategy">The execution strategy: <c>SingleCommand</c> or <c>TwoPhase</c>.</param>
    /// <param name="phase">The round trip that failed: <c>Command</c>, <c>ReadTargets</c> or <c>WriteTargets</c>.</param>
    /// <param name="targetCount">The number of ids in the batch that failed, for a <c>WriteTargets</c> round trip; otherwise <see langword="null"/>.</param>
    /// <param name="exception">The exception that was thrown.</param>
    public static void BulkUpdateFailed(
        this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics,
        TimeSpan duration,
        CollectionNamespace collectionNamespace,
        string strategy,
        string phase,
        long? targetCount,
        Exception exception)
    {
        var definition = LogBulkUpdateFailed(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics,
                duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
                collectionNamespace,
                strategy,
                phase,
                FormatTargetCount(targetCount),
                FailureText(exception, diagnostics.ShouldLogSensitiveData()));
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new MongoBulkOperationErrorEventData(
                definition, BulkFailedMessage, duration, collectionNamespace, strategy, phase, targetCount, exception,
                diagnostics.ShouldLogSensitiveData());

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static BulkFailedEventDefinition LogBulkUpdateFailed(IDiagnosticsLogger logger)
        => (BulkFailedEventDefinition)
            (((MongoLoggingDefinitions)logger.Definitions).LogBulkUpdateFailed ?? NonCapturingLazyInitializer.EnsureInitialized(
                ref ((MongoLoggingDefinitions)logger.Definitions).LogBulkUpdateFailed,
                logger,
                static logger => new BulkFailedEventDefinition(
                    logger.Options,
                    MongoEventId.BulkUpdateFailed,
                    LogLevel.Error,
                    "MongoEventId.BulkUpdateFailed",
                    level => LoggerMessage.Define<string, CollectionNamespace, string, string, string, string>(
                        level,
                        MongoEventId.BulkUpdateFailed,
                        "Bulk Update failed ({elapsed} ms) Collection='{collectionNamespace}', Strategy={strategy}, Phase={phase}, "
                        + "Targets={targetCount}: {error}"))));

    // Shared by both failure events (the delete and update definitions have the same shape).
    private static string BulkFailedMessage(EventDefinitionBase definition, EventData payload)
    {
        var d = (BulkFailedEventDefinition)definition;
        var p = (MongoBulkOperationErrorEventData)payload;
        return d.GenerateMessage(
            p.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
            p.CollectionNamespace,
            p.Strategy,
            p.Phase,
            FormatTargetCount(p.TargetCount),
            FailureText(p.Exception, p.LogSensitiveData));
    }

    // A server error can quote document values (a duplicate-key error prints the key), so, like the command and MQL on the other paths,
    // the message is only logged with sensitive-data logging on; otherwise just the exception type is.
    private static string FailureText(Exception exception, bool logSensitiveData)
        => logSensitiveData ? exception.Message : exception.GetType().Name;

    // The logged command of a round trip that has none yet (the ReadTargets Executing event).
    private const string NoCommand = "-";

    private static string FormatTargetCount(long? targetCount)
        => targetCount?.ToString(CultureInfo.InvariantCulture) ?? "-";
#endif
}
