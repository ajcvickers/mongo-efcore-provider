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

namespace MongoDB.EntityFrameworkCore.Query.Expressions;

/// <summary>The date-part components translated natively.</summary>
/// <remarks>
/// <see cref="TimeOfDay"/> is produced only as a whole projection leaf (<c>TryTranslateTimeOfDayLeaf</c>), never by
/// the general value translator: it renders as milliseconds since midnight, so only a read through
/// <c>BsonSerializerFactory.TimeOfDayMillisecondsSerializer</c> gives the right <see cref="TimeSpan"/>; a filter, group
/// key or accumulator would misread those milliseconds. <see cref="TimeOfDayTicks"/> is the same leaf for a
/// <see cref="DateTimeOffset"/>, computed from the stored <c>Ticks</c> field as ticks since midnight: the stored
/// <c>DateTime</c> is a BSON date, which keeps whole milliseconds only (and rounds a pre-1970 value the wrong way).
/// Read through <c>BsonSerializerFactory.TimeOfDayTicksSerializer</c>.
/// </remarks>
internal enum MongoDatePart
{
    Year,
    Month,
    Day,
    Hour,
    Minute,
    Second,
    Millisecond,
    DayOfWeek,
    DayOfYear,
    Date,
    TimeOfDay,
    TimeOfDayTicks
}

/// <summary>
/// Extracts one <see cref="MongoDatePart"/> from a datetime-valued <see cref="Operand"/> (which may wrap a
/// <see cref="MongoDateTimeOffsetLocalExpression"/>) via the matching MQL date operator. Aggregation dialect only;
/// must never be admitted by <c>MongoQueryLanguageRenderer.IsQueryDialectRenderable</c>.
/// </summary>
internal sealed class MongoDatePartExpression(MongoExpression operand, MongoDatePart part) : MongoExpression
{
    public MongoExpression Operand { get; } = operand;

    public MongoDatePart Part { get; } = part;

    /// <summary>
    /// Whether <paramref name="node"/> is a <see cref="MongoDatePart.TimeOfDay"/> leaf (milliseconds, not a
    /// <see cref="TimeSpan"/>'s default stored form). The one predicate the emit-side declines and the read side share.
    /// </summary>
    internal static bool IsTimeOfDay(MongoExpression node)
        => node is MongoDatePartExpression { Part: MongoDatePart.TimeOfDay or MongoDatePart.TimeOfDayTicks };

    /// <inheritdoc />
    public override Type Type { get; } = part switch
    {
        MongoDatePart.Date => typeof(DateTime),
        MongoDatePart.DayOfWeek => typeof(DayOfWeek),
        MongoDatePart.TimeOfDay or MongoDatePart.TimeOfDayTicks => typeof(TimeSpan),
        _ => typeof(int)
    };
}
