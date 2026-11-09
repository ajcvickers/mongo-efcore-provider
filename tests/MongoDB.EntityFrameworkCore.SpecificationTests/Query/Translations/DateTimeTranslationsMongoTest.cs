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

public class DateTimeTranslationsMongoTest : DateTimeTranslationsTestBase<MongoPinnedBasicTypesQueryFixture>
{
    public DateTimeTranslationsMongoTest(MongoPinnedBasicTypesQueryFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
    public override async Task AddYear()
    {
        await base.AddYear();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$year" : { "$dateAdd" : { "startDate" : "$DateTime", "unit" : "year", "amount" : 1 } } }, 1999] } } }
    """);
    }

    // Fails (some data rows): Unspecified-kind DateTime constants/parameters are shifted by the local offset (timezone dependent) EF-X006
    public override Task Date()
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.XunitException>(() => base.Date(), "Assert.Equal() Failure: Values differ");

    public override async Task Day()
    {
        await base.Day();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$dayOfMonth" : "$DateTime" }, 4] } } }
    """);
    }

    public override async Task DayOfYear()
    {
        await base.DayOfYear();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$dayOfYear" : "$DateTime" }, 124] } } }
    """);
    }

    public override async Task Hour()
    {
        await base.Hour();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$hour" : "$DateTime" }, 15] } } }
    """);
    }

    public override async Task Millisecond()
    {
        await base.Millisecond();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$millisecond" : "$DateTime" }, 123] } } }
    """);
    }

    public override async Task Minute()
    {
        await base.Minute();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$minute" : "$DateTime" }, 30] } } }
    """);
    }

    public override async Task Month()
    {
        await base.Month();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$month" : "$DateTime" }, 5] } } }
    """);
    }

    // Fails (some data rows): Unspecified-kind DateTime constants/parameters are shifted by the local offset (timezone dependent) EF-X006
    public override Task New_with_constant()
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.XunitException>(() => base.New_with_constant(), "Assert.Equal() Failure: Values differ");

    // Fails (some data rows): Unspecified-kind DateTime constants/parameters are shifted by the local offset (timezone dependent) EF-X006
    public override Task New_with_parameters()
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.XunitException>(() => base.New_with_parameters(), "Assert.Equal() Failure: Values differ");

    public override async Task Now()
    {
        await base.Now();

        // The translated MQL embeds the current time, so it is not asserted.
    }

    // Fails (some data rows): Unspecified-kind DateTime constants/parameters are shifted by the local offset (timezone dependent) EF-X006
    public override Task Parse_with_constant()
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.XunitException>(() => base.Parse_with_constant(), "Assert.Equal() Failure: Values differ");

    // Fails (some data rows): Unspecified-kind DateTime constants/parameters are shifted by the local offset (timezone dependent) EF-X006
    public override Task Parse_with_parameter()
        => SpecFails.AsyncSomeRows<global::Xunit.Sdk.XunitException>(() => base.Parse_with_parameter(), "Assert.Equal() Failure: Values differ");

    public override async Task Second()
    {
        await base.Second();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$second" : "$DateTime" }, 10] } } }
    """);
    }

    public override async Task TimeOfDay()
    {
        await base.TimeOfDay();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$dateDiff" : { "startDate" : { "$dateTrunc" : { "date" : "$DateTime", "unit" : "day" } }, "endDate" : "$DateTime", "unit" : "millisecond" } }, 0] } } }
    """);
    }

    public override async Task Today()
    {
        await base.Today();

        // The translated MQL embeds today's date, so it is not asserted.
    }

    public override async Task UtcNow()
    {
        await base.UtcNow();

        // The translated MQL embeds the current time, so it is not asserted.
    }

    public override async Task Year()
    {
        await base.Year();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$eq" : [{ "$year" : "$DateTime" }, 1998] } } }
    """);
    }

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task subtract_and_TotalDays()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.subtract_and_TotalDays(), "Expression not supported: (b.DateTime -");
    // </generated>

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);
}

#endif
