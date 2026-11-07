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

#if !EF8 && !EF9

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Translations;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.Query.Translations.Temporal;
namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query.Translations;

public class DateTimeOffsetTranslationsMongoTest : DateTimeOffsetTranslationsTestBase<MongoBasicTypesQueryFixture>
{
    public DateTimeOffsetTranslationsMongoTest(MongoBasicTypesQueryFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task AddDays()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.AddDays(), "Expression not supported: b.DateTimeOffset.AddDays(1).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task AddHours()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.AddHours(), "Expression not supported: b.DateTimeOffset.AddHours(1).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task AddMilliseconds()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.AddMilliseconds(), "Expression not supported: b.DateTimeOffset.AddMilliseconds(300).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task AddMinutes()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.AddMinutes(), "Expression not supported: b.DateTimeOffset.AddMinutes(1).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task AddMonths()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.AddMonths(), "Expression not supported: b.DateTimeOffset.AddMonths(1).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task AddSeconds()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.AddSeconds(), "Expression not supported: b.DateTimeOffset.AddSeconds(1).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task AddYears()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.AddYears(), "Expression not supported: b.DateTimeOffset.AddYears(1).");

    public override async Task Date()
    {
        await base.Date();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$gt" : [{ "$dateTrunc" : { "date" : { "$dateAdd" : { "startDate" : "$DateTimeOffset.DateTime", "unit" : "minute", "amount" : "$DateTimeOffset.Offset" } }, "unit" : "day" } }, { "$date" : { "$numberLong" : "-62135596800000" } }] } } }
    """);
    }

    public override async Task Day()
    {
        await base.Day();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$dayOfMonth" : { "$dateAdd" : { "startDate" : "$DateTimeOffset.DateTime", "unit" : "minute", "amount" : "$DateTimeOffset.Offset" } } }, 4] } } }
    """);
    }

    public override async Task DayOfYear()
    {
        await base.DayOfYear();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$dayOfYear" : { "$dateAdd" : { "startDate" : "$DateTimeOffset.DateTime", "unit" : "minute", "amount" : "$DateTimeOffset.Offset" } } }, 124] } } }
    """);
    }

    public override async Task Hour()
    {
        await base.Hour();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$hour" : { "$dateAdd" : { "startDate" : "$DateTimeOffset.DateTime", "unit" : "minute", "amount" : "$DateTimeOffset.Offset" } } }, 15] } } }
    """);
    }

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Microsecond()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Microsecond(), "Expression not supported: b.DateTimeOffset.Microsecond because seriali");

    public override async Task Millisecond()
    {
        await base.Millisecond();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$millisecond" : { "$dateAdd" : { "startDate" : "$DateTimeOffset.DateTime", "unit" : "minute", "amount" : "$DateTimeOffset.Offset" } } }, 123] } } }
    """);
    }

    public override async Task Milliseconds_parameter_and_constant()
    {
        await base.Milliseconds_parameter_and_constant();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "DateTimeOffset" : { "DateTime" : { "$date" : { "$numberLong" : "-2145799799876" } }, "Ticks" : 599898024001234567, "Offset" : 90 } } }, { "$count" : "_v" }
    """);
    }

    public override async Task Minute()
    {
        await base.Minute();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$minute" : { "$dateAdd" : { "startDate" : "$DateTimeOffset.DateTime", "unit" : "minute", "amount" : "$DateTimeOffset.Offset" } } }, 30] } } }
    """);
    }

    public override async Task Month()
    {
        await base.Month();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$month" : { "$dateAdd" : { "startDate" : "$DateTimeOffset.DateTime", "unit" : "minute", "amount" : "$DateTimeOffset.Offset" } } }, 5] } } }
    """);
    }

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Nanosecond()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Nanosecond(), "Expression not supported: b.DateTimeOffset.Nanosecond because serializ");

    public override async Task Now()
    {
        await base.Now();

        // The MQL contains the current time, so it is not asserted.
    }

    public override async Task Second()
    {
        await base.Second();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$second" : { "$dateAdd" : { "startDate" : "$DateTimeOffset.DateTime", "unit" : "minute", "amount" : "$DateTimeOffset.Offset" } } }, 10] } } }
    """);
    }

    public override async Task TimeOfDay()
    {
        await base.TimeOfDay();

        AssertMql(
    """
    BasicTypesEntities.{ "$project" : { "_v" : { "$dateDiff" : { "startDate" : { "$dateTrunc" : { "date" : { "$dateAdd" : { "startDate" : "$DateTimeOffset.DateTime", "unit" : "minute", "amount" : "$DateTimeOffset.Offset" } }, "unit" : "day" } }, "endDate" : { "$dateAdd" : { "startDate" : "$DateTimeOffset.DateTime", "unit" : "minute", "amount" : "$DateTimeOffset.Offset" } }, "unit" : "millisecond" } }, "_id" : 0 } }
    """);
    }

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task ToUnixTimeMilliseconds()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.ToUnixTimeMilliseconds(), "Expression not supported: b.DateTimeOffset.ToUnixTimeMilliseconds().");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task ToUnixTimeSecond()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.ToUnixTimeSecond(), "Expression not supported: b.DateTimeOffset.ToUnixTimeSeconds().");

    public override async Task UtcNow()
    {
        await base.UtcNow();

        // The MQL contains the current time, so it is not asserted.
    }

    public override async Task Year()
    {
        await base.Year();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$year" : { "$dateAdd" : { "startDate" : "$DateTimeOffset.DateTime", "unit" : "minute", "amount" : "$DateTimeOffset.Offset" } } }, 1998] } } }
    """);
    }
    // </generated>

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);
}

#endif
