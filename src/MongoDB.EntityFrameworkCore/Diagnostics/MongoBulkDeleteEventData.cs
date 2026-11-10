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
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Driver;

namespace MongoDB.EntityFrameworkCore.Diagnostics;

/// <summary>
/// A <see cref="DiagnosticSource" /> event payload class for EF bulk <c>ExecuteDelete</c> events.
/// </summary>
/// <remarks>
/// See <see href="https://aka.ms/efcore-docs-diagnostics">Logging, events, and diagnostics</see> for more information and examples.
/// </remarks>
public class MongoBulkDeleteEventData : EventData
{
    /// <summary>
    /// Constructs the event payload.
    /// </summary>
    /// <param name="eventDefinition">The event definition.</param>
    /// <param name="messageGenerator">A delegate that generates a log message for this event.</param>
    /// <param name="elapsed">
    /// For <c>SingleCommand</c>, the time since immediately before the <c>deleteMany</c>/<c>updateMany</c> call (so on the
    /// <c>Executing</c> event this is effectively zero, and on <c>Executed</c> it is the driver round-trip time). For
    /// <c>TwoPhase</c>, the time since immediately before the phase-1 read that collects the target ids (translation of that read
    /// included), so both the <c>Executing</c> and <c>Executed</c> events include phase 1 as well as phase 2.
    /// </param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> being operated upon.</param>
    /// <param name="deleteCount">The number of documents deleted by this bulk delete operation.</param>
    /// <param name="targetCount">
    /// For a two-phase bulk delete — the number of documents identified in phase one to be deleted by
    /// <c>_id</c>; <see langword="null" /> for a single-command bulk delete.
    /// </param>
    /// <param name="strategy">
    /// The execution strategy used for this operation: <c>SingleCommand</c> for a single server command, or
    /// <c>TwoPhase</c> for an id-collecting read followed by a write by <c>_id</c>.
    /// </param>
    /// <param name="command">
    /// The rendered MongoDB command for this operation, or <c>"?"</c> when sensitive-data logging is disabled.
    /// <see langword="null" /> on the <c>Executed</c> event, which does not carry the command.
    /// </param>
    /// <param name="logSensitiveData">
    /// Indicates whether the application allows logging of sensitive data. Always <see langword="false" /> on the <c>Executed</c>
    /// event, which carries nothing sensitive.
    /// </param>
    public MongoBulkDeleteEventData(
        EventDefinitionBase eventDefinition,
        Func<EventDefinitionBase, EventData, string> messageGenerator,
        TimeSpan elapsed,
        CollectionNamespace collectionNamespace,
        long deleteCount,
        long? targetCount,
        string strategy,
        string? command,
        bool logSensitiveData)
        : base(eventDefinition, messageGenerator)
    {
        Elapsed = elapsed;
        CollectionNamespace = collectionNamespace;
        DeleteCount = deleteCount;
        TargetCount = targetCount;
        Strategy = strategy;
        Command = command;
        LogSensitiveData = logSensitiveData;
    }

    /// <summary>
    /// For <c>SingleCommand</c>, the time since immediately before the <c>deleteMany</c>/<c>updateMany</c> call (so on the
    /// <c>Executing</c> event this is effectively zero, and on <c>Executed</c> it is the driver round-trip time). For
    /// <c>TwoPhase</c>, the time since immediately before the phase-1 read that collects the target ids (translation of that read
    /// included), so both the <c>Executing</c> and <c>Executed</c> events include phase 1 as well as phase 2.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// The <see cref="CollectionNamespace"/> being operated upon.
    /// </summary>
    public CollectionNamespace CollectionNamespace { get; }

    /// <summary>
    /// The number of documents deleted by this bulk delete operation.
    /// </summary>
    public long DeleteCount { get; }

    /// <summary>
    /// For a two-phase bulk delete — the number of documents identified in phase one to be deleted by
    /// <c>_id</c>; <see langword="null" /> for a single-command bulk delete.
    /// </summary>
    public long? TargetCount { get; }

    /// <summary>
    /// The execution strategy used for this operation: <c>SingleCommand</c> for a single server command, or
    /// <c>TwoPhase</c> for an id-collecting read followed by a write by <c>_id</c>.
    /// </summary>
    public string Strategy { get; }

    /// <summary>
    /// The rendered MongoDB command for this operation (the filter, and for an update the update document), or <c>"?"</c> when
    /// sensitive-data logging is disabled. <see langword="null" /> on the <c>Executed</c> event, which does not carry the command.
    /// </summary>
    public string? Command { get; }

    /// <summary>
    /// Indicates whether the application allows logging of sensitive data. Always <see langword="false" /> on the <c>Executed</c>
    /// event, which carries nothing sensitive.
    /// </summary>
    public bool LogSensitiveData { get; }
}

#endif
