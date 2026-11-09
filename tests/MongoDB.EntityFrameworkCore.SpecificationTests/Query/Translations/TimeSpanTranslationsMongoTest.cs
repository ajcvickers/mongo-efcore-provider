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

public class TimeSpanTranslationsMongoTest : TimeSpanTranslationsTestBase<MongoPinnedBasicTypesQueryFixture>
{
    public TimeSpanTranslationsMongoTest(MongoPinnedBasicTypesQueryFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Hours()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Hours(), "Expression not supported: b.TimeSpan.Hours because serializer for Syst");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Microseconds()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Microseconds(), "Expression not supported: b.TimeSpan.Microseconds because serializer f");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Milliseconds()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Milliseconds(), "Expression not supported: b.TimeSpan.Milliseconds because serializer f");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Minutes()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Minutes(), "Expression not supported: b.TimeSpan.Minutes because serializer for Sy");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Nanoseconds()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Nanoseconds(), "Expression not supported: b.TimeSpan.Nanoseconds because serializer fo");

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task Seconds()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.Seconds(), "Expression not supported: b.TimeSpan.Seconds because serializer for Sy");
    // </generated>

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);
}

#endif
