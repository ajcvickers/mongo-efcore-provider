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
using Microsoft.EntityFrameworkCore.Query.Translations.Operators;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query.Translations.Operators;

public class ComparisonOperatorTranslationsMongoTest : ComparisonOperatorTranslationsTestBase<MongoBasicTypesQueryFixture>
{
    public ComparisonOperatorTranslationsMongoTest(MongoBasicTypesQueryFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override Task Equal()
        => base.Equal();

    public override Task NotEqual()
        => base.NotEqual();

    public override Task GreaterThan()
        => base.GreaterThan();

    public override Task GreaterThanOrEqual()
        => base.GreaterThanOrEqual();

    public override Task LessThan()
        => base.LessThan();

    public override Task LessThanOrEqual()
        => base.LessThanOrEqual();

    // <generated>
    // </generated>

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);
}

#endif
