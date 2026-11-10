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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class ComplexNavigationsQueryMongoTest : ComplexNavigationsQueryTestBase<ComplexNavigationsQueryMongoTest.ComplexNavigationsQueryMongoFixture>
{
    public ComplexNavigationsQueryMongoTest(ComplexNavigationsQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

#if EF11
    // Fails: new in the EF11 upstream package; asserts any failure until triaged. EF-X605 (depends on the time zone: passes in UTC) EF-X720
    public override Task Join_on_anonymous_type_with_single_property(bool async)
        => SpecFails.EitherAsync(() => base.Join_on_anonymous_type_with_single_property(async));

    // Fails: depends on the time zone: passes in UTC EF-X720
    public override Task Join_on_anonymous_type_with_multiple_properties(bool async)
        => SpecFails.EitherAsync(() => base.Join_on_anonymous_type_with_multiple_properties(async));
#endif

#if (EF10 || EF11)
    public override async Task Entity_equality_empty(bool async)
    {
        await base.Entity_equality_empty(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$match" : { "_inner._id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_when_sentinel_ef_property(bool async)
    {
        await base.Key_equality_when_sentinel_ef_property(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2._id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_using_property_method_required(bool async)
    {
        await base.Key_equality_using_property_method_required(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Required_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2._id" : { "$gt" : 7 } } }
""");
    }
#endif

    public override async Task Key_equality_using_property_method_required2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_required2(async), "Failure");
    }

#if (EF10 || EF11)
    public override async Task Key_equality_using_property_method_nested(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_nested(async), "Failure");
    }
#endif

    public override async Task Key_equality_using_property_method_nested2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_nested2(async), "Failure");
    }

#if (EF10 || EF11)
    public override async Task Key_equality_using_property_method_and_member_expression1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_and_member_expression1(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Key_equality_using_property_method_and_member_expression2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_and_member_expression2(async), "Failure");
    }
#endif

    public override async Task Key_equality_using_property_method_and_member_expression3(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_and_member_expression3(async), "Failure");
    }

    public override async Task Key_equality_navigation_converted_to_FK(bool async)
    {
        await base.Key_equality_navigation_converted_to_FK(async);

        AssertMql(
            """
LevelTwo.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_outer.Level1_Required_Id", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : "$_inner" }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$match" : { "_inner._id" : 1 } }
""");
    }

#if (EF10 || EF11)
    public override async Task Key_equality_two_conditions_on_same_navigation(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_two_conditions_on_same_navigation(async), "Failure");
    }
#endif

    public override async Task Key_equality_two_conditions_on_same_navigation2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_two_conditions_on_same_navigation2(async), "Failure");
    }

    public override async Task Multi_level_include_with_short_circuiting(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multi_level_include_with_short_circuiting(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Join_navigation_key_access_optional(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_navigation_key_access_optional(async), "Expression not supported");
    }
#endif

    public override async Task Join_navigation_key_access_required(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_navigation_key_access_required(async), "Expression not supported");
    }

#if (EF10 || EF11)
    public override async Task Navigation_key_access_optional_comparison(bool async)
    {
        await base.Navigation_key_access_optional_comparison(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "OneToOne_Optional_PK_Inverse2Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_PK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Optional_PK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_PK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Optional_PK_Inverse2._id" : { "$gt" : 5 } } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Simple_level1_include(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1_include(async), "Failure");
    }
#endif

    public override async Task Simple_level1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1(async), "Failure");
    }

#if (EF10 || EF11)
    public override async Task Simple_level1_level2_include(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1_level2_include(async), "Failure");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Simple_level1_level2_GroupBy_Count(bool async)
    {
        await base.Simple_level1_level2_GroupBy_Count(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_Level2" } }, { "$set" : { "_lookup_Level2" : { "$arrayElemAt" : ["$_lookup_Level2", 0] } } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_Level2._id", "foreignField" : "_id", "as" : "_lookup_Level3" } }, { "$set" : { "_lookup_Level3" : { "$arrayElemAt" : ["$_lookup_Level3", 0] } } }, { "$group" : { "_id" : "$_lookup_Level3.Name", "_v" : { "$sum" : 1 } } }, { "$project" : { "_v" : "$_v", "_id" : 0 } }
""");
    }
#else
    public override async Task Simple_level1_level2_GroupBy_Count(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Simple_level1_level2_GroupBy_Count(async), "could not be translated");
    }
#endif

    public override async Task Simple_level1_level2_GroupBy_Having_Count(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Simple_level1_level2_GroupBy_Having_Count(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Simple_level1_level2_level3_include(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1_level2_level3_include(async), "Failure");
    }
#endif

    public override async Task Navigation_key_access_required_comparison(bool async)
    {
        await base.Navigation_key_access_required_comparison(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "_id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_PK_Inverse2" } }, { "$match" : { "_lookup_OneToOne_Required_PK_Inverse2.0" : { "$exists" : true } } }, { "$set" : { "_lookup_OneToOne_Required_PK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_PK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Required_PK_Inverse2._id" : { "$gt" : 5 } } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }

#if (EF10 || EF11)
    public override async Task Navigation_inside_method_call_translated_to_join(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Navigation_inside_method_call_translated_to_join(async), "Failure");
    }
#endif

    public override async Task Navigation_inside_method_call_translated_to_join2(bool async)
    {
        await base.Navigation_inside_method_call_translated_to_join2(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse3" } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse3.0" : { "$exists" : true } } }, { "$set" : { "_lookup_OneToOne_Required_FK_Inverse3" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_FK_Inverse3", 0] } } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse3.Name" : { "$regularExpression" : { "pattern" : "^L", "options" : "s" } } } }
""");
    }

#if (EF10 || EF11)
    public override async Task Optional_navigation_inside_method_call_translated_to_join(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_method_call_translated_to_join(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_inside_property_method_translated_to_join(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_property_method_translated_to_join(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_inside_nested_method_call_translated_to_join(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_nested_method_call_translated_to_join(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Method_call_on_optional_navigation_translates_to_null_conditional_properly_for_arguments(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Method_call_on_optional_navigation_translates_to_null_conditional_properly_for_arguments(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_inside_method_call_translated_to_join_keeps_original_nullability(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_method_call_translated_to_join_keeps_original_nullability(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability_also_for_arguments(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability_also_for_arguments(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigation_in_outer_selector_translated_to_extra_join(bool async)
    {
        await base.Join_navigation_in_outer_selector_translated_to_extra_join(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_lookup_Level2._id", "foreignField" : "_id", "as" : "_lookup_Level2_1" } }, { "$match" : { "_lookup_Level2_1.0" : { "$exists" : true } } }, { "$set" : { "_lookup_Level2_1" : { "$arrayElemAt" : ["$_lookup_Level2_1", 0] } } }, { "$project" : { "Id1" : "$_id", "Id2" : "$_lookup_Level2_1._id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Join_navigation_in_outer_selector_translated_to_extra_join_nested(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Join_navigation_in_outer_selector_translated_to_extra_join_nested(async));
    }
#else
    public override async Task Join_navigation_in_outer_selector_translated_to_extra_join_nested(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_navigation_in_outer_selector_translated_to_extra_join_nested(async), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigation_in_outer_selector_translated_to_extra_join_nested2(bool async)
    {
        await base.Join_navigation_in_outer_selector_translated_to_extra_join_nested2(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse3" } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse3.0" : { "$exists" : true } } }, { "$set" : { "_lookup_OneToOne_Required_FK_Inverse3" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_FK_Inverse3", 0] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Required_FK_Inverse3.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_FK_Inverse2", 0] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Optional_FK_Inverse2._id", "foreignField" : "_id", "as" : "_lookup_Level1" } }, { "$match" : { "_lookup_Level1.0" : { "$exists" : true } } }, { "$set" : { "_lookup_Level1" : { "$arrayElemAt" : ["$_lookup_Level1", 0] } } }, { "$project" : { "Id3" : "$_id", "Id1" : "$_lookup_Level1._id", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigation_in_inner_selector(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_navigation_in_inner_selector(async), "Expression not supported");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigations_in_inner_selector_translated_without_collision(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_navigations_in_inner_selector_translated_without_collision(async), "Expression not supported");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigation_non_key_join(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_navigation_non_key_join(async), "Expression not supported");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_with_orderby_on_inner_sequence_navigation_non_key_join(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_with_orderby_on_inner_sequence_navigation_non_key_join(async), "Expression not supported");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigation_self_ref(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Join_navigation_self_ref(async), "Expression not supported");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigation_nested(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Join_navigation_nested(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigation_nested2(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Join_navigation_nested2(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task Join_navigation_deeply_nested_non_key_join(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Join_navigation_deeply_nested_non_key_join(async), "Unsupported cross-DbSet query");
    }
#endif

    public override async Task Join_navigation_deeply_nested_required(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Join_navigation_deeply_nested_required(async), "Unsupported cross-DbSet query");
    }

#if (EF10 || EF11)
    public override async Task Include_reference_and_project_into_anonymous_type(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include_reference_and_project_into_anonymous_type(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_nav_prop_reference_optional1(bool async)
    {
        await base.Select_nav_prop_reference_optional1(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level2.Name", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_nav_prop_reference_optional1_via_DefaultIfEmpty(bool async)
    {
        await base.Select_nav_prop_reference_optional1_via_DefaultIfEmpty(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level2.Name", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_nav_prop_reference_optional2(bool async)
    {
        await base.Select_nav_prop_reference_optional2(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level2._id", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_nav_prop_reference_optional2_via_DefaultIfEmpty(bool async)
    {
        await base.Select_nav_prop_reference_optional2_via_DefaultIfEmpty(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level2._id", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_nav_prop_reference_optional3(bool async)
    {
        await base.Select_nav_prop_reference_optional3(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_FK_Inverse2", 0] } } }, { "$project" : { "_v" : "$_lookup_OneToOne_Optional_FK_Inverse2.Name", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_nav_prop_reference_optional1(bool async)
    {
        await base.Where_nav_prop_reference_optional1(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "$or" : [{ "_lookup_Level2.Name" : "L2 05" }, { "_lookup_Level2.Name" : "L2 07" }] } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

    public override async Task Where_nav_prop_reference_optional1_via_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Where_nav_prop_reference_optional1_via_DefaultIfEmpty(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Where_nav_prop_reference_optional2(bool async)
    {
        await base.Where_nav_prop_reference_optional2(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "$or" : [{ "_lookup_Level2.Name" : "L2 05" }, { "_lookup_Level2.Name" : { "$ne" : "L2 42" } }] } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

    public override async Task Where_nav_prop_reference_optional2_via_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Where_nav_prop_reference_optional2_via_DefaultIfEmpty(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Select_multiple_nav_prop_reference_optional(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Select_multiple_nav_prop_reference_optional(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_member_compared_to_value(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Where_multiple_nav_prop_reference_optional_member_compared_to_value(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_member_compared_to_null(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Where_multiple_nav_prop_reference_optional_member_compared_to_null(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Where_multiple_nav_prop_reference_optional_compared_to_null1(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null2(bool async)
    {
        await base.Where_multiple_nav_prop_reference_optional_compared_to_null2(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse3" } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse3" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_FK_Inverse3", 0] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Optional_FK_Inverse3.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_FK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Optional_FK_Inverse2" : null } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null3(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Where_multiple_nav_prop_reference_optional_compared_to_null3(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null4(bool async)
    {
        await base.Where_multiple_nav_prop_reference_optional_compared_to_null4(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse3" } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse3" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_FK_Inverse3", 0] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Optional_FK_Inverse3.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_FK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ne" : null } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null5(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Where_multiple_nav_prop_reference_optional_compared_to_null5(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Select_multiple_nav_prop_reference_required(bool async)
    {
        await base.Select_multiple_nav_prop_reference_required(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Required_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_Level2._id", "foreignField" : "Level2_Required_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level3._id", "_id" : 0 } }
""");
    }
#endif

    public override async Task Select_multiple_nav_prop_reference_required2(bool async)
    {
        await base.Select_multiple_nav_prop_reference_required2(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse3" } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse3.0" : { "$exists" : true } } }, { "$set" : { "_lookup_OneToOne_Required_FK_Inverse3" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_FK_Inverse3", 0] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Required_FK_Inverse3.Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse2.0" : { "$exists" : true } } }, { "$set" : { "_lookup_OneToOne_Required_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_FK_Inverse2", 0] } } }, { "$project" : { "_v" : "$_lookup_OneToOne_Required_FK_Inverse2._id", "_id" : 0 } }
""");
    }

#if (EF10 || EF11)
    public override async Task Select_multiple_nav_prop_optional_required(bool async)
    {
        await base.Select_multiple_nav_prop_optional_required(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_Level2._id", "foreignField" : "Level2_Required_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level3._id", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_multiple_nav_prop_optional_required(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Where_multiple_nav_prop_optional_required(async), "Failure");
    }
#endif

    public override async Task SelectMany_navigation_comparison1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_comparison1(async), "could not be translated");
    }

    public override async Task SelectMany_navigation_comparison2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_comparison2(async), "could not be translated");
    }

    public override async Task SelectMany_navigation_comparison3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_comparison3(async), "could not be translated");
    }

    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Where_complex_predicate_with_with_nav_prop_and_OrElse1(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse2(bool async)
    {
        await base.Where_complex_predicate_with_with_nav_prop_and_OrElse2(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_Level2._id", "foreignField" : "Level2_Required_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level3" : { "$ifNull" : ["$_lookup_Level3", null] } } }, { "$match" : { "$or" : [{ "_lookup_Level3.Name" : "L3 05" }, { "_lookup_Level2.Name" : { "$ne" : "L2 05" } }] } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse3(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Where_complex_predicate_with_with_nav_prop_and_OrElse3(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse4(bool async)
    {
        await base.Where_complex_predicate_with_with_nav_prop_and_OrElse4(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse3" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Required_FK_Inverse3", "preserveNullAndEmptyArrays" : false } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse3" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse3", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse3" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse3", null] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Required_FK_Inverse3.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] } } }, { "$match" : { "$or" : [{ "_lookup_OneToOne_Optional_FK_Inverse3.Name" : { "$ne" : "L2 05" } }, { "_lookup_OneToOne_Optional_FK_Inverse2.Name" : "L1 05" }] } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

    public override async Task Complex_navigations_with_predicate_projected_into_anonymous_type(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Complex_navigations_with_predicate_projected_into_anonymous_type(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Complex_navigations_with_predicate_projected_into_anonymous_type2(bool async)
    {
        await base.Complex_navigations_with_predicate_projected_into_anonymous_type2(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse3" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Required_FK_Inverse3", "preserveNullAndEmptyArrays" : false } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Required_FK_Inverse3.Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Required_FK_Inverse2", "preserveNullAndEmptyArrays" : false } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Required_FK_Inverse3.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] } } }, { "$match" : { "$and" : [{ "$expr" : { "$ne" : ["$_lookup_OneToOne_Required_FK_Inverse2", "$_lookup_OneToOne_Optional_FK_Inverse2"] } }, { "_lookup_OneToOne_Optional_FK_Inverse2._id" : { "$ne" : 7 } }] } }, { "$project" : { "Name" : "$Name", "_id" : "$_lookup_OneToOne_Optional_FK_Inverse2._id" } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_projected_into_DTO(bool async)
    {
        await base.Optional_navigation_projected_into_DTO(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$project" : { "_id" : "$_v._outer._id", "Name" : "$_v._outer.Name", "Inner" : { "$cond" : { "if" : { "$ne" : ["$_v._inner", null] }, "then" : { "_id" : "$_v._inner._id", "Name" : "$_v._inner.Name" }, "else" : null } } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task OrderBy_nav_prop_reference_optional(bool async)
    {
        await base.OrderBy_nav_prop_reference_optional(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "__sort0" : "$_id" } }, { "$sort" : { "_lookup_Level2.Name" : 1, "__sort0" : 1 } }, { "$unset" : ["__sort0"] }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task OrderBy_nav_prop_reference_optional_via_DefaultIfEmpty(bool async)
    {
        await base.OrderBy_nav_prop_reference_optional_via_DefaultIfEmpty(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "__sort0" : "$_id" } }, { "$sort" : { "_lookup_Level2.Name" : 1, "__sort0" : 1 } }, { "$unset" : ["__sort0"] }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_Sum(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Sum(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$group" : { "_id" : null, "_v" : { "$sum" : "$_v._inner.Level1_Required_Id" } } }, { "$project" : { "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_Min(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Result_operator_nav_prop_reference_optional_Min(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_Max(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Max(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$group" : { "_id" : null, "_max" : { "$max" : { "_v" : "$_v._inner.Level1_Required_Id" } } } }, { "$replaceRoot" : { "newRoot" : "$_max" } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_Average(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Average(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$group" : { "_id" : null, "_v" : { "$avg" : "$_v._inner.Level1_Required_Id" } } }, { "$project" : { "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_Average_with_identity_selector(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Average_with_identity_selector(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$group" : { "_id" : null, "_v" : { "$avg" : "$_v._inner.Level1_Required_Id" } } }, { "$project" : { "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_Average_without_selector(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Average_without_selector(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level2.Level1_Required_Id", "_id" : 0 } }, { "$group" : { "_id" : null, "v" : { "$avg" : "$_v" } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Result_operator_nav_prop_reference_optional_via_DefaultIfEmpty(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_via_DefaultIfEmpty(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$group" : { "_id" : null, "_v" : { "$sum" : { "$cond" : { "if" : { "$eq" : ["$_v._inner", null] }, "then" : 0, "else" : "$_v._inner.Level1_Required_Id" } } } } }, { "$project" : { "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include_with_optional_navigation(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include_with_optional_navigation(async), "Failure");
    }
#endif

    public override async Task Join_flattening_bug_4539(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_flattening_bug_4539(async), "could not be translated");
    }

    public override async Task Query_source_materialization_bug_4547(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Query_source_materialization_bug_4547(async), "Unsupported cross-DbSet query");
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task SelectMany_navigation_property(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.SelectMany_navigation_property(async));
    }
#else
    public override async Task SelectMany_navigation_property(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_property(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task SelectMany_navigation_property_and_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.SelectMany_navigation_property_and_projection(async));
    }
#else
    public override async Task SelectMany_navigation_property_and_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_property_and_projection(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task SelectMany_navigation_property_and_filter_before(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.SelectMany_navigation_property_and_filter_before(async));
    }
#else
    public override async Task SelectMany_navigation_property_and_filter_before(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_property_and_filter_before(async), "could not be translated");
    }
#endif

    public override async Task SelectMany_navigation_property_and_filter_after(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_property_and_filter_after(async), "could not be translated");
    }

    public override async Task SelectMany_nested_navigation_property_required(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_nested_navigation_property_required(async), "could not be translated");
    }

    public override async Task SelectMany_nested_navigation_property_optional_and_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_nested_navigation_property_optional_and_projection(async), "could not be translated");
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Multiple_SelectMany_calls(bool async)
    {
        await base.Multiple_SelectMany_calls(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "OneToMany_Optional_Inverse2Id", "as" : "_lookup_OneToMany_Optional1" } }, { "$unwind" : { "path" : "$_lookup_OneToMany_Optional1", "preserveNullAndEmptyArrays" : false } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_OneToMany_Optional1._id", "foreignField" : "OneToMany_Optional_Inverse3Id", "as" : "_lookup_OneToMany_Optional2" } }, { "$unwind" : { "path" : "$_lookup_OneToMany_Optional2", "preserveNullAndEmptyArrays" : false } }, { "$replaceRoot" : { "newRoot" : "$_lookup_OneToMany_Optional2" } }
""");
    }
#else
    public override async Task Multiple_SelectMany_calls(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_SelectMany_calls(async), "could not be translated");
    }
#endif

    public override async Task SelectMany_navigation_property_with_another_navigation_in_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_navigation_property_with_another_navigation_in_subquery(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Where_navigation_property_to_collection(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Where_navigation_property_to_collection(async), "Unsupported cross-DbSet query");
    }
#endif

    public override async Task Where_navigation_property_to_collection2(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_navigation_property_to_collection2(async), "Expression not supported");
    }

    public override async Task Where_navigation_property_to_collection_of_original_entity_type(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Where_navigation_property_to_collection_of_original_entity_type(async), "Expression not supported");
    }

    public override async Task Correlated_subquery_doesnt_project_unnecessary_columns_in_top_level(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Correlated_subquery_doesnt_project_unnecessary_columns_in_top_level(async), "Unsupported cross-DbSet query");
    }

#if (EF10 || EF11)
    public override async Task Correlated_subquery_doesnt_project_unnecessary_columns_in_top_level_join(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Correlated_subquery_doesnt_project_unnecessary_columns_in_top_level_join(async), "Expression not supported");
    }
#endif

    public override async Task Correlated_nested_subquery_doesnt_project_unnecessary_columns_in_top_level(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Correlated_nested_subquery_doesnt_project_unnecessary_columns_in_top_level(async), "Unsupported cross-DbSet query");
    }

    public override async Task Correlated_nested_two_levels_up_subquery_doesnt_project_unnecessary_columns_in_top_level(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Correlated_nested_two_levels_up_subquery_doesnt_project_unnecessary_columns_in_top_level(async), "Unsupported cross-DbSet query");
    }

    public override async Task SelectMany_where_with_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_where_with_subquery(async), "could not be translated");
    }

    public override async Task Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access1(async), "Failure");
    }

    public override async Task Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access2(async), "Failure");
    }

    public override async Task Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access3(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access3(async), "Failure");
    }

    public override async Task Order_by_key_of_navigation_similar_to_projected_gets_optimized_into_FK_access(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Order_by_key_of_navigation_similar_to_projected_gets_optimized_into_FK_access(async), "Failure");
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(bool async)
    {
        await base.Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse3" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Required_FK_Inverse3", "preserveNullAndEmptyArrays" : false } }, { "$sort" : { "_lookup_OneToOne_Required_FK_Inverse3._id" : 1 } }, { "$limit" : 10 }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Required_FK_Inverse3.Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Required_FK_Inverse2", "preserveNullAndEmptyArrays" : false } }, { "$project" : { "_v" : "$_lookup_OneToOne_Required_FK_Inverse2.Name", "_id" : 0 } }
""");
    }
#else
    public override async Task Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.ThrowsAsync(() => base.Order_by_key_of_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(async), "composed between two cross-collection joins");
    }
#endif

    public override async Task Order_by_key_of_anonymous_type_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(bool async)
    {
        await base.Order_by_key_of_anonymous_type_projected_navigation_doesnt_get_optimized_into_FK_access_subquery(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse3" } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse3.0" : { "$exists" : true } } }, { "$set" : { "_lookup_OneToOne_Required_FK_Inverse3" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_FK_Inverse3", 0] } } }, { "$sort" : { "_lookup_OneToOne_Required_FK_Inverse3._id" : 1 } }, { "$limit" : 10 }, { "$project" : { "_v" : "$_lookup_OneToOne_Required_FK_Inverse3.Name", "_id" : 0 } }
""");
    }

#if (EF10 || EF11)
    public override async Task Optional_navigation_take_optional_navigation(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Optional_navigation_take_optional_navigation(async));
    }
#endif

    public override async Task Projection_select_correct_table_from_subquery_when_materialization_is_not_required(bool async)
    {
        await base.Projection_select_correct_table_from_subquery_when_materialization_is_not_required(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse2.0" : { "$exists" : true } } }, { "$set" : { "_lookup_OneToOne_Required_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_FK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse2.Name" : "L1 03" } }, { "$set" : { "__sort0" : "$_id" } }, { "$sort" : { "__sort0" : 1 } }, { "$unset" : ["__sort0"] }, { "$limit" : 3 }, { "$project" : { "_v" : "$Name", "_id" : 0 } }
""");
    }

    public override async Task Projection_select_correct_table_with_anonymous_projection_in_subquery(bool async)
    {
        await base.Projection_select_correct_table_with_anonymous_projection_in_subquery(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Required_FK_Inverse2", "preserveNullAndEmptyArrays" : false } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_OneToOne_Required_FK_Inverse2._id", "foreignField" : "Level2_Required_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : false } }, { "$sort" : { "_lookup_OneToOne_Required_FK_Inverse2._id" : 1 } }, { "$limit" : 3 }, { "$project" : { "_v" : "$Name", "_id" : 0 } }
""");
    }

    public override async Task Projection_select_correct_table_in_subquery_when_materialization_is_not_required_in_multiple_joins(bool async)
    {
        await base.Projection_select_correct_table_in_subquery_when_materialization_is_not_required_in_multiple_joins(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Required_FK_Inverse2", "preserveNullAndEmptyArrays" : false } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_OneToOne_Required_FK_Inverse2._id", "foreignField" : "Level2_Required_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : false } }, { "$sort" : { "_lookup_OneToOne_Required_FK_Inverse2._id" : 1 } }, { "$limit" : 3 }, { "$project" : { "_v" : "$_lookup_OneToOne_Required_FK_Inverse2.Name", "_id" : 0 } }
""");
    }

#if (EF10 || EF11)
    public override async Task Where_predicate_on_optional_reference_navigation(bool async)
    {
        await base.Where_predicate_on_optional_reference_navigation(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Required_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2.Name" : "L2 03" } }, { "$set" : { "__sort0" : "$_id" } }, { "$sort" : { "__sort0" : 1 } }, { "$unset" : ["__sort0"] }, { "$limit" : 3 }, { "$project" : { "_v" : "$Name", "_id" : 0 } }
""");
    }
#endif

    public override async Task SelectMany_with_string_based_Include1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_string_based_Include1(async), "could not be translated");
    }

    public override async Task SelectMany_with_EF_Property_Include1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_EF_Property_Include1(async), "could not be translated");
    }

    public override async Task SelectMany_with_string_based_Include2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_string_based_Include2(async), "could not be translated");
    }

    public override async Task Multiple_SelectMany_with_string_based_Include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_SelectMany_with_string_based_Include(async), "could not be translated");
    }

    public override async Task Multiple_SelectMany_with_EF_Property_Include(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_SelectMany_with_EF_Property_Include(async), "could not be translated");
    }

    public override async Task Multiple_required_navigations_with_Include(bool async)
    {
        await base.Multiple_required_navigations_with_Include(async);

        AssertMql();
    }

    public override async Task Multiple_required_navigation_using_multiple_selects_with_Include(bool async)
    {
        await base.Multiple_required_navigation_using_multiple_selects_with_Include(async);

        AssertMql();
    }

    public override async Task Multiple_required_navigation_with_string_based_Include(bool async)
    {
        await base.Multiple_required_navigation_with_string_based_Include(async);

        AssertMql();
    }

    public override async Task Multiple_required_navigation_with_EF_Property_Include(bool async)
    {
        await base.Multiple_required_navigation_with_EF_Property_Include(async);

        AssertMql();
    }

    public override async Task Multiple_required_navigation_using_multiple_selects_with_string_based_Include(bool async)
    {
        await base.Multiple_required_navigation_using_multiple_selects_with_string_based_Include(async);

        AssertMql();
    }

    public override async Task Multiple_required_navigation_using_multiple_selects_with_EF_Property_Include(bool async)
    {
        await base.Multiple_required_navigation_using_multiple_selects_with_EF_Property_Include(async);

        AssertMql();
    }

    public override async Task Optional_navigation_with_Include(bool async)
    {
        await base.Optional_navigation_with_Include(async);

        AssertMql();
    }

    public override async Task SelectMany_with_navigation_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_navigation_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task SelectMany_with_navigation_filter_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_navigation_filter_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigation_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigation_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigation_filter_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigation_filter_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_required_navigation_filter_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_required_navigation_filter_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigations_and_additional_joins_outside_of_SelectMany(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigations_and_additional_joins_outside_of_SelectMany(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany2(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany3(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany4(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigations_explicit_DefaultIfEmpty_and_additional_joins_outside_of_SelectMany4(async), "could not be translated");
    }

    public override async Task Multiple_SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_joined_together(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_joined_together(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_followed_by_Select_required_navigation_using_same_navs(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_followed_by_Select_required_navigation_using_same_navs(async), "could not be translated");
    }

    public override async Task SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_followed_by_Select_required_navigation_using_different_navs(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_followed_by_Select_required_navigation_using_different_navs(async), "could not be translated");
    }

    public override async Task Multiple_SelectMany_with_navigation_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_SelectMany_with_navigation_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task SelectMany_with_navigation_filter_paging_and_explicit_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_navigation_filter_paging_and_explicit_DefaultIfEmpty(async), "could not be translated");
    }

    public override async Task Select_join_subquery_containing_filter_and_distinct(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Select_join_subquery_containing_filter_and_distinct(async), "Expression not supported");
    }

    public override async Task Select_join_with_key_selector_being_a_subquery(bool async)
    {
        // Fails: Join key selector that is a sub-query: serializer cannot be created for MongoQuery EF-X534
        await SpecFails.ThrowsAsync(() => base.Select_join_with_key_selector_being_a_subquery(async), "BsonSerializationException");
    }

#if (EF10 || EF11)
    public override async Task Contains_with_subquery_optional_navigation_and_constant_item(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Contains_with_subquery_optional_navigation_and_constant_item(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task Contains_with_subquery_optional_navigation_scalar_distinct_and_constant_item(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Contains_with_subquery_optional_navigation_scalar_distinct_and_constant_item(async), "Unsupported cross-DbSet query");
    }
#endif

    public override async Task Complex_query_with_optional_navigations_and_client_side_evaluation(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Complex_query_with_optional_navigations_and_client_side_evaluation(async), "Unsupported cross-DbSet query");
    }

    public override async Task Required_navigation_on_a_subquery_with_First_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Required_navigation_on_a_subquery_with_First_in_projection(async), "could not be translated");
    }

    public override async Task Required_navigation_on_a_subquery_with_complex_projection_and_First(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Required_navigation_on_a_subquery_with_complex_projection_and_First(async), "could not be translated");
    }

    public override async Task Required_navigation_on_a_subquery_with_First_in_predicate(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Required_navigation_on_a_subquery_with_First_in_predicate(async), "Unsupported cross-DbSet query");
    }

#if (EF10 || EF11)
    public override async Task Manually_created_left_join_propagates_nullability_to_navigations(bool async)
    {
        await base.Manually_created_left_join_propagates_nullability_to_navigations(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_Level2.Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Required_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_FK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse2.Name" : { "$ne" : "L3 02" } } }, { "$project" : { "_v" : "$_lookup_OneToOne_Required_FK_Inverse2.Name", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Optional_navigation_propagates_nullability_to_manually_created_left_join1(bool async)
    {
        await base.Optional_navigation_propagates_nullability_to_manually_created_left_join1(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_lookup_Level2.Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_Level2_1" } }, { "$set" : { "_lookup_Level2_1" : { "$arrayElemAt" : ["$_lookup_Level2_1", 0] } } }, { "$project" : { "Id1" : "$_lookup_Level2._id", "Id2" : "$_lookup_Level2_1._id", "_id" : 0 } }
""");
    }
#else
    public override async Task Optional_navigation_propagates_nullability_to_manually_created_left_join1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_propagates_nullability_to_manually_created_left_join1(async), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Optional_navigation_propagates_nullability_to_manually_created_left_join2(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Optional_navigation_propagates_nullability_to_manually_created_left_join2(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Null_reference_protection_complex(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Null_reference_protection_complex(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Null_reference_protection_complex_materialization(bool async)
    {
        // Fails: ArgumentNullException (null string/source argument) during translation or materialization EF-X531
        await SpecFails.ThrowsAsync(() => base.Null_reference_protection_complex_materialization(async), "ArgumentNullException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Null_reference_protection_complex_client_eval(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.Null_reference_protection_complex_client_eval(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened2(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened2(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened3(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened3(async), "ArgumentException");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.TranslationFailedAsync(() => base.GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer_with_client_method(bool async)
    {
        // Fails: an unsupported query now throws a different exception type than the base test expects (EF-X705)
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.XunitException>(() => base.GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer_with_client_method(async), "Exception type was not an exact match");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_inner(bool async)
    {
        await base.GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_inner(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$sort" : { "_id" : 1 } }, { "$limit" : 2 }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_Level2.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] } } }, { "$project" : { "_v" : "$_lookup_OneToOne_Optional_FK_Inverse2.Name", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_with_orderby_on_inner_sequence_projecting_inner(bool async)
    {
        await base.GroupJoin_on_a_subquery_containing_another_GroupJoin_with_orderby_on_inner_sequence_projecting_inner(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$sort" : { "_id" : 1 } }, { "$limit" : 2 }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_Level2.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] } } }, { "$project" : { "_v" : "$_lookup_OneToOne_Optional_FK_Inverse2.Name", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_on_left_side_being_a_subquery(bool async)
    {
        await base.GroupJoin_on_left_side_being_a_subquery(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "__sort0" : "$_id" } }, { "$sort" : { "_lookup_Level2.Name" : 1, "__sort0" : 1 } }, { "$unset" : ["__sort0"] }, { "$limit" : 2 }, { "$project" : { "Id" : "$_id", "Brand" : "$_lookup_Level2.Name", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_on_right_side_being_a_subquery(bool async)
    {
        // Fails: Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) EF-X533
        await SpecFails.ThrowsAsync(() => base.GroupJoin_on_right_side_being_a_subquery(async), "ArgumentException");
    }
#endif

    public override async Task GroupJoin_in_subquery_with_client_result_operator(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.GroupJoin_in_subquery_with_client_result_operator(async), "Unsupported cross-DbSet query");
    }

    public override async Task GroupJoin_in_subquery_with_client_projection(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.GroupJoin_in_subquery_with_client_projection(async), "Unsupported cross-DbSet query");
    }

    public override async Task GroupJoin_in_subquery_with_client_projection_nested1(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.GroupJoin_in_subquery_with_client_projection_nested1(async), "Unsupported cross-DbSet query");
    }

    public override async Task GroupJoin_in_subquery_with_client_projection_nested2(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.GroupJoin_in_subquery_with_client_projection_nested2(async), "Unsupported cross-DbSet query");
    }

#if (EF10 || EF11)
    public override async Task GroupJoin_client_method_on_outer(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.GroupJoin_client_method_on_outer(async), "Expression not supported");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_client_method_in_OrderBy(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.GroupJoin_client_method_in_OrderBy(async), "Expression not supported");
    }
#endif

    public override async Task GroupJoin_without_DefaultIfEmpty(bool async)
    {
        await base.GroupJoin_without_DefaultIfEmpty(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : false } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }

    public override async Task GroupJoin_with_subquery_on_inner(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupJoin_with_subquery_on_inner(async), "could not be translated");
    }

    public override async Task GroupJoin_with_subquery_on_inner_and_no_DefaultIfEmpty(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupJoin_with_subquery_on_inner_and_no_DefaultIfEmpty(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Optional_navigation_in_subquery_with_unrelated_projection(bool async)
    {
        await base.Optional_navigation_in_subquery_with_unrelated_projection(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2.Name" : { "$ne" : "Foo" } } }, { "$set" : { "__sort0" : "$_id" } }, { "$sort" : { "__sort0" : 1 } }, { "$unset" : ["__sort0"] }, { "$limit" : 15 }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection(bool async)
    {
        await base.Explicit_GroupJoin_in_subquery_with_unrelated_projection(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2.Name" : { "$ne" : "Foo" } } }, { "$set" : { "__sort0" : "$_id" } }, { "$sort" : { "__sort0" : 1 } }, { "$unset" : ["__sort0"] }, { "$limit" : 15 }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection2(bool async)
    {
        await base.Explicit_GroupJoin_in_subquery_with_unrelated_projection2(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2.Name" : { "$ne" : "Foo" } } }, { "$unset" : ["_lookup_Level2"] }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }, { "$project" : { "_id" : "$_id" } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection3(bool async)
    {
        await base.Explicit_GroupJoin_in_subquery_with_unrelated_projection3(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2.Name" : { "$ne" : "Foo" } } }, { "$group" : { "_id" : { "_v" : "$_id", "_v__isMissing" : { "$eq" : [{ "$type" : "$_id" }, "missing"] } } } }, { "$project" : { "_v" : { "$cond" : { "if" : "$_id._v__isMissing", "then" : "$$REMOVE", "else" : "$_id._v" } }, "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection4(bool async)
    {
        await base.Explicit_GroupJoin_in_subquery_with_unrelated_projection4(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$match" : { "_lookup_Level2.Name" : { "$ne" : "Foo" } } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }, { "$sort" : { "_v" : 1 } }, { "$limit" : 20 }
""");
    }
#endif

    public override async Task Explicit_GroupJoin_in_subquery_with_scalar_result_operator(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Explicit_GroupJoin_in_subquery_with_scalar_result_operator(async), "Unsupported cross-DbSet query");
    }

    public override async Task Explicit_GroupJoin_in_subquery_with_multiple_result_operator_distinct_count_materializes_main_clause(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Explicit_GroupJoin_in_subquery_with_multiple_result_operator_distinct_count_materializes_main_clause(async), "Unsupported cross-DbSet query");
    }

    public override async Task Where_on_multilevel_reference_in_subquery_with_outer_projection(bool async)
    {
        await base.Where_on_multilevel_reference_in_subquery_with_outer_projection(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "OneToMany_Required_Inverse3Id", "foreignField" : "_id", "as" : "_lookup_OneToMany_Required_Inverse3" } }, { "$unwind" : { "path" : "$_lookup_OneToMany_Required_Inverse3", "preserveNullAndEmptyArrays" : false } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToMany_Required_Inverse3.Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Required_FK_Inverse2", "preserveNullAndEmptyArrays" : false } }, { "$match" : { "$or" : [{ "_lookup_OneToOne_Required_FK_Inverse2.Name" : "L1 10" }, { "_lookup_OneToOne_Required_FK_Inverse2.Name" : "L1 01" }] } }, { "$sort" : { "Level2_Required_Id" : 1 } }, { "$skip" : 0 }, { "$limit" : 10 }, { "$project" : { "_v" : "$Name", "_id" : 0 } }
""");
    }

#if !EF11
    public override async Task Join_condition_optimizations_applied_correctly_when_anonymous_type_with_single_property(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Join_condition_optimizations_applied_correctly_when_anonymous_type_with_single_property(async));
    }

    public override async Task Join_condition_optimizations_applied_correctly_when_anonymous_type_with_multiple_properties(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Join_condition_optimizations_applied_correctly_when_anonymous_type_with_multiple_properties(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Nested_group_join_with_take(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Nested_group_join_with_take(async));
    }
#endif

    public override async Task Navigation_with_same_navigation_compared_to_null(bool async)
    {
        await base.Navigation_with_same_navigation_compared_to_null(async);

        AssertMql(
            """
LevelTwo.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_outer.OneToMany_Required_Inverse2Id", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : "$_inner" }, { "$project" : { "Outer" : "$_outer", "Inner" : "$_inner", "_id" : 0 } }, { "$match" : { "Inner.Name" : { "$ne" : "L1 07" }, "Inner" : { "$ne" : null } } }, { "$project" : { "_v" : "$Outer._id", "_id" : 0 } }
""");
    }

#if (EF10 || EF11)
    public override async Task Multi_level_navigation_compared_to_null(bool async)
    {
        await base.Multi_level_navigation_compared_to_null(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "OneToMany_Optional_Inverse3Id", "foreignField" : "_id", "as" : "_lookup_OneToMany_Optional_Inverse3" } }, { "$set" : { "_lookup_OneToMany_Optional_Inverse3" : { "$arrayElemAt" : ["$_lookup_OneToMany_Optional_Inverse3", 0] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToMany_Optional_Inverse3.Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Required_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_FK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse2" : { "$ne" : null } } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Multi_level_navigation_with_same_navigation_compared_to_null(bool async)
    {
        await base.Multi_level_navigation_with_same_navigation_compared_to_null(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "OneToMany_Optional_Inverse3Id", "foreignField" : "_id", "as" : "_lookup_OneToMany_Optional_Inverse3" } }, { "$set" : { "_lookup_OneToMany_Optional_Inverse3" : { "$arrayElemAt" : ["$_lookup_OneToMany_Optional_Inverse3", 0] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToMany_Optional_Inverse3.Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Required_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_FK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse2.Name" : { "$ne" : "L1 07" }, "_lookup_OneToOne_Required_FK_Inverse2" : { "$ne" : null } } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

    public override async Task Navigations_compared_to_each_other1(bool async)
    {
        await base.Navigations_compared_to_each_other1(async);

        AssertMql(
            """
LevelTwo.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_outer.OneToMany_Required_Inverse2Id", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : "$_inner" }, { "$project" : { "Outer" : "$_outer", "Inner" : "$_inner", "_id" : 0 } }, { "$match" : { "$expr" : { "$eq" : ["$Inner", "$Inner"] } } }, { "$project" : { "_v" : "$Outer.Name", "_id" : 0 } }
""");
    }

#if (EF10 || EF11)
    public override async Task Navigations_compared_to_each_other2(bool async)
    {
        await base.Navigations_compared_to_each_other2(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "OneToOne_Optional_PK_Inverse2Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_PK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_PK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_PK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_PK_Inverse2", null] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "OneToMany_Required_Inverse2Id", "foreignField" : "_id", "as" : "_lookup_OneToMany_Required_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToMany_Required_Inverse2", "preserveNullAndEmptyArrays" : false } }, { "$match" : { "$expr" : { "$eq" : ["$_lookup_OneToMany_Required_Inverse2", "$_lookup_OneToOne_Optional_PK_Inverse2"] } } }, { "$project" : { "_v" : "$Name", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Navigations_compared_to_each_other3(bool async)
    {
        await base.Navigations_compared_to_each_other3(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelThree", "localField" : "_id", "foreignField" : "OneToMany_Optional_Inverse3Id", "as" : "_lookup_OneToMany_Optional2" } }, { "$match" : { "_lookup_OneToMany_Optional2.0" : { "$exists" : true } } }, { "$project" : { "Name" : "$Name", "_id" : 0 } }
""");
    }
#else
    public override async Task Navigations_compared_to_each_other3(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Navigations_compared_to_each_other3(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task Navigations_compared_to_each_other4(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Navigations_compared_to_each_other4(async), "Unsupported cross-DbSet query");
    }
#endif

#if (EF10 || EF11)
    public override async Task Navigations_compared_to_each_other5(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Navigations_compared_to_each_other5(async), "Unsupported cross-DbSet query");
    }
#endif

    public override async Task Level4_Include(bool async)
    {
        await base.Level4_Include(async);

        AssertMql();
    }

#if (EF10 || EF11)
    public override async Task Comparing_collection_navigation_on_optional_reference_to_null(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Comparing_collection_navigation_on_optional_reference_to_null(async), "Unsupported cross-DbSet query");
    }
#endif

    public override async Task Select_subquery_with_client_eval_and_navigation1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_with_client_eval_and_navigation1(async), "could not be translated");
    }

    public override async Task Select_subquery_with_client_eval_and_navigation2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_with_client_eval_and_navigation2(async), "could not be translated");
    }

    public override async Task Select_subquery_with_client_eval_and_multi_level_navigation(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_subquery_with_client_eval_and_multi_level_navigation(async), "could not be translated");
    }

    public override async Task Member_doesnt_get_pushed_down_into_subquery_with_result_operator(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Member_doesnt_get_pushed_down_into_subquery_with_result_operator(async), "could not be translated");
    }

    public override async Task Subquery_with_Distinct_Skip_FirstOrDefault_without_OrderBy(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Subquery_with_Distinct_Skip_FirstOrDefault_without_OrderBy(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Project_collection_navigation_count(bool async)
    {
        // Fails: Server error executing the generated pipeline EF-X528
        await SpecFails.TranslationFailedAsync(() => base.Project_collection_navigation_count(async));
    }
#endif

    public override async Task Select_optional_navigation_property_string_concat(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Select_optional_navigation_property_string_concat(async), "could not be translated");
    }

    public override async Task Entries_for_detached_entities_are_removed(bool async)
    {
        await base.Entries_for_detached_entities_are_removed(async);

        AssertMql(
            """
LevelTwo.{ "$sort" : { "_id" : 1 } }, { "$limit" : 1 }
""");
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Include_reference_with_groupby_in_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Include_reference_with_groupby_in_subquery(async));
    }
#else
    public override async Task Include_reference_with_groupby_in_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include_reference_with_groupby_in_subquery(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Multi_include_with_groupby_in_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Multi_include_with_groupby_in_subquery(async));
    }
#else
    public override async Task Multi_include_with_groupby_in_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multi_include_with_groupby_in_subquery(async), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task String_include_multiple_derived_navigation_with_same_name_and_same_type(bool async)
    {
        await base.String_include_multiple_derived_navigation_with_same_name_and_same_type(async);

        AssertMql(
            """
InheritanceOne.{ "$lookup" : { "from" : "InheritanceLeafOne", "localField" : "_id", "foreignField" : "SameTypeReference_InheritanceDerived2Id", "as" : "_lookup_InheritanceLeaf1_1" } }, { "$unwind" : { "path" : "$_lookup_InheritanceLeaf1_1", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_InheritanceLeaf1_1" : { "$ifNull" : ["$_lookup_InheritanceLeaf1_1", null] } } }, { "$lookup" : { "from" : "InheritanceLeafOne", "localField" : "_id", "foreignField" : "SameTypeReference_InheritanceDerived1Id", "as" : "_lookup_InheritanceLeaf1" } }, { "$unwind" : { "path" : "$_lookup_InheritanceLeaf1", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_InheritanceLeaf1" : { "$ifNull" : ["$_lookup_InheritanceLeaf1", null] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task String_include_multiple_derived_navigation_with_same_name_and_different_type(bool async)
    {
        await base.String_include_multiple_derived_navigation_with_same_name_and_different_type(async);

        AssertMql(
            """
InheritanceOne.{ "$lookup" : { "from" : "InheritanceLeafTwo", "localField" : "_id", "foreignField" : "DifferentTypeReference_InheritanceDerived2Id", "as" : "_lookup_InheritanceLeaf2" } }, { "$unwind" : { "path" : "$_lookup_InheritanceLeaf2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_InheritanceLeaf2" : { "$ifNull" : ["$_lookup_InheritanceLeaf2", null] } } }, { "$lookup" : { "from" : "InheritanceLeafOne", "localField" : "_id", "foreignField" : "DifferentTypeReference_InheritanceDerived1Id", "as" : "_lookup_InheritanceLeaf1" } }, { "$unwind" : { "path" : "$_lookup_InheritanceLeaf1", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_InheritanceLeaf1" : { "$ifNull" : ["$_lookup_InheritanceLeaf1", null] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task String_include_multiple_derived_navigation_with_same_name_and_different_type_nested_also_includes_partially_matching_navigation_chains(bool async)
    {
        await base.String_include_multiple_derived_navigation_with_same_name_and_different_type_nested_also_includes_partially_matching_navigation_chains(async);

        AssertMql(
            """
InheritanceOne.{ "$lookup" : { "from" : "InheritanceLeafTwo", "localField" : "_id", "foreignField" : "DifferentTypeReference_InheritanceDerived2Id", "as" : "_lookup_InheritanceLeaf2" } }, { "$unwind" : { "path" : "$_lookup_InheritanceLeaf2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_InheritanceLeaf2" : { "$ifNull" : ["$_lookup_InheritanceLeaf2", null] } } }, { "$lookup" : { "from" : "InheritanceLeafOne", "localField" : "_id", "foreignField" : "DifferentTypeReference_InheritanceDerived1Id", "as" : "_lookup_InheritanceLeaf1" } }, { "$unwind" : { "path" : "$_lookup_InheritanceLeaf1", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_InheritanceLeaf1" : { "$ifNull" : ["$_lookup_InheritanceLeaf1", null] } } }, { "$lookup" : { "from" : "InheritanceTwo", "localField" : "_lookup_InheritanceLeaf2._id", "foreignField" : "InheritanceLeaf2Id", "as" : "_lookup_InheritanceLeaf2._lookup_BaseCollection" } }
""");
    }
#endif

    public override async Task String_include_multiple_derived_collection_navigation_with_same_name_and_same_type(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.String_include_multiple_derived_collection_navigation_with_same_name_and_same_type(async), "Failure");
    }

    public override async Task String_include_multiple_derived_collection_navigation_with_same_name_and_different_type(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.String_include_multiple_derived_collection_navigation_with_same_name_and_different_type(async), "Failure");
    }

    public override async Task String_include_multiple_derived_collection_navigation_with_same_name_and_different_type_nested_also_includes_partially_matching_navigation_chains(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.String_include_multiple_derived_collection_navigation_with_same_name_and_different_type_nested_also_includes_partially_matching_navigation_chains(async), "Failure");
    }

#if (EF10 || EF11)
    public override async Task String_include_multiple_derived_navigations_complex(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.String_include_multiple_derived_navigations_complex(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Nav_rewrite_doesnt_apply_null_protection_for_function_arguments(bool async)
    {
        await base.Nav_rewrite_doesnt_apply_null_protection_for_function_arguments(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "OneToOne_Optional_PK_Inverse2Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2" : { "$ne" : null } } }, { "$project" : { "_v" : { "$max" : ["$_lookup_Level2.Level1_Required_Id", 7] }, "_id" : 0 } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Accessing_optional_property_inside_result_operator_subquery(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Accessing_optional_property_inside_result_operator_subquery(async), "Failure");
    }
#endif

    public override async Task SelectMany_subquery_with_custom_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_subquery_with_custom_projection(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Include1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include1(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include2(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include2(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include3(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include3(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include4(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include4(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include5(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include5(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include6(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include6(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include7(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include7(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include8(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include8(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include9(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include9(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include10(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include10(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include11(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include11(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include12(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include12(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include13(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include13(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include14(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Include14(async), "Failure");
    }
#endif

    public override async Task Include17(bool async)
    {
        await base.Include17(async);

        AssertMql();
    }

#if (EF10 || EF11)
    public override async Task Include18_1(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include18_1(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_1_1(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.TranslationFailedAsync(() => base.Include18_1_1(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_2(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.TranslationFailedAsync(() => base.Include18_2(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_3(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.TranslationFailedAsync(() => base.Include18_3(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_3_1(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.TranslationFailedAsync(() => base.Include18_3_1(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_3_2(bool async)
    {
        // Fails: Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported EF-X535
        await SpecFails.TranslationFailedAsync(() => base.Include18_3_2(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_3_3(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include18_3_3(async));
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18_4(bool async)
    {
        // Fails: Missing property values issue EF-164
        await SpecFails.ThrowsAsync(() => base.Include18_4(async), "Document element is missing for required");
    }
#endif

#if (EF10 || EF11)
    public override async Task Include18(bool async)
    {
        await base.Include18(async);

        AssertMql(
            """
LevelOne.{ "$sort" : { "_id" : 1 } }, { "$limit" : 10 }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2_1" } }, { "$unwind" : { "path" : "$_lookup_Level2_1", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2_1" : { "$ifNull" : ["$_lookup_Level2_1", null] } } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "OneToOne_Optional_PK_Inverse2Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }
""");
    }
#endif

    public override async Task Include19(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include19(async), "could not be translated");
    }

    public override async Task Include_with_all_method_include_gets_ignored(bool async)
    {
        await base.Include_with_all_method_include_gets_ignored(async);

        AssertMql(
            """
LevelOne.{ "$match" : { "Name" : "Foo" } }, { "$limit" : 1 }
""");
    }

#if (EF10 || EF11)
    public override async Task Join_with_navigations_in_the_result_selector1(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Join_with_navigations_in_the_result_selector1(async));
    }
#endif

    public override async Task Join_with_navigations_in_the_result_selector2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_with_navigations_in_the_result_selector2(async), "could not be translated");
    }

    public override async Task Member_pushdown_chain_3_levels_deep(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Member_pushdown_chain_3_levels_deep(async), "Unsupported cross-DbSet query");
    }

    public override async Task Member_pushdown_chain_3_levels_deep_entity(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Member_pushdown_chain_3_levels_deep_entity(async), "could not be translated");
    }

    public override async Task Member_pushdown_with_collection_navigation_in_the_middle(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Member_pushdown_with_collection_navigation_in_the_middle(async), "could not be translated");
    }

    public override async Task Member_pushdown_with_multiple_collections(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Member_pushdown_with_multiple_collections(async), "could not be translated");
    }

    public override async Task Include_multiple_collections_on_same_level(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include_multiple_collections_on_same_level(async), "Failure");
    }

#if (EF10 || EF11)
    public override async Task Null_check_removal_applied_recursively(bool async)
    {
        await base.Null_check_removal_applied_recursively(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_Level2._id", "foreignField" : "Level2_Optional_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "LevelFour", "localField" : "_lookup_Level3._id", "foreignField" : "OneToOne_Optional_PK_Inverse4Id", "as" : "_lookup_Level4" } }, { "$unwind" : { "path" : "$_lookup_Level4", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level4.Name" : "L4 01" } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Null_check_different_structure_does_not_remove_null_checks(bool async)
    {
        await base.Null_check_different_structure_does_not_remove_null_checks(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_Level2._id", "foreignField" : "Level2_Optional_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level3" : { "$ifNull" : ["$_lookup_Level3", null] } } }, { "$lookup" : { "from" : "LevelFour", "localField" : "_lookup_Level3._id", "foreignField" : "OneToOne_Optional_PK_Inverse4Id", "as" : "_lookup_Level4" } }, { "$unwind" : { "path" : "$_lookup_Level4", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level4" : { "$ifNull" : ["$_lookup_Level4", null] } } }, { "$match" : { "$expr" : { "$eq" : [{ "$cond" : { "if" : { "$eq" : ["$_lookup_Level2", null] }, "then" : null, "else" : { "$cond" : { "if" : { "$eq" : ["$_lookup_Level3", null] }, "then" : null, "else" : "$_lookup_Level4.Name" } } } }, "L4 01"] } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Union_over_entities_with_different_nullability(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.Union_over_entities_with_different_nullability(async), "Expression not supported");
    }
#endif

    public override async Task Including_reference_navigation_and_projecting_collection_navigation_2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Including_reference_navigation_and_projecting_collection_navigation_2(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task OrderBy_collection_count_ThenBy_reference_navigation(bool async)
    {
        // Fails: Join key selector that is a sub-query: serializer cannot be created for MongoQuery EF-X534
        await SpecFails.ThrowsAsync(() => base.OrderBy_collection_count_ThenBy_reference_navigation(async), "BsonSerializationException");
    }
#endif

#if (EF10 || EF11)
    public override async Task Null_conditional_is_not_applied_explicitly_for_optional_navigation(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Null_conditional_is_not_applied_explicitly_for_optional_navigation(async), "Failure");
    }
#endif

    public override async Task Sum_with_selector_cast_using_as(bool async)
    {
        await base.Sum_with_selector_cast_using_as(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_v" : "$_id", "_id" : 0 } }, { "$group" : { "_id" : null, "_v" : { "$sum" : "$_v" } } }, { "$project" : { "_id" : 0 } }
""");
    }

    public override async Task Sum_with_filter_with_include_selector_cast_using_as(bool async)
    {
        // Fails: Cross-document navigation access issue EF-216
        await SpecFails.ThrowsAsync(() => base.Sum_with_filter_with_include_selector_cast_using_as(async), "Unsupported cross-DbSet query");
    }

#if (EF10 || EF11)
    public override async Task Select_with_joined_where_clause_cast_using_as(bool async)
    {
        await base.Select_with_joined_where_clause_cast_using_as(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$match" : { "$expr" : { "$eq" : [{ "$add" : ["$_outer._id", 7] }, "$_inner._id"] } } }
""");
    }
#endif

    public override async Task SelectMany_with_outside_reference_to_joined_table_correctly_translated_to_apply(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_with_outside_reference_to_joined_table_correctly_translated_to_apply(async), "could not be translated");
    }

    public override async Task Nested_SelectMany_correlated_with_join_table_correctly_translated_to_apply(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Nested_SelectMany_correlated_with_join_table_correctly_translated_to_apply(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Contains_over_optional_navigation_with_null_constant(bool async)
    {
        await base.Contains_over_optional_navigation_with_null_constant(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$match" : { "_lookup_Level2" : null } }, { "$limit" : 1 }, { "$project" : { "_id" : 0, "_v" : null } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Contains_over_optional_navigation_with_null_parameter(bool async)
    {
        await base.Contains_over_optional_navigation_with_null_parameter(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$match" : { "_lookup_Level2" : null } }, { "$limit" : 1 }, { "$project" : { "_id" : 0, "_v" : null } }
""");
    }
#endif

    public override async Task Contains_over_optional_navigation_with_null_column(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Contains_over_optional_navigation_with_null_column(async), "could not be translated");
    }

    public override async Task Contains_over_optional_navigation_with_null_entity_reference(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Contains_over_optional_navigation_with_null_entity_reference(async), "could not be translated");
    }

    public override async Task Element_selector_with_coalesce_repeated_in_aggregate(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Element_selector_with_coalesce_repeated_in_aggregate(async), "could not be translated");
    }

    public override async Task Nested_object_constructed_from_group_key_properties(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Nested_object_constructed_from_group_key_properties(async), "could not be translated");
    }

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task GroupBy_aggregate_where_required_relationship(bool async)
    {
        await base.GroupBy_aggregate_where_required_relationship(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "OneToMany_Required_Inverse2Id", "foreignField" : "_id", "as" : "_lookup_OneToMany_Required_Inverse2" } }, { "$match" : { "_lookup_OneToMany_Required_Inverse2.0" : { "$exists" : true } } }, { "$set" : { "_lookup_OneToMany_Required_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToMany_Required_Inverse2", 0] } } }, { "$group" : { "_id" : "$_lookup_OneToMany_Required_Inverse2._id", "Max" : { "$max" : "$_id" } } }, { "$project" : { "Key" : "$_id", "Max" : "$Max", "_id" : 0 } }, { "$match" : { "$expr" : { "$ne" : ["$Max", 2] } } }
""");
    }
#else
    public override async Task GroupBy_aggregate_where_required_relationship(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_aggregate_where_required_relationship(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task GroupBy_aggregate_where_required_relationship_2(bool async)
    {
        await base.GroupBy_aggregate_where_required_relationship_2(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "OneToMany_Required_Inverse2Id", "foreignField" : "_id", "as" : "_lookup_OneToMany_Required_Inverse2" } }, { "$match" : { "_lookup_OneToMany_Required_Inverse2.0" : { "$exists" : true } } }, { "$set" : { "_lookup_OneToMany_Required_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToMany_Required_Inverse2", 0] } } }, { "$group" : { "_id" : "$_lookup_OneToMany_Required_Inverse2._id", "Max" : { "$max" : "$_id" } } }, { "$project" : { "Key" : "$_id", "Max" : "$Max", "_id" : 0 } }, { "$match" : { "$or" : [{ "$expr" : { "$lt" : ["$Max", 2] } }, { "$expr" : { "$gt" : ["$Max", 2] } }] } }
""");
    }
#else
    public override async Task GroupBy_aggregate_where_required_relationship_2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupBy_aggregate_where_required_relationship_2(async), "could not be translated");
    }
#endif

#if (EF10 || EF11)
    public override async Task Member_over_null_check_ternary_and_nested_dto_type(bool async)
    {
        await base.Member_over_null_check_ternary_and_nested_dto_type(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$sort" : { "_v._inner.Name" : 1, "_v._outer._id" : 1 } }, { "$project" : { "_id" : "$_v._outer._id", "Name" : "$_v._outer.Name", "Level2" : { "$cond" : { "if" : { "$eq" : ["$_v._inner", null] }, "then" : null, "else" : { "_id" : "$_v._inner._id", "Name" : "$_v._inner.Name" } } } } }
""");
    }
#endif

#if (EF10 || EF11)
    public override async Task Member_over_null_check_ternary_and_nested_anonymous_type(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.Member_over_null_check_ternary_and_nested_anonymous_type(async), "Failure");
    }
#endif

    public override async Task Let_let_contains_from_outer_let(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Let_let_contains_from_outer_let(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task Multiple_conditionals_in_projection(bool async)
    {
        await base.Multiple_conditionals_in_projection(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] } } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_id", "foreignField" : "Level2_Optional_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level3" : { "$ifNull" : ["$_lookup_Level3", null] } } }, { "$project" : { "_id" : "$_id", "Name" : "$_lookup_Level3.Name", "Level2" : { "$cond" : { "if" : { "$eq" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] }, "then" : null, "else" : { "_id" : 0, "Name" : null } } } } }
""");
    }
#endif

    public override async Task Composite_key_join_on_groupby_aggregate_projecting_only_grouping_key(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Composite_key_join_on_groupby_aggregate_projecting_only_grouping_key(async), "could not be translated");
    }

#if EF9 || (EF10 || EF11)
    public override async Task Composite_key_join_on_groupby_aggregate_projecting_only_grouping_key2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Composite_key_join_on_groupby_aggregate_projecting_only_grouping_key2(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Multiple_joins_groupby_predicate(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Multiple_joins_groupby_predicate(async));
    }
#else
    public override async Task Multiple_joins_groupby_predicate(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_joins_groupby_predicate(async), "could not be translated");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task Collection_FirstOrDefault_property_accesses_in_projection(bool async)
    {
        await base.Collection_FirstOrDefault_property_accesses_in_projection(async);

        AssertMql(
            """
LevelOne.{ "$match" : { "_id" : { "$lt" : 3 } } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "OneToMany_Optional_Inverse2Id", "pipeline" : [{ "$match" : { "Name" : "L2 02" } }, { "$limit" : 1 }], "as" : "_lookup_OneToMany_Optional1" } }, { "$unwind" : { "path" : "$_lookup_OneToMany_Optional1", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "Id" : "$_id", "Pushdown" : "$_lookup_OneToMany_Optional1.Name", "_id" : 0 } }
""");
    }
#else
    public override async Task Collection_FirstOrDefault_property_accesses_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Collection_FirstOrDefault_property_accesses_in_projection(async), "could not be translated");
    }
#endif

    public override async Task Collection_FirstOrDefault_entity_reference_accesses_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Collection_FirstOrDefault_entity_reference_accesses_in_projection(async), "could not be translated");
    }

    public override async Task Collection_FirstOrDefault_entity_collection_accesses_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Collection_FirstOrDefault_entity_collection_accesses_in_projection(async), "could not be translated");
    }

    public override async Task Multiple_collection_FirstOrDefault_followed_by_member_access_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_collection_FirstOrDefault_followed_by_member_access_in_projection(async), "could not be translated");
    }

    public override async Task Projecting_columns_with_same_name_from_different_entities_making_sure_aliasing_works_after_Distinct(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Projecting_columns_with_same_name_from_different_entities_making_sure_aliasing_works_after_Distinct(async), "could not be translated");
    }

    public override async Task Complex_query_with_let_collection_SelectMany(bool async)
    {
        await base.Complex_query_with_let_collection_SelectMany(async);

        AssertMql();
    }

    public override async Task SelectMany_without_collection_selector_returning_queryable(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.SelectMany_without_collection_selector_returning_queryable(async), "could not be translated");
    }

    public override async Task Select_projecting_queryable_followed_by_SelectMany(bool async)
    {
        await base.Select_projecting_queryable_followed_by_SelectMany(async);

        AssertMql();
    }

    public override async Task Join_with_result_selector_returning_queryable_throws_validation_error(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Join_with_result_selector_returning_queryable_throws_validation_error(async), "could not be translated");
    }

    public override async Task Select_projecting_queryable_followed_by_Join(bool async)
    {
        await base.Select_projecting_queryable_followed_by_Join(async);

        AssertMql();
    }

    public override async Task Select_projecting_queryable_in_anonymous_projection_followed_by_Join(bool async)
    {
        await base.Select_projecting_queryable_in_anonymous_projection_followed_by_Join(async);

        AssertMql();
    }

    public override async Task Project_shadow_properties1(bool async)
    {
        await base.Project_shadow_properties1(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "Id" : "$_id", "OneToOne_Optional_Self1Id" : "$OneToOne_Optional_Self1Id", "OneToMany_Required_Self_Inverse1Id" : "$OneToMany_Required_Self_Inverse1Id", "OneToMany_Optional_Self_Inverse1Id" : "$OneToMany_Optional_Self_Inverse1Id", "_id" : 0 } }
""");
    }

    public override async Task Project_shadow_properties2(bool async)
    {
        await base.Project_shadow_properties2(async);

        AssertMql(
            """
LevelTwo.{ "$project" : { "Id" : "$_id", "OneToOne_Optional_PK_Inverse2Id" : "$OneToOne_Optional_PK_Inverse2Id", "OneToMany_Required_Inverse2Id" : "$OneToMany_Required_Inverse2Id", "OneToMany_Optional_Inverse2Id" : "$OneToMany_Optional_Inverse2Id", "OneToOne_Optional_Self2Id" : "$OneToOne_Optional_Self2Id", "OneToMany_Required_Self_Inverse2Id" : "$OneToMany_Required_Self_Inverse2Id", "OneToMany_Optional_Self_Inverse2Id" : "$OneToMany_Optional_Self_Inverse2Id", "_id" : 0 } }
""");
    }

    public override async Task Project_shadow_properties3(bool async)
    {
        await base.Project_shadow_properties3(async);

        AssertMql(
            """
LevelThree.{ "$project" : { "Id" : "$_id", "OneToOne_Optional_PK_Inverse3Id" : "$OneToOne_Optional_PK_Inverse3Id", "OneToMany_Required_Inverse3Id" : "$OneToMany_Required_Inverse3Id", "OneToMany_Optional_Inverse3Id" : "$OneToMany_Optional_Inverse3Id", "OneToOne_Optional_Self3Id" : "$OneToOne_Optional_Self3Id", "OneToMany_Required_Self_Inverse3Id" : "$OneToMany_Required_Self_Inverse3Id", "OneToMany_Optional_Self_Inverse3Id" : "$OneToMany_Optional_Self_Inverse3Id", "_id" : 0 } }
""");
    }

    public override async Task Project_shadow_properties4(bool async)
    {
        await base.Project_shadow_properties4(async);

        AssertMql(
            """
LevelFour.{ "$project" : { "Id" : "$_id", "OneToOne_Optional_PK_Inverse4Id" : "$OneToOne_Optional_PK_Inverse4Id", "OneToMany_Required_Inverse4Id" : "$OneToMany_Required_Inverse4Id", "OneToMany_Optional_Inverse4Id" : "$OneToMany_Optional_Inverse4Id", "OneToOne_Optional_Self4Id" : "$OneToOne_Optional_Self4Id", "OneToMany_Required_Self_Inverse4Id" : "$OneToMany_Required_Self_Inverse4Id", "OneToMany_Optional_Self_Inverse4Id" : "$OneToMany_Optional_Self_Inverse4Id", "_id" : 0 } }
""");
    }

    public override async Task Project_shadow_properties5(bool async)
    {
        await base.Project_shadow_properties5(async);

        AssertMql(
            """
InheritanceOne.{ "$project" : { "Id" : "$_id", "InheritanceBase2Id" : "$InheritanceBase2Id", "InheritanceBase2Id1" : "$InheritanceBase2Id1", "_id" : 0 } }
""");
    }

    public override async Task Project_shadow_properties6(bool async)
    {
        await base.Project_shadow_properties6(async);

        AssertMql(
            """
InheritanceOne.{ "$match" : { "_t" : "InheritanceDerived1" } }, { "$project" : { "Id" : "$_id", "InheritanceBase2Id" : "$InheritanceBase2Id", "InheritanceBase2Id1" : "$InheritanceBase2Id1", "_id" : 0 } }
""");
    }

    public override async Task Project_shadow_properties7(bool async)
    {
        await base.Project_shadow_properties7(async);

        AssertMql(
            """
InheritanceOne.{ "$match" : { "_t" : "InheritanceDerived2" } }, { "$project" : { "Id" : "$_id", "InheritanceBase2Id" : "$InheritanceBase2Id", "InheritanceBase2Id1" : "$InheritanceBase2Id1", "_id" : 0 } }
""");
    }

    public override async Task Project_shadow_properties8(bool async)
    {
        await base.Project_shadow_properties8(async);

        AssertMql(
            """
InheritanceTwo.{ "$project" : { "Id" : "$_id", "InheritanceLeaf2Id" : "$InheritanceLeaf2Id", "_id" : 0 } }
""");
    }

    public override async Task Project_shadow_properties9(bool async)
    {
        await base.Project_shadow_properties9(async);

        AssertMql(
            """
InheritanceLeafOne.{ "$project" : { "Id" : "$_id", "DifferentTypeReference_InheritanceDerived1Id" : "$DifferentTypeReference_InheritanceDerived1Id", "InheritanceDerived1Id" : "$InheritanceDerived1Id", "InheritanceDerived1Id1" : "$InheritanceDerived1Id1", "InheritanceDerived2Id" : "$InheritanceDerived2Id", "SameTypeReference_InheritanceDerived1Id" : "$SameTypeReference_InheritanceDerived1Id", "SameTypeReference_InheritanceDerived2Id" : "$SameTypeReference_InheritanceDerived2Id", "_id" : 0 } }
""");
    }

    public override async Task Project_shadow_properties10(bool async)
    {
        await base.Project_shadow_properties10(async);

        AssertMql(
            """
InheritanceLeafTwo.{ "$project" : { "Id" : "$_id", "DifferentTypeReference_InheritanceDerived2Id" : "$DifferentTypeReference_InheritanceDerived2Id", "InheritanceDerived2Id" : "$InheritanceDerived2Id", "_id" : 0 } }
""");
    }

    public override async Task Prune_does_not_throw_null_ref(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Prune_does_not_throw_null_ref(async), "could not be translated");
    }

#if (EF10 || EF11)
    public override async Task GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure(bool async)
    {
        await base.GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "pipeline" : [{ "$match" : { "_id" : { "$ne" : 10 } } }], "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "Id1" : "$_id", "Id2" : "$_lookup_Level2._id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task GroupJoin_SelectMany_with_predicate_using_closure(bool async)
    {
        await base.GroupJoin_SelectMany_with_predicate_using_closure(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "pipeline" : [{ "$match" : { "_id" : { "$ne" : 10 } } }], "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : false } }, { "$project" : { "Id1" : "$_id", "Id2" : "$_lookup_Level2._id", "_id" : 0 } }
""");
    }
#else
    public override async Task GroupJoin_SelectMany_with_predicate_using_closure(bool async)
    {
        // Fails: Expression not supported by the driver LINQ translator EF-X527
        await SpecFails.ThrowsAsync(() => base.GroupJoin_SelectMany_with_predicate_using_closure(async), "Expression not supported");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested(async), "Failure");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task GroupJoin_SelectMany_with_predicate_using_closure_nested(bool async)
    {
        await base.GroupJoin_SelectMany_with_predicate_using_closure_nested(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "pipeline" : [{ "$match" : { "_id" : { "$ne" : 10 } } }], "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : false } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_Level2._id", "foreignField" : "Level2_Optional_Id", "pipeline" : [{ "$match" : { "_id" : { "$ne" : 20 } } }], "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : false } }, { "$project" : { "Id1" : "$_id", "Id2" : "$_lookup_Level2._id", "Id3" : "$_lookup_Level3._id", "_id" : 0 } }
""");
    }
#else
    public override async Task GroupJoin_SelectMany_with_predicate_using_closure_nested(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.GroupJoin_SelectMany_with_predicate_using_closure_nested(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested_same_param(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested_same_param(async), "Failure");
    }
#endif

#if EF8 || EF9 || (EF10 || EF11)
    public override async Task GroupJoin_SelectMany_with_predicate_using_closure_nested_same_param(bool async)
    {
        await base.GroupJoin_SelectMany_with_predicate_using_closure_nested_same_param(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "pipeline" : [{ "$match" : { "_id" : { "$ne" : 10 } } }], "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : false } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_Level2._id", "foreignField" : "Level2_Optional_Id", "pipeline" : [{ "$match" : { "_id" : { "$ne" : 10 } } }], "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : false } }, { "$project" : { "Id1" : "$_id", "Id2" : "$_lookup_Level2._id", "Id3" : "$_lookup_Level3._id", "_id" : 0 } }
""");
    }
#else
    public override async Task GroupJoin_SelectMany_with_predicate_using_closure_nested_same_param(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530
        await SpecFails.ThrowsAsync(() => base.GroupJoin_SelectMany_with_predicate_using_closure_nested_same_param(async), "Failure");
    }
#endif

#if (EF10 || EF11)
    public override async Task Multiple_optional_navs_should_not_deadlock(bool async)
    {
        await base.Multiple_optional_navs_should_not_deadlock(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "OneToMany_Optional_Inverse2Id", "foreignField" : "_id", "as" : "_lookup_OneToMany_Optional_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToMany_Optional_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToMany_Optional_Inverse2" : { "$ifNull" : ["$_lookup_OneToMany_Optional_Inverse2", null] } } }, { "$match" : { "$or" : [{ "_lookup_OneToMany_Optional_Inverse2" : { "$ne" : null }, "_lookup_OneToMany_Optional_Inverse2.Name" : { "$regularExpression" : { "pattern" : "L1\\ 01", "options" : "s" } } }, { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ne" : null }, "_lookup_OneToOne_Optional_FK_Inverse2.Name" : { "$regularExpression" : { "pattern" : "L1\\ 01", "options" : "s" } } }] } }, { "$count" : "_v" }
""");
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Null_check_removal_applied_recursively_complex(bool async)
    {
        // Fails: Wrong results compared with the reference (client) evaluation EF-X530 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Null_check_removal_applied_recursively_complex(async), "Failure");
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Correlated_projection_with_first(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Correlated_projection_with_first(async), "could not be translated");
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Max_in_multi_level_nested_subquery(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Max_in_multi_level_nested_subquery(async), "could not be translated");
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Multiple_select_many_in_projection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Multiple_select_many_in_projection(async), "could not be translated");
    }
#endif

#if EF9 || (EF10 || EF11)
    public override async Task Single_select_many_in_projection_with_take(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Single_select_many_in_projection_with_take(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Entity_equality_empty(bool async)
    {
        await base.Entity_equality_empty(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$match" : { "_inner._id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_when_sentinel_ef_property(bool async)
    {
        await base.Key_equality_when_sentinel_ef_property(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2._id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_using_property_method_required(bool async)
    {
        await base.Key_equality_using_property_method_required(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Required_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2._id" : { "$gt" : 7 } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_using_property_method_nested(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_nested(async));
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_using_property_method_and_member_expression1(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_and_member_expression1(async));
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_using_property_method_and_member_expression2(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_using_property_method_and_member_expression2(async));
    }
#endif

#if EF8 || EF9
    public override async Task Key_equality_two_conditions_on_same_navigation(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Key_equality_two_conditions_on_same_navigation(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_key_access_optional(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Join_navigation_key_access_optional(async));
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_key_access_optional_comparison(bool async)
    {
        await base.Navigation_key_access_optional_comparison(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "OneToOne_Optional_PK_Inverse2Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_PK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Optional_PK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_PK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Optional_PK_Inverse2._id" : { "$gt" : 5 } } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Simple_level1_include(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1_include(async));
    }
#endif

#if EF8 || EF9
    public override async Task Simple_level1_level2_include(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1_level2_include(async));
    }
#endif

#if EF8 || EF9
    public override async Task Simple_level1_level2_level3_include(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Simple_level1_level2_level3_include(async));
    }
#endif

#if EF8 || EF9
    public override async Task Navigation_inside_method_call_translated_to_join(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Navigation_inside_method_call_translated_to_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_inside_method_call_translated_to_join(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_method_call_translated_to_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_inside_property_method_translated_to_join(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_property_method_translated_to_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_inside_nested_method_call_translated_to_join(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_nested_method_call_translated_to_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Method_call_on_optional_navigation_translates_to_null_conditional_properly_for_arguments(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Method_call_on_optional_navigation_translates_to_null_conditional_properly_for_arguments(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_inside_method_call_translated_to_join_keeps_original_nullability(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_method_call_translated_to_join_keeps_original_nullability(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability_also_for_arguments(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Optional_navigation_inside_nested_method_call_translated_to_join_keeps_original_nullability_also_for_arguments(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_in_outer_selector_translated_to_extra_join(bool async)
    {
        await base.Join_navigation_in_outer_selector_translated_to_extra_join(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_lookup_Level2._id", "foreignField" : "_id", "as" : "_lookup_Level2_1" } }, { "$match" : { "_lookup_Level2_1.0" : { "$exists" : true } } }, { "$set" : { "_lookup_Level2_1" : { "$arrayElemAt" : ["$_lookup_Level2_1", 0] } } }, { "$project" : { "Id1" : "$_id", "Id2" : "$_lookup_Level2_1._id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_in_outer_selector_translated_to_extra_join_nested2(bool async)
    {
        await base.Join_navigation_in_outer_selector_translated_to_extra_join_nested2(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse3" } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse3.0" : { "$exists" : true } } }, { "$set" : { "_lookup_OneToOne_Required_FK_Inverse3" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_FK_Inverse3", 0] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Required_FK_Inverse3.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_FK_Inverse2", 0] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Optional_FK_Inverse2._id", "foreignField" : "_id", "as" : "_lookup_Level1" } }, { "$match" : { "_lookup_Level1.0" : { "$exists" : true } } }, { "$set" : { "_lookup_Level1" : { "$arrayElemAt" : ["$_lookup_Level1", 0] } } }, { "$project" : { "Id3" : "$_id", "Id1" : "$_lookup_Level1._id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_in_inner_selector(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Join_navigation_in_inner_selector(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigations_in_inner_selector_translated_without_collision(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Join_navigations_in_inner_selector_translated_without_collision(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_non_key_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Join_navigation_non_key_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_with_orderby_on_inner_sequence_navigation_non_key_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Join_with_orderby_on_inner_sequence_navigation_non_key_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_self_ref(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Join_navigation_self_ref(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_nested(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Join_navigation_nested(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_nested2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Join_navigation_nested2(async));
    }
#endif

#if EF8 || EF9
    public override async Task Join_navigation_deeply_nested_non_key_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Join_navigation_deeply_nested_non_key_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include_reference_and_project_into_anonymous_type(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include_reference_and_project_into_anonymous_type(async));
    }
#endif

#if EF8 || EF9
    public override async Task Select_nav_prop_reference_optional1(bool async)
    {
        await base.Select_nav_prop_reference_optional1(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level2.Name", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Select_nav_prop_reference_optional1_via_DefaultIfEmpty(bool async)
    {
        await base.Select_nav_prop_reference_optional1_via_DefaultIfEmpty(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level2.Name", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Select_nav_prop_reference_optional2(bool async)
    {
        await base.Select_nav_prop_reference_optional2(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level2._id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Select_nav_prop_reference_optional2_via_DefaultIfEmpty(bool async)
    {
        await base.Select_nav_prop_reference_optional2_via_DefaultIfEmpty(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level2._id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Select_nav_prop_reference_optional3(bool async)
    {
        await base.Select_nav_prop_reference_optional3(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_FK_Inverse2", 0] } } }, { "$project" : { "_v" : "$_lookup_OneToOne_Optional_FK_Inverse2.Name", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Where_nav_prop_reference_optional1(bool async)
    {
        await base.Where_nav_prop_reference_optional1(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "$or" : [{ "_lookup_Level2.Name" : "L2 05" }, { "_lookup_Level2.Name" : "L2 07" }] } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Where_nav_prop_reference_optional2(bool async)
    {
        await base.Where_nav_prop_reference_optional2(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "$or" : [{ "_lookup_Level2.Name" : "L2 05" }, { "_lookup_Level2.Name" : { "$ne" : "L2 42" } }] } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Select_multiple_nav_prop_reference_optional(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Select_multiple_nav_prop_reference_optional(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_member_compared_to_value(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Where_multiple_nav_prop_reference_optional_member_compared_to_value(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_member_compared_to_null(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Where_multiple_nav_prop_reference_optional_member_compared_to_null(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null1(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Where_multiple_nav_prop_reference_optional_compared_to_null1(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null2(bool async)
    {
        await base.Where_multiple_nav_prop_reference_optional_compared_to_null2(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse3" } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse3" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_FK_Inverse3", 0] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Optional_FK_Inverse3.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_FK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Optional_FK_Inverse2" : null } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null3(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Where_multiple_nav_prop_reference_optional_compared_to_null3(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null4(bool async)
    {
        await base.Where_multiple_nav_prop_reference_optional_compared_to_null4(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse3" } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse3" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_FK_Inverse3", 0] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Optional_FK_Inverse3.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Optional_FK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ne" : null } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_reference_optional_compared_to_null5(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Where_multiple_nav_prop_reference_optional_compared_to_null5(async));
    }
#endif

#if EF8 || EF9
    public override async Task Select_multiple_nav_prop_reference_required(bool async)
    {
        await base.Select_multiple_nav_prop_reference_required(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Required_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_Level2._id", "foreignField" : "Level2_Required_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level3._id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Select_multiple_nav_prop_optional_required(bool async)
    {
        await base.Select_multiple_nav_prop_optional_required(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_Level2._id", "foreignField" : "Level2_Required_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level3._id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Where_multiple_nav_prop_optional_required(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Where_multiple_nav_prop_optional_required(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse2(bool async)
    {
        await base.Where_complex_predicate_with_with_nav_prop_and_OrElse2(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_Level2._id", "foreignField" : "Level2_Required_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level3" : { "$ifNull" : ["$_lookup_Level3", null] } } }, { "$match" : { "$or" : [{ "_lookup_Level3.Name" : "L3 05" }, { "_lookup_Level2.Name" : { "$ne" : "L2 05" } }] } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse3(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Where_complex_predicate_with_with_nav_prop_and_OrElse3(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_complex_predicate_with_with_nav_prop_and_OrElse4(bool async)
    {
        await base.Where_complex_predicate_with_with_nav_prop_and_OrElse4(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse3" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Required_FK_Inverse3", "preserveNullAndEmptyArrays" : false } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse3" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse3", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse3" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse3", null] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Required_FK_Inverse3.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] } } }, { "$match" : { "$or" : [{ "_lookup_OneToOne_Optional_FK_Inverse3.Name" : { "$ne" : "L2 05" } }, { "_lookup_OneToOne_Optional_FK_Inverse2.Name" : "L1 05" }] } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Complex_navigations_with_predicate_projected_into_anonymous_type2(bool async)
    {
        await base.Complex_navigations_with_predicate_projected_into_anonymous_type2(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "Level2_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse3" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Required_FK_Inverse3", "preserveNullAndEmptyArrays" : false } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Required_FK_Inverse3.Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Required_FK_Inverse2", "preserveNullAndEmptyArrays" : false } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToOne_Required_FK_Inverse3.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] } } }, { "$match" : { "$and" : [{ "$expr" : { "$ne" : ["$_lookup_OneToOne_Required_FK_Inverse2", "$_lookup_OneToOne_Optional_FK_Inverse2"] } }, { "_lookup_OneToOne_Optional_FK_Inverse2._id" : { "$ne" : 7 } }] } }, { "$project" : { "Name" : "$Name", "_id" : "$_lookup_OneToOne_Optional_FK_Inverse2._id" } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_projected_into_DTO(bool async)
    {
        await base.Optional_navigation_projected_into_DTO(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$project" : { "_id" : "$_v._outer._id", "Name" : "$_v._outer.Name", "Inner" : { "$cond" : { "if" : { "$ne" : ["$_v._inner", null] }, "then" : { "_id" : "$_v._inner._id", "Name" : "$_v._inner.Name" }, "else" : null } } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task OrderBy_nav_prop_reference_optional(bool async)
    {
        await base.OrderBy_nav_prop_reference_optional(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "__sort0" : "$_id" } }, { "$sort" : { "_lookup_Level2.Name" : 1, "__sort0" : 1 } }, { "$unset" : ["__sort0"] }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task OrderBy_nav_prop_reference_optional_via_DefaultIfEmpty(bool async)
    {
        await base.OrderBy_nav_prop_reference_optional_via_DefaultIfEmpty(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "__sort0" : "$_id" } }, { "$sort" : { "_lookup_Level2.Name" : 1, "__sort0" : 1 } }, { "$unset" : ["__sort0"] }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_Sum(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Sum(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$group" : { "_id" : null, "_v" : { "$sum" : "$_v._inner.Level1_Required_Id" } } }, { "$project" : { "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_Min(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Result_operator_nav_prop_reference_optional_Min(async));
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_Max(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Max(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$group" : { "_id" : null, "_max" : { "$max" : { "_v" : "$_v._inner.Level1_Required_Id" } } } }, { "$replaceRoot" : { "newRoot" : "$_max" } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_Average(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Average(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$group" : { "_id" : null, "_v" : { "$avg" : "$_v._inner.Level1_Required_Id" } } }, { "$project" : { "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_Average_with_identity_selector(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Average_with_identity_selector(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$group" : { "_id" : null, "_v" : { "$avg" : "$_v._inner.Level1_Required_Id" } } }, { "$project" : { "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_Average_without_selector(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_Average_without_selector(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_v" : "$_lookup_Level2.Level1_Required_Id", "_id" : 0 } }, { "$group" : { "_id" : null, "v" : { "$avg" : "$_v" } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Result_operator_nav_prop_reference_optional_via_DefaultIfEmpty(bool async)
    {
        await base.Result_operator_nav_prop_reference_optional_via_DefaultIfEmpty(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$group" : { "_id" : null, "_v" : { "$sum" : { "$cond" : { "if" : { "$eq" : ["$_v._inner", null] }, "then" : 0, "else" : "$_v._inner.Level1_Required_Id" } } } } }, { "$project" : { "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Include_with_optional_navigation(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include_with_optional_navigation(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_navigation_property_to_collection(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Where_navigation_property_to_collection(async));
    }
#endif

#if EF8 || EF9
    public override async Task Correlated_subquery_doesnt_project_unnecessary_columns_in_top_level_join(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Correlated_subquery_doesnt_project_unnecessary_columns_in_top_level_join(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_take_optional_navigation(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Optional_navigation_take_optional_navigation(async));
    }
#endif

#if EF8 || EF9
    public override async Task Where_predicate_on_optional_reference_navigation(bool async)
    {
        await base.Where_predicate_on_optional_reference_navigation(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Required_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2.Name" : "L2 03" } }, { "$set" : { "__sort0" : "$_id" } }, { "$sort" : { "__sort0" : 1 } }, { "$unset" : ["__sort0"] }, { "$limit" : 3 }, { "$project" : { "_v" : "$Name", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Contains_with_subquery_optional_navigation_and_constant_item(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Contains_with_subquery_optional_navigation_and_constant_item(async));
    }
#endif

#if EF8 || EF9
    public override async Task Contains_with_subquery_optional_navigation_scalar_distinct_and_constant_item(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Contains_with_subquery_optional_navigation_scalar_distinct_and_constant_item(async));
    }
#endif

#if EF8 || EF9
    public override async Task Manually_created_left_join_propagates_nullability_to_navigations(bool async)
    {
        await base.Manually_created_left_join_propagates_nullability_to_navigations(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_Level2.Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Required_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_FK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse2.Name" : { "$ne" : "L3 02" } } }, { "$project" : { "_v" : "$_lookup_OneToOne_Required_FK_Inverse2.Name", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_propagates_nullability_to_manually_created_left_join2(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Optional_navigation_propagates_nullability_to_manually_created_left_join2(async));
    }
#endif

#if EF8 || EF9
    public override async Task Null_reference_protection_complex(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Null_reference_protection_complex(async));
    }
#endif

#if EF8 || EF9
    public override async Task Null_reference_protection_complex_materialization(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentNullException>(() => base.Null_reference_protection_complex_materialization(async));
    }
#endif

#if EF8 || EF9
    public override async Task Null_reference_protection_complex_client_eval(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.Null_reference_protection_complex_client_eval(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened2(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened2(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened3(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_with_complex_subquery_with_joins_does_not_get_flattened3(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer_with_client_method(bool async)
    {
        // Fails: an unsupported query now throws a different exception type than the base test expects (EF-X705)
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.XunitException>(() => base.GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_outer_with_client_method(async), "Exception type was not an exact match");
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_inner(bool async)
    {
        await base.GroupJoin_on_a_subquery_containing_another_GroupJoin_projecting_inner(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$sort" : { "_id" : 1 } }, { "$limit" : 2 }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_Level2.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] } } }, { "$project" : { "_v" : "$_lookup_OneToOne_Optional_FK_Inverse2.Name", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_on_a_subquery_containing_another_GroupJoin_with_orderby_on_inner_sequence_projecting_inner(bool async)
    {
        await base.GroupJoin_on_a_subquery_containing_another_GroupJoin_with_orderby_on_inner_sequence_projecting_inner(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$sort" : { "_id" : 1 } }, { "$limit" : 2 }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_Level2.Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] } } }, { "$project" : { "_v" : "$_lookup_OneToOne_Optional_FK_Inverse2.Name", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_on_left_side_being_a_subquery(bool async)
    {
        await base.GroupJoin_on_left_side_being_a_subquery(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "__sort0" : "$_id" } }, { "$sort" : { "_lookup_Level2.Name" : 1, "__sort0" : 1 } }, { "$unset" : ["__sort0"] }, { "$limit" : 2 }, { "$project" : { "Id" : "$_id", "Brand" : "$_lookup_Level2.Name", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_on_right_side_being_a_subquery(bool async)
    {
        // Fails: Argument exception while translating or materializing EF-X603
        await SpecFails.ThrowsAsync<global::System.ArgumentException>(() => base.GroupJoin_on_right_side_being_a_subquery(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_client_method_on_outer(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.GroupJoin_client_method_on_outer(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_client_method_in_OrderBy(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.GroupJoin_client_method_in_OrderBy(async));
    }
#endif

#if EF8 || EF9
    public override async Task Optional_navigation_in_subquery_with_unrelated_projection(bool async)
    {
        await base.Optional_navigation_in_subquery_with_unrelated_projection(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2.Name" : { "$ne" : "Foo" } } }, { "$set" : { "__sort0" : "$_id" } }, { "$sort" : { "__sort0" : 1 } }, { "$unset" : ["__sort0"] }, { "$limit" : 15 }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection(bool async)
    {
        await base.Explicit_GroupJoin_in_subquery_with_unrelated_projection(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2.Name" : { "$ne" : "Foo" } } }, { "$set" : { "__sort0" : "$_id" } }, { "$sort" : { "__sort0" : 1 } }, { "$unset" : ["__sort0"] }, { "$limit" : 15 }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection2(bool async)
    {
        await base.Explicit_GroupJoin_in_subquery_with_unrelated_projection2(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2.Name" : { "$ne" : "Foo" } } }, { "$unset" : ["_lookup_Level2"] }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }, { "$project" : { "_id" : "$_id" } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection3(bool async)
    {
        await base.Explicit_GroupJoin_in_subquery_with_unrelated_projection3(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2.Name" : { "$ne" : "Foo" } } }, { "$group" : { "_id" : { "_v" : "$_id", "_v__isMissing" : { "$eq" : [{ "$type" : "$_id" }, "missing"] } } } }, { "$project" : { "_v" : { "$cond" : { "if" : "$_id._v__isMissing", "then" : "$$REMOVE", "else" : "$_id._v" } }, "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Explicit_GroupJoin_in_subquery_with_unrelated_projection4(bool async)
    {
        await base.Explicit_GroupJoin_in_subquery_with_unrelated_projection4(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$match" : { "_lookup_Level2.Name" : { "$ne" : "Foo" } } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }, { "$group" : { "_id" : "$$ROOT" } }, { "$replaceRoot" : { "newRoot" : "$_id" } }, { "$sort" : { "_v" : 1 } }, { "$limit" : 20 }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Nested_group_join_with_take(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Nested_group_join_with_take(async));
    }
#endif

#if EF8 || EF9
    public override async Task Multi_level_navigation_compared_to_null(bool async)
    {
        await base.Multi_level_navigation_compared_to_null(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "OneToMany_Optional_Inverse3Id", "foreignField" : "_id", "as" : "_lookup_OneToMany_Optional_Inverse3" } }, { "$set" : { "_lookup_OneToMany_Optional_Inverse3" : { "$arrayElemAt" : ["$_lookup_OneToMany_Optional_Inverse3", 0] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToMany_Optional_Inverse3.Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Required_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_FK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse2" : { "$ne" : null } } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Multi_level_navigation_with_same_navigation_compared_to_null(bool async)
    {
        await base.Multi_level_navigation_with_same_navigation_compared_to_null(async);

        AssertMql(
            """
LevelThree.{ "$lookup" : { "from" : "LevelTwo", "localField" : "OneToMany_Optional_Inverse3Id", "foreignField" : "_id", "as" : "_lookup_OneToMany_Optional_Inverse3" } }, { "$set" : { "_lookup_OneToMany_Optional_Inverse3" : { "$arrayElemAt" : ["$_lookup_OneToMany_Optional_Inverse3", 0] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "_lookup_OneToMany_Optional_Inverse3.Level1_Required_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Required_FK_Inverse2" } }, { "$set" : { "_lookup_OneToOne_Required_FK_Inverse2" : { "$arrayElemAt" : ["$_lookup_OneToOne_Required_FK_Inverse2", 0] } } }, { "$match" : { "_lookup_OneToOne_Required_FK_Inverse2.Name" : { "$ne" : "L1 07" }, "_lookup_OneToOne_Required_FK_Inverse2" : { "$ne" : null } } }, { "$project" : { "_v" : "$_id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Navigations_compared_to_each_other2(bool async)
    {
        await base.Navigations_compared_to_each_other2(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "OneToOne_Optional_PK_Inverse2Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_PK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_PK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_PK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_PK_Inverse2", null] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "OneToMany_Required_Inverse2Id", "foreignField" : "_id", "as" : "_lookup_OneToMany_Required_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToMany_Required_Inverse2", "preserveNullAndEmptyArrays" : false } }, { "$match" : { "$expr" : { "$eq" : ["$_lookup_OneToMany_Required_Inverse2", "$_lookup_OneToOne_Optional_PK_Inverse2"] } } }, { "$project" : { "_v" : "$Name", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Navigations_compared_to_each_other4(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Navigations_compared_to_each_other4(async));
    }
#endif

#if EF8 || EF9
    public override async Task Navigations_compared_to_each_other5(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Navigations_compared_to_each_other5(async));
    }
#endif

#if EF8 || EF9
    public override async Task Comparing_collection_navigation_on_optional_reference_to_null(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Comparing_collection_navigation_on_optional_reference_to_null(async));
    }
#endif

#if EF8 || EF9
    public override async Task Project_collection_navigation_count(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.TranslationFailedAsync(() => base.Project_collection_navigation_count(async));
    }
#endif

#if EF8 || EF9
    public override async Task String_include_multiple_derived_navigation_with_same_name_and_same_type(bool async)
    {
        await base.String_include_multiple_derived_navigation_with_same_name_and_same_type(async);

        AssertMql(
            """
InheritanceOne.{ "$lookup" : { "from" : "InheritanceLeafOne", "localField" : "_id", "foreignField" : "SameTypeReference_InheritanceDerived2Id", "as" : "_lookup_InheritanceLeaf1_1" } }, { "$unwind" : { "path" : "$_lookup_InheritanceLeaf1_1", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_InheritanceLeaf1_1" : { "$ifNull" : ["$_lookup_InheritanceLeaf1_1", null] } } }, { "$lookup" : { "from" : "InheritanceLeafOne", "localField" : "_id", "foreignField" : "SameTypeReference_InheritanceDerived1Id", "as" : "_lookup_InheritanceLeaf1" } }, { "$unwind" : { "path" : "$_lookup_InheritanceLeaf1", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_InheritanceLeaf1" : { "$ifNull" : ["$_lookup_InheritanceLeaf1", null] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task String_include_multiple_derived_navigation_with_same_name_and_different_type(bool async)
    {
        await base.String_include_multiple_derived_navigation_with_same_name_and_different_type(async);

        AssertMql(
            """
InheritanceOne.{ "$lookup" : { "from" : "InheritanceLeafTwo", "localField" : "_id", "foreignField" : "DifferentTypeReference_InheritanceDerived2Id", "as" : "_lookup_InheritanceLeaf2" } }, { "$unwind" : { "path" : "$_lookup_InheritanceLeaf2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_InheritanceLeaf2" : { "$ifNull" : ["$_lookup_InheritanceLeaf2", null] } } }, { "$lookup" : { "from" : "InheritanceLeafOne", "localField" : "_id", "foreignField" : "DifferentTypeReference_InheritanceDerived1Id", "as" : "_lookup_InheritanceLeaf1" } }, { "$unwind" : { "path" : "$_lookup_InheritanceLeaf1", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_InheritanceLeaf1" : { "$ifNull" : ["$_lookup_InheritanceLeaf1", null] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task String_include_multiple_derived_navigation_with_same_name_and_different_type_nested_also_includes_partially_matching_navigation_chains(bool async)
    {
        await base.String_include_multiple_derived_navigation_with_same_name_and_different_type_nested_also_includes_partially_matching_navigation_chains(async);

        AssertMql(
            """
InheritanceOne.{ "$lookup" : { "from" : "InheritanceLeafTwo", "localField" : "_id", "foreignField" : "DifferentTypeReference_InheritanceDerived2Id", "as" : "_lookup_InheritanceLeaf2" } }, { "$unwind" : { "path" : "$_lookup_InheritanceLeaf2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_InheritanceLeaf2" : { "$ifNull" : ["$_lookup_InheritanceLeaf2", null] } } }, { "$lookup" : { "from" : "InheritanceLeafOne", "localField" : "_id", "foreignField" : "DifferentTypeReference_InheritanceDerived1Id", "as" : "_lookup_InheritanceLeaf1" } }, { "$unwind" : { "path" : "$_lookup_InheritanceLeaf1", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_InheritanceLeaf1" : { "$ifNull" : ["$_lookup_InheritanceLeaf1", null] } } }, { "$lookup" : { "from" : "InheritanceTwo", "localField" : "_lookup_InheritanceLeaf2._id", "foreignField" : "InheritanceLeaf2Id", "as" : "_lookup_InheritanceLeaf2._lookup_BaseCollection" } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task String_include_multiple_derived_navigations_complex(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.String_include_multiple_derived_navigations_complex(async));
    }
#endif

#if EF8 || EF9
    public override async Task Nav_rewrite_doesnt_apply_null_protection_for_function_arguments(bool async)
    {
        await base.Nav_rewrite_doesnt_apply_null_protection_for_function_arguments(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "OneToOne_Optional_PK_Inverse2Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level2" : { "$ne" : null } } }, { "$project" : { "_v" : { "$max" : ["$_lookup_Level2.Level1_Required_Id", 7] }, "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Accessing_optional_property_inside_result_operator_subquery(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Accessing_optional_property_inside_result_operator_subquery(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include1(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include1(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include2(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include2(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include3(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include3(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include4(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include4(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include5(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include5(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include6(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include6(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include7(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include7(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include8(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include8(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include9(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include9(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include10(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include10(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include11(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include11(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include12(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include12(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include13(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include13(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include14(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include14(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include18_1(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Include18_1(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include18_1_1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include18_1_1(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include18_2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include18_2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include18_3(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include18_3(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include18_3_1(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include18_3_1(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include18_3_2(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Include18_3_2(async), "could not be translated");
    }
#endif

#if EF8 || EF9
    public override async Task Include18_3_3(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Include18_3_3(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include18_4(bool async)
    {
        // Fails: InvalidOperationException from query execution or materialization EF-X604
        await SpecFails.ThrowsAsync<global::System.InvalidOperationException>(() => base.Include18_4(async));
    }
#endif

#if EF8 || EF9
    public override async Task Include18(bool async)
    {
        await base.Include18(async);

        AssertMql(
            """
LevelOne.{ "$sort" : { "_id" : 1 } }, { "$limit" : 10 }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2_1" } }, { "$unwind" : { "path" : "$_lookup_Level2_1", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2_1" : { "$ifNull" : ["$_lookup_Level2_1", null] } } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "OneToOne_Optional_PK_Inverse2Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }
""");
    }
#endif

#if EF8 || EF9
#if EF8
    public override async Task Join_with_navigations_in_the_result_selector1(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Join_with_navigations_in_the_result_selector1(async));
    }
#else
    public override async Task Join_with_navigations_in_the_result_selector1(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Join_with_navigations_in_the_result_selector1(async));
    }
#endif
#endif

#if EF8 || EF9
    public override async Task Null_check_removal_applied_recursively(bool async)
    {
        await base.Null_check_removal_applied_recursively(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_Level2._id", "foreignField" : "Level2_Optional_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "LevelFour", "localField" : "_lookup_Level3._id", "foreignField" : "OneToOne_Optional_PK_Inverse4Id", "as" : "_lookup_Level4" } }, { "$unwind" : { "path" : "$_lookup_Level4", "preserveNullAndEmptyArrays" : true } }, { "$match" : { "_lookup_Level4.Name" : "L4 01" } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Null_check_different_structure_does_not_remove_null_checks(bool async)
    {
        await base.Null_check_different_structure_does_not_remove_null_checks(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_lookup_Level2._id", "foreignField" : "Level2_Optional_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level3" : { "$ifNull" : ["$_lookup_Level3", null] } } }, { "$lookup" : { "from" : "LevelFour", "localField" : "_lookup_Level3._id", "foreignField" : "OneToOne_Optional_PK_Inverse4Id", "as" : "_lookup_Level4" } }, { "$unwind" : { "path" : "$_lookup_Level4", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level4" : { "$ifNull" : ["$_lookup_Level4", null] } } }, { "$match" : { "$expr" : { "$eq" : [{ "$cond" : { "if" : { "$eq" : ["$_lookup_Level2", null] }, "then" : null, "else" : { "$cond" : { "if" : { "$eq" : ["$_lookup_Level3", null] }, "then" : null, "else" : "$_lookup_Level4.Name" } } } }, "L4 01"] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Union_over_entities_with_different_nullability(bool async)
    {
        // Fails: Other unexpected exception from query execution EF-X605
        await SpecFails.ThrowsAsync<global::System.NotSupportedException>(() => base.Union_over_entities_with_different_nullability(async));
    }
#endif

#if EF8 || EF9
#if EF8
    public override async Task OrderBy_collection_count_ThenBy_reference_navigation(bool async)
    {
        // Fails: Other unexpected exception from query execution EF-X605
        await SpecFails.ThrowsAsync<global::MongoDB.Bson.BsonSerializationException>(() => base.OrderBy_collection_count_ThenBy_reference_navigation(async));
    }
#else
    public override async Task OrderBy_collection_count_ThenBy_reference_navigation(bool async)
    {
        // Fails: Other unexpected exception from query execution EF-X605
        await SpecFails.ThrowsAsync<global::MongoDB.Bson.BsonSerializationException>(() => base.OrderBy_collection_count_ThenBy_reference_navigation(async));
    }
#endif
#endif

#if EF8 || EF9
    public override async Task Null_conditional_is_not_applied_explicitly_for_optional_navigation(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600 (depends on the time zone: passes in UTC) EF-X720
        await SpecFails.EitherAsync(() => base.Null_conditional_is_not_applied_explicitly_for_optional_navigation(async));
    }
#endif

#if EF8 || EF9
    public override async Task Select_with_joined_where_clause_cast_using_as(bool async)
    {
        await base.Select_with_joined_where_clause_cast_using_as(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$match" : { "$expr" : { "$eq" : [{ "$add" : ["$_outer._id", 7] }, "$_inner._id"] } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Contains_over_optional_navigation_with_null_constant(bool async)
    {
        await base.Contains_over_optional_navigation_with_null_constant(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$match" : { "_lookup_Level2" : null } }, { "$limit" : 1 }, { "$project" : { "_id" : 0, "_v" : null } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Contains_over_optional_navigation_with_null_parameter(bool async)
    {
        await base.Contains_over_optional_navigation_with_null_parameter(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level2" : { "$ifNull" : ["$_lookup_Level2", null] } } }, { "$match" : { "_lookup_Level2" : null } }, { "$limit" : 1 }, { "$project" : { "_id" : 0, "_v" : null } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Member_over_null_check_ternary_and_nested_dto_type(bool async)
    {
        await base.Member_over_null_check_ternary_and_nested_dto_type(async);

        AssertMql(
            """
LevelOne.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "LevelTwo", "localField" : "_outer._id", "foreignField" : "Level1_Optional_Id", "as" : "_inner" } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }, { "$project" : { "_v" : { "$map" : { "input" : { "$cond" : { "if" : { "$eq" : [{ "$size" : "$_inner" }, 0] }, "then" : [null], "else" : "$_inner" } }, "as" : "i", "in" : { "_outer" : "$_outer", "_inner" : "$$i" } } }, "_id" : 0 } }, { "$unwind" : "$_v" }, { "$sort" : { "_v._inner.Name" : 1, "_v._outer._id" : 1 } }, { "$project" : { "_id" : "$_v._outer._id", "Name" : "$_v._outer.Name", "Level2" : { "$cond" : { "if" : { "$eq" : ["$_v._inner", null] }, "then" : null, "else" : { "_id" : "$_v._inner._id", "Name" : "$_v._inner.Name" } } } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task Member_over_null_check_ternary_and_nested_anonymous_type(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.Member_over_null_check_ternary_and_nested_anonymous_type(async));
    }
#endif

#if EF8 || EF9
    public override async Task Multiple_conditionals_in_projection(bool async)
    {
        await base.Multiple_conditionals_in_projection(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] } } }, { "$lookup" : { "from" : "LevelThree", "localField" : "_id", "foreignField" : "Level2_Optional_Id", "as" : "_lookup_Level3" } }, { "$unwind" : { "path" : "$_lookup_Level3", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Level3" : { "$ifNull" : ["$_lookup_Level3", null] } } }, { "$project" : { "_id" : "$_id", "Name" : "$_lookup_Level3.Name", "Level2" : { "$cond" : { "if" : { "$eq" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] }, "then" : null, "else" : { "_id" : 0, "Name" : null } } } } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure(bool async)
    {
        await base.GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure(async);

        AssertMql(
            """
LevelOne.{ "$lookup" : { "from" : "LevelTwo", "localField" : "_id", "foreignField" : "Level1_Optional_Id", "pipeline" : [{ "$match" : { "_id" : { "$ne" : 10 } } }], "as" : "_lookup_Level2" } }, { "$unwind" : { "path" : "$_lookup_Level2", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "Id1" : "$_id", "Id2" : "$_lookup_Level2._id", "_id" : 0 } }
""");
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested(async));
    }
#endif

#if EF8 || EF9
    public override async Task GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested_same_param(bool async)
    {
        // Fails: Query translates natively but returns different data than the expected result EF-X600
        await SpecFails.ThrowsAsync<global::Xunit.Sdk.EqualException>(() => base.GroupJoin_SelectMany_DefaultIfEmpty_with_predicate_using_closure_nested_same_param(async));
    }
#endif

#if EF9
    public override async Task Multiple_optional_navs_should_not_deadlock(bool async)
    {
        await base.Multiple_optional_navs_should_not_deadlock(async);

        AssertMql(
            """
LevelTwo.{ "$lookup" : { "from" : "LevelOne", "localField" : "Level1_Optional_Id", "foreignField" : "_id", "as" : "_lookup_OneToOne_Optional_FK_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToOne_Optional_FK_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ifNull" : ["$_lookup_OneToOne_Optional_FK_Inverse2", null] } } }, { "$lookup" : { "from" : "LevelOne", "localField" : "OneToMany_Optional_Inverse2Id", "foreignField" : "_id", "as" : "_lookup_OneToMany_Optional_Inverse2" } }, { "$unwind" : { "path" : "$_lookup_OneToMany_Optional_Inverse2", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_OneToMany_Optional_Inverse2" : { "$ifNull" : ["$_lookup_OneToMany_Optional_Inverse2", null] } } }, { "$match" : { "$or" : [{ "_lookup_OneToMany_Optional_Inverse2" : { "$ne" : null }, "_lookup_OneToMany_Optional_Inverse2.Name" : { "$regularExpression" : { "pattern" : "L1\\ 01", "options" : "s" } } }, { "_lookup_OneToOne_Optional_FK_Inverse2" : { "$ne" : null }, "_lookup_OneToOne_Optional_FK_Inverse2.Name" : { "$regularExpression" : { "pattern" : "L1\\ 01", "options" : "s" } } }] } }, { "$count" : "_v" }
""");
    }
#endif

#if EF8
    public override async Task Distinct_skip_without_orderby(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Distinct_skip_without_orderby(async), "could not be translated");
    }
#endif

#if EF8
    public override async Task Distinct_take_without_orderby(bool async)
    {
        // Fails: Query shape (navigation/projection/set operation) not translated EF-X526
        await SpecFails.ThrowsAsync(() => base.Distinct_take_without_orderby(async), "could not be translated");
    }
#endif

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    public class ComplexNavigationsQueryMongoFixture : ComplexNavigationsQueryFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("CNav");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
            MongoSpecModelHelpers.RelaxNullableUniqueIndexes(modelBuilder);
        }

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }
    }
}
