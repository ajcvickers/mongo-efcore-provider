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

public class BitwiseOperatorTranslationsMongoTest : BitwiseOperatorTranslationsTestBase<MongoBasicTypesQueryFixture>
{
    public BitwiseOperatorTranslationsMongoTest(MongoBasicTypesQueryFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override Task Or()
        => base.Or();

    public override Task Or_over_boolean()
        => base.Or_over_boolean();

    public override Task Or_multiple()
        => base.Or_multiple();

    public override Task And()
        => base.And();

    public override Task And_over_boolean()
        => base.And_over_boolean();

    public override Task Xor()
        => base.Xor();

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: ((b.Int == Convert(b EF-X809
    public override Task Xor_over_boolean()
        => SpecFails.ThrowsAsync(() => base.Xor_over_boolean());

    public override Task Complement()
        => base.Complement();

    public override Task And_or_over_boolean()
        => base.And_or_over_boolean();

    public override Task Or_with_logical_or()
        => base.Or_with_logical_or();

    public override Task And_with_logical_and()
        => base.And_with_logical_and();

    public override Task Or_with_logical_and()
        => base.Or_with_logical_and();

    public override Task And_with_logical_or()
        => base.And_with_logical_or();

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: (b.Int << N). EF-X810
    public override Task Left_shift()
        => SpecFails.ThrowsAsync(() => base.Left_shift());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: (b.Int >> N). EF-X811
    public override Task Right_shift()
        => SpecFails.ThrowsAsync(() => base.Right_shift());

    // <generated>
    // </generated>

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);
}

#endif
