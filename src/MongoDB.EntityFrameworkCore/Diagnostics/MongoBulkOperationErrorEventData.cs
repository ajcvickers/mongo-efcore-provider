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
/// A <see cref="DiagnosticSource" /> event payload class for the events raised when a bulk <c>ExecuteDelete</c> or
/// <c>ExecuteUpdate</c> database round trip fails (<see cref="MongoEventId.BulkDeleteFailed"/>, <see cref="MongoEventId.BulkUpdateFailed"/>).
/// </summary>
/// <remarks>
/// <para>
/// The failure event replaces the <c>Executed</c> event of the round trip that was open: every <c>Executing</c> event is followed by
/// exactly one <c>Executed</c> or failure event. A failure to commit the transaction of a two-phase operation is not a bulk round trip
/// and is reported by <see cref="MongoEventId.TransactionError"/> instead.
/// </para>
/// <para>
/// Each round trip is reported as it completes, so the <c>Executed</c> events of the batches that succeeded before a later round trip
/// failed have already been raised when the failure event arrives, and their writes are rolled back with the transaction. Do not total
/// the counts of an operation that ended in a failure event. A query shape the driver only rejects when the <c>ReadTargets</c> read is
/// first enumerated is reported as a <c>ReadTargets</c> failure whose <see cref="Exception"/> is EF's "could not be translated" error.
/// </para>
/// <para>
/// The log message carries the exception's message only when sensitive-data logging is on, and the log record has no exception object
/// attached; subscribers that need the exception (and its stack trace) read <see cref="Exception"/> from this payload.
/// </para>
/// <para>
/// See <see href="https://aka.ms/efcore-docs-diagnostics">Logging, events, and diagnostics</see> for more information and examples.
/// </para>
/// </remarks>
public class MongoBulkOperationErrorEventData : EventData, IErrorEventData
{
    /// <summary>
    /// Constructs the event payload.
    /// </summary>
    /// <param name="eventDefinition">The event definition.</param>
    /// <param name="messageGenerator">A delegate that generates a log message for this event.</param>
    /// <param name="elapsed">The time from immediately before the failed round trip began to the failure.</param>
    /// <param name="collectionNamespace">The <see cref="CollectionNamespace"/> being operated upon.</param>
    /// <param name="strategy">The execution strategy: <c>SingleCommand</c> or <c>TwoPhase</c>.</param>
    /// <param name="phase">The round trip that failed: <c>Command</c>, <c>ReadTargets</c> or <c>WriteTargets</c>.</param>
    /// <param name="targetCount">
    /// For a <c>WriteTargets</c> round trip, the number of ids in the <c>$in</c> filter of the batch that failed; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="logSensitiveData">
    /// Indicates whether the application allows logging of sensitive data. The log message of this event includes the exception's
    /// message (which can contain document values, for example in a duplicate-key error) only when it is <see langword="true" />.
    /// </param>
    public MongoBulkOperationErrorEventData(
        EventDefinitionBase eventDefinition,
        Func<EventDefinitionBase, EventData, string> messageGenerator,
        TimeSpan elapsed,
        CollectionNamespace collectionNamespace,
        string strategy,
        string phase,
        long? targetCount,
        Exception exception,
        bool logSensitiveData)
        : base(eventDefinition, messageGenerator)
    {
        Elapsed = elapsed;
        CollectionNamespace = collectionNamespace;
        Strategy = strategy;
        Phase = phase;
        TargetCount = targetCount;
        Exception = exception;
        LogSensitiveData = logSensitiveData;
    }

    /// <summary>
    /// The time from immediately before the failed round trip began (when its <c>Executing</c> event was raised) to the failure.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// The <see cref="CollectionNamespace"/> being operated upon.
    /// </summary>
    public CollectionNamespace CollectionNamespace { get; }

    /// <summary>
    /// The execution strategy used for this operation: <c>SingleCommand</c> for a single server command, or
    /// <c>TwoPhase</c> for an id-collecting read followed by a write by <c>_id</c>.
    /// </summary>
    public string Strategy { get; }

    /// <summary>
    /// The round trip that failed: <c>Command</c>, <c>ReadTargets</c> or <c>WriteTargets</c>. See
    /// <see cref="MongoBulkDeleteEventData.Phase"/>.
    /// </summary>
    public string Phase { get; }

    /// <summary>
    /// For a <c>WriteTargets</c> round trip, the number of ids in the <c>$in</c> filter of the batch that failed; otherwise
    /// <see langword="null" />. The earlier batches of a failed two-phase operation were rolled back with the rest of its transaction.
    /// </summary>
    public long? TargetCount { get; }

    /// <summary>
    /// The exception that was thrown.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Indicates whether the application allows logging of sensitive data. The log message of this event includes the exception's
    /// message (which can contain document values, for example in a duplicate-key error) only when it is <see langword="true" />;
    /// <see cref="Exception"/> itself is always carried by this payload.
    /// </summary>
    public bool LogSensitiveData { get; }
}

#endif
