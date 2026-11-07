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

#if EF10

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Translations;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.Query.Translations.Temporal;
namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query.Translations;

public class TimeOnlyTranslationsMongoTest : TimeOnlyTranslationsTestBase<MongoBasicTypesQueryFixture>
{
    public TimeOnlyTranslationsMongoTest(MongoBasicTypesQueryFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task AddHours()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.AddHours(), "Expression not supported: b.TimeOnly.AddHours(3).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task AddMinutes()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.AddMinutes(), "Expression not supported: b.TimeOnly.AddMinutes(3).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Add_TimeSpan()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Add_TimeSpan(), "Expression not supported: b.TimeOnly.Add(03:00:00).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task FromDateTime_compared_to_constant()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.FromDateTime_compared_to_constant(), "Expression not supported: FromDateTime(b.DateTime).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task FromDateTime_compared_to_parameter()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.FromDateTime_compared_to_parameter(), "Expression not supported: FromDateTime(b.DateTime).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task FromDateTime_compared_to_property()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.FromDateTime_compared_to_property(), "Expression not supported: FromDateTime(b.DateTime).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task FromTimeSpan_compared_to_parameter()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.FromTimeSpan_compared_to_parameter(), "Expression not supported: FromTimeSpan(b.TimeSpan).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task FromTimeSpan_compared_to_property()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.FromTimeSpan_compared_to_property(), "Expression not supported: FromTimeSpan(b.TimeSpan).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Hour()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Hour(), "Expression not supported: b.TimeOnly.Hour because serializer for Syste");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task IsBetween()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.IsBetween(), "Expression not supported: b.TimeOnly.IsBetween(2:00 PM, 4:00 PM).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Microsecond()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Microsecond(), "Expression not supported: b.TimeOnly.Microsecond because serializer fo");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Millisecond()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Millisecond(), "Expression not supported: b.TimeOnly.Millisecond because serializer fo");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Minute()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Minute(), "Expression not supported: b.TimeOnly.Minute because serializer for Sys");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Nanosecond()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Nanosecond(), "Expression not supported: b.TimeOnly.Nanosecond because serializer for");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Order_by_FromTimeSpan()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Order_by_FromTimeSpan(), "Expression not supported: FromTimeSpan(b.TimeSpan).");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Second()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Second(), "Expression not supported: b.TimeOnly.Second because serializer for Sys");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Subtract()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Subtract(), "Expression not supported: (b.TimeOnly - 3:00 AM).");
    // </generated>

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);
}

#endif
