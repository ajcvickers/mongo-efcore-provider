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

// From EF10 most of the NorthwindFunctionsQueryTestBase tests moved to the Translations.* suites (ported separately), leaving the ten
// below. The much larger EF8/EF9 version of this class, with MQL baselines, is in NorthwindFunctionsQueryMongoTest.cs.
#if !EF8 && !EF9

using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class NorthwindFunctionsQueryMongoTest : NorthwindFunctionsQueryTestBase<NorthwindQueryMongoFixture<NoopModelCustomizer>>
{
    public NorthwindFunctionsQueryMongoTest(NorthwindQueryMongoFixture<NoopModelCustomizer> fixture)
        : base(fixture)
    {
        ClearLog();
    }

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override Task Client_evaluation_of_uncorrelated_method_call(bool async)
        => base.Client_evaluation_of_uncorrelated_method_call(async);

    public override Task Order_by_length_twice(bool async)
        => base.Order_by_length_twice(async);

    // Fails: Cross-document navigation access issue EF-216
    public override Task Order_by_length_twice_followed_by_projection_of_naked_collection_navigation(bool async)
        => SpecFails.ThrowsAsync(() => base.Order_by_length_twice_followed_by_projection_of_naked_collection_navigation(async));

    // Fails: Subquery selection EF-X001
    public override Task Sum_over_round_works_correctly_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Sum_over_round_works_correctly_in_projection(async));

    // Fails: Subquery selection EF-X001
    public override Task Sum_over_round_works_correctly_in_projection_2(bool async)
        => SpecFails.ThrowsAsync(() => base.Sum_over_round_works_correctly_in_projection_2(async));

    // Fails: Subquery selection EF-X001
    public override Task Sum_over_truncate_works_correctly_in_projection(bool async)
        => SpecFails.ThrowsAsync(() => base.Sum_over_truncate_works_correctly_in_projection(async));

    // Fails: Subquery selection EF-X001
    public override Task Sum_over_truncate_works_correctly_in_projection_2(bool async)
        => SpecFails.ThrowsAsync(() => base.Sum_over_truncate_works_correctly_in_projection_2(async));

    public override Task Where_functions_nested(bool async)
        => base.Where_functions_nested(async);

    // Fails: MongoDB DateTimeKind handling EF-X006 (depends on the time zone: passes in UTC) EF-X720
    public override Task Static_equals_nullable_datetime_compared_to_non_nullable(bool async)
        => SpecFails.EitherAsync(() => base.Static_equals_nullable_datetime_compared_to_non_nullable(async));

    public override Task Static_equals_int_compared_to_long(bool async)
        => base.Static_equals_int_compared_to_long(async);

    protected override void ClearLog()
        => Fixture.TestMqlLoggerFactory.Clear();
}

#endif
