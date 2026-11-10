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

    public override async Task Client_evaluation_of_uncorrelated_method_call(bool async)
    {
        await base.Client_evaluation_of_uncorrelated_method_call(async);

        AssertMql(
            """
OrderDetails.{ "$match" : { "UnitPrice" : { "$lt" : { "$numberDecimal" : "7" } }, "_id.ProductID" : { "$gt" : 10 } } }
""");
    }

    public override async Task Order_by_length_twice(bool async)
    {
        await base.Order_by_length_twice(async);

        AssertMql(
            """
Customers.{ "$set" : { "__sort0" : { "$cond" : { "if" : { "$eq" : [{ "$ifNull" : ["$_id", null] }, null] }, "then" : null, "else" : { "$strLenCP" : "$_id" } } }, "__sort1" : { "$cond" : { "if" : { "$eq" : [{ "$ifNull" : ["$_id", null] }, null] }, "then" : null, "else" : { "$strLenCP" : "$_id" } } } } }, { "$sort" : { "__sort0" : 1, "__sort1" : 1, "_id" : 1 } }, { "$unset" : ["__sort0", "__sort1"] }
""");
    }

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

    public override async Task Where_functions_nested(bool async)
    {
        await base.Where_functions_nested(async);

        AssertMql(
            """
Customers.{ "$match" : { "$expr" : { "$eq" : [{ "$pow" : [{ "$cond" : { "if" : { "$eq" : [{ "$ifNull" : ["$_id", null] }, null] }, "then" : null, "else" : { "$strLenCP" : "$_id" } } }, 2.0] }, 25.0] } } }
""");
    }

    // Fails: MongoDB DateTimeKind handling EF-X006 (depends on the time zone: passes in UTC) EF-X720
    public override Task Static_equals_nullable_datetime_compared_to_non_nullable(bool async)
        => SpecFails.EitherAsync(() => base.Static_equals_nullable_datetime_compared_to_non_nullable(async));

    public override async Task Static_equals_int_compared_to_long(bool async)
    {
        await base.Static_equals_int_compared_to_long(async);

        AssertMql(
            """
Orders.{ "$match" : { "_id" : { "$type" : -1 } } }
""");
    }

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    protected override void ClearLog()
        => Fixture.TestMqlLoggerFactory.Clear();
}

#endif
