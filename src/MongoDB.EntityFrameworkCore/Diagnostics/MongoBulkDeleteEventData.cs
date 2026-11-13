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
    /// Constructs the event payload as it was before the strategy and command were added. <see cref="Strategy"/> is inferred from
    /// <paramref name="targetCount"/> (<see langword="null"/> means <c>SingleCommand</c>), <see cref="Command"/> is <see langword="null"/>
    /// and <see cref="LogSensitiveData"/> is <see langword="false"/>.
    /// </summary>
    /// <param name="eventDefinition">The event definition.</param>
    /// <param name="messageGenerator">A delegate that generates a log message for this event.</param>
    /// <param name="elapsed">The elapsed time (see <see cref="Elapsed"/>).</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> being operated upon.</param>
    /// <param name="deleteCount">The number of documents deleted by this bulk delete operation.</param>
    /// <param name="targetCount">The target count (see <see cref="TargetCount"/>).</param>
    public MongoBulkDeleteEventData(
        EventDefinitionBase eventDefinition,
        Func<EventDefinitionBase, EventData, string> messageGenerator,
        TimeSpan elapsed,
        CollectionNamespace collectionNamespace,
        long deleteCount,
        long? targetCount)
        : this(
            eventDefinition, messageGenerator, elapsed, collectionNamespace, deleteCount, targetCount,
            strategy: targetCount == null ? "SingleCommand" : "TwoPhase", command: null, logSensitiveData: false)
    {
    }

    /// <summary>
    /// Constructs the event payload as it was before the phase was added. <see cref="Phase"/> is inferred from
    /// <paramref name="strategy"/>: <c>Command</c> for <c>SingleCommand</c>, otherwise <c>WriteTargets</c>.
    /// </summary>
    /// <param name="eventDefinition">The event definition.</param>
    /// <param name="messageGenerator">A delegate that generates a log message for this event.</param>
    /// <param name="elapsed">The elapsed time (see <see cref="Elapsed"/>).</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> being operated upon.</param>
    /// <param name="deleteCount">The number of documents deleted by this bulk delete operation.</param>
    /// <param name="targetCount">The target count (see <see cref="TargetCount"/>).</param>
    /// <param name="strategy">The execution strategy (see <see cref="Strategy"/>).</param>
    /// <param name="command">The rendered command (see <see cref="Command"/>).</param>
    /// <param name="logSensitiveData">Whether sensitive-data logging is on (see <see cref="LogSensitiveData"/>).</param>
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
        : this(
            eventDefinition, messageGenerator, elapsed, collectionNamespace, deleteCount, targetCount, strategy,
            phase: strategy == "SingleCommand" ? "Command" : "WriteTargets", command, logSensitiveData)
    {
    }

    /// <summary>
    /// Constructs the event payload.
    /// </summary>
    /// <param name="eventDefinition">The event definition.</param>
    /// <param name="messageGenerator">A delegate that generates a log message for this event.</param>
    /// <param name="elapsed">
    /// The time since immediately before the database round trip this event describes began (see <see cref="Elapsed"/>).
    /// </param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> being operated upon.</param>
    /// <param name="deleteCount">The number of documents deleted by this round trip.</param>
    /// <param name="targetCount">The target count (see <see cref="TargetCount"/>).</param>
    /// <param name="strategy">
    /// The execution strategy used for this operation: <c>SingleCommand</c> for a single server command, or
    /// <c>TwoPhase</c> for an id-collecting read followed by a write by <c>_id</c>.
    /// </param>
    /// <param name="phase">
    /// The database round trip this event describes: <c>Command</c> (the one server command of a <c>SingleCommand</c> operation),
    /// <c>ReadTargets</c> (phase 1 of a <c>TwoPhase</c> operation, the read that collects the <c>_id</c> of every matching document) or
    /// <c>WriteTargets</c> (phase 2, the write by <c>_id</c>, once per batch of ids).
    /// </param>
    /// <param name="command">
    /// The rendered MongoDB command for this round trip, or <c>"?"</c> when sensitive-data logging is disabled.
    /// <see langword="null" /> on the <c>Executed</c> event, which does not carry the command, and on the <c>Executing</c> event of the
    /// <c>ReadTargets</c> phase (the pipeline is only known once that read has run; it is part of the command of the
    /// <c>WriteTargets</c> events).
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
        string phase,
        string? command,
        bool logSensitiveData)
        : base(eventDefinition, messageGenerator)
    {
        Elapsed = elapsed;
        CollectionNamespace = collectionNamespace;
        DeleteCount = deleteCount;
        TargetCount = targetCount;
        Strategy = strategy;
        Phase = phase;
        Command = command;
        LogSensitiveData = logSensitiveData;
    }

    /// <summary>
    /// The time since immediately before the database round trip this event describes began, so on the <c>Executing</c> event this is
    /// effectively zero and on <c>Executed</c> it is that round trip's duration. Every round trip is timed separately: a two-phase
    /// operation has one for the read that collects the target ids and one for each batch of ids written (see <see cref="Phase"/>).
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// The <see cref="CollectionNamespace"/> being operated upon.
    /// </summary>
    public CollectionNamespace CollectionNamespace { get; }

    /// <summary>
    /// The number of documents deleted by the round trip this event describes. Only meaningful on the <c>Executed</c> event of a
    /// <c>Command</c> or <c>WriteTargets</c> round trip; it is 0 on <c>Executing</c> and on <c>ReadTargets</c> events. A large two-phase
    /// operation is written in batches, each its own round trip with its own <c>Executed</c> event: sum <see cref="DeleteCount"/> over
    /// the <c>WriteTargets</c> <c>Executed</c> events for the total.
    /// </summary>
    public long DeleteCount { get; }

    /// <summary>
    /// For a two-phase bulk delete: on the <c>Executed</c> event of the <c>ReadTargets</c> phase, the number of ids found (0 when nothing
    /// matched, in which case no <c>WriteTargets</c> round trip follows); on the <c>WriteTargets</c> events, the number of ids in the
    /// <c>$in</c> filter of the batch THIS round trip writes (phase 2 is written in batches of up to 50,000 ids inside one transaction).
    /// <see langword="null" /> on the <c>Executing</c> event of the <c>ReadTargets</c> phase and for a single-command bulk delete.
    /// </summary>
    public long? TargetCount { get; }

    /// <summary>
    /// The execution strategy used for this operation: <c>SingleCommand</c> for a single server command, or
    /// <c>TwoPhase</c> for an id-collecting read followed by a write by <c>_id</c>.
    /// </summary>
    public string Strategy { get; }

    /// <summary>
    /// The database round trip this event describes: <c>Command</c> (the one server command of a <c>SingleCommand</c> operation),
    /// <c>ReadTargets</c> (phase 1 of a <c>TwoPhase</c> operation, the read that collects the <c>_id</c> of every matching document) or
    /// <c>WriteTargets</c> (phase 2, the write by <c>_id</c>, once per batch of ids). Each round trip raises its own <c>Executing</c>
    /// event followed by exactly one <c>Executed</c> or failure event.
    /// </summary>
    public string Phase { get; }

    /// <summary>
    /// The rendered MongoDB command for this round trip (the filter, and for an update the update document), or <c>"?"</c> when
    /// sensitive-data logging is disabled. <see langword="null" /> on the <c>Executed</c> event, which does not carry the command, and on
    /// the <c>Executing</c> event of the <c>ReadTargets</c> phase (the pipeline is only known once that read has run; it is part of the
    /// command of the <c>WriteTargets</c> events).
    /// </summary>
    public string? Command { get; }

    /// <summary>
    /// Indicates whether the application allows logging of sensitive data. Always <see langword="false" /> on the <c>Executed</c>
    /// event, which carries nothing sensitive.
    /// </summary>
    public bool LogSensitiveData { get; }
}

#endif
