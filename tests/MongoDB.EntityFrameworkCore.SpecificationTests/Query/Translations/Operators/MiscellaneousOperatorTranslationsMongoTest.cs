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

public class MiscellaneousOperatorTranslationsMongoTest : MiscellaneousOperatorTranslationsTestBase<MongoPinnedBasicTypesQueryFixture>
{
    public MiscellaneousOperatorTranslationsMongoTest(MongoPinnedBasicTypesQueryFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override Task Conditional()
        => base.Conditional();

    public override Task Coalesce()
        => base.Coalesce();

#if EF11
    public override Task Conditional_simplifiable_equality()
        => base.Conditional_simplifiable_equality();

    public override Task Conditional_simplifiable_inequality()
        => base.Conditional_simplifiable_inequality();

    public override Task Conditional_uncoalesce_with_equality_left()
        => base.Conditional_uncoalesce_with_equality_left();

    public override Task Conditional_uncoalesce_with_equality_right()
        => base.Conditional_uncoalesce_with_equality_right();

    public override Task Conditional_uncoalesce_with_inequality_left()
        => base.Conditional_uncoalesce_with_inequality_left();

    public override Task Conditional_uncoalesce_with_inequality_right()
        => base.Conditional_uncoalesce_with_inequality_right();

    public override Task Conditional_uncoalesce_with_string()
        => base.Conditional_uncoalesce_with_string();

    public override Task Conditional_uncoalesce_is_null()
        => base.Conditional_uncoalesce_is_null();

    public override Task Conditional_uncoalesce_is_not_null_with_nullable_column()
        => base.Conditional_uncoalesce_is_not_null_with_nullable_column();

    public override Task Conditional_uncoalesce_with_coalesce()
        => base.Conditional_uncoalesce_with_coalesce();

#endif

    // <generated>
    // </generated>

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);
}

#endif
