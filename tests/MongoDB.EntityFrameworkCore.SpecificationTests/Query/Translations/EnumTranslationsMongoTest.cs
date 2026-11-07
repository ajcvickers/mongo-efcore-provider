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

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query.Translations;

public class EnumTranslationsMongoTest : EnumTranslationsTestBase<MongoBasicTypesQueryFixture>
{
    public EnumTranslationsMongoTest(MongoBasicTypesQueryFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
    public override async Task Bitwise_and_enum_constant()
    {
        await base.Bitwise_and_enum_constant();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$gt" : [{ "$bitAnd" : ["$FlagsEnum", 1] }, 0] } } }
    """,
                //
                """
    BasicTypesEntities.{ "$match" : { "FlagsEnum" : { "$bitsAllSet" : 1 } } }
    """);
    }

    // Fails: ValueConverterSerializer lacks IHasRepresentationSerializer for queries over converted properties EF-X302
    public override Task Bitwise_and_integral_constant()
        => SpecFails.Async<global::System.NotSupportedException>(() => base.Bitwise_and_integral_constant(), "Serializer class ConvertIntegralTypeToEnumSerializer`3 does not implem");

    public override async Task Bitwise_and_nullable_enum_with_constant()
    {
        await base.Bitwise_and_nullable_enum_with_constant();

        AssertMql(
    """
    NullableBasicTypesEntities.{ "$match" : { "$expr" : { "$gt" : [{ "$bitAnd" : ["$FlagsEnum", 8] }, 0] } } }
    """);
    }

    public override async Task Bitwise_or()
    {
        await base.Bitwise_or();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "$expr" : { "$gt" : [{ "$bitOr" : ["$FlagsEnum", 8] }, 0] } } }
    """);
    }

    public override async Task Bitwise_projects_values_in_select()
    {
        await base.Bitwise_projects_values_in_select();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "FlagsEnum" : { "$bitsAllSet" : 8 } } }, { "$project" : { "BitwiseTrue" : { "$eq" : [{ "$bitAnd" : ["$FlagsEnum", 8] }, 8] }, "BitwiseFalse" : { "$eq" : [{ "$bitAnd" : ["$FlagsEnum", 8] }, 4] }, "BitwiseValue" : { "$bitAnd" : ["$FlagsEnum", 8] }, "_id" : 0 } }, { "$limit" : 1 }
    """);
    }

    public override async Task Equality_nullable_enum_to_constant()
    {
        await base.Equality_nullable_enum_to_constant();

        AssertMql(
    """
    NullableBasicTypesEntities.{ "$match" : { "Enum" : 0 } }
    """);
    }

    public override async Task Equality_nullable_enum_to_null_constant()
    {
        await base.Equality_nullable_enum_to_null_constant();

        AssertMql(
    """
    NullableBasicTypesEntities.{ "$match" : { "Enum" : null } }
    """);
    }

    public override async Task Equality_nullable_enum_to_null_parameter()
    {
        await base.Equality_nullable_enum_to_null_parameter();

        AssertMql(
    """
    NullableBasicTypesEntities.{ "$match" : { "Enum" : null } }
    """);
    }

    public override async Task Equality_nullable_enum_to_nullable_parameter()
    {
        await base.Equality_nullable_enum_to_nullable_parameter();

        AssertMql(
    """
    NullableBasicTypesEntities.{ "$match" : { "Enum" : 0 } }
    """);
    }

    public override async Task Equality_nullable_enum_to_parameter()
    {
        await base.Equality_nullable_enum_to_parameter();

        AssertMql(
    """
    NullableBasicTypesEntities.{ "$match" : { "Enum" : 0 } }
    """);
    }

    public override async Task Equality_to_constant()
    {
        await base.Equality_to_constant();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "Enum" : 0 } }
    """);
    }

    public override async Task Equality_to_parameter()
    {
        await base.Equality_to_parameter();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "Enum" : 0 } }
    """);
    }

    // Fails: DateOnly/TimeOnly/TimeSpan/DateTimeOffset members and methods are not translated by the driver EF-X321
    public override Task HasFlag()
        => SpecFails.Async<global::MongoDB.Driver.Linq.ExpressionNotSupportedException>(() => base.HasFlag(), "Expression not supported: Eight.HasFlag(Convert(b.FlagsEnum, Enum)).");

    public override async Task HasFlag_with_non_nullable_parameter()
    {
        await base.HasFlag_with_non_nullable_parameter();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "FlagsEnum" : { "$bitsAllSet" : 8 } } }
    """);
    }

    public override async Task HasFlag_with_nullable_parameter()
    {
        await base.HasFlag_with_nullable_parameter();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "FlagsEnum" : { "$bitsAllSet" : 8 } } }
    """);
    }

    public override async Task Where_bitwise_and_nullable_enum_with_non_nullable_parameter()
    {
        await base.Where_bitwise_and_nullable_enum_with_non_nullable_parameter();

        AssertMql(
    """
    NullableBasicTypesEntities.{ "$match" : { "$expr" : { "$gt" : [{ "$bitAnd" : ["$FlagsEnum", 8] }, 0] } } }
    """);
    }

    public override async Task Where_bitwise_and_nullable_enum_with_null_constant()
    {
        await base.Where_bitwise_and_nullable_enum_with_null_constant();

        AssertMql(
    """
    NullableBasicTypesEntities.{ "$match" : { "$expr" : { "$gt" : [{ "$bitAnd" : ["$FlagsEnum", null] }, 0] } } }
    """);
    }

    public override async Task Where_bitwise_and_nullable_enum_with_nullable_parameter()
    {
        await base.Where_bitwise_and_nullable_enum_with_nullable_parameter();

        AssertMql(
    """
    NullableBasicTypesEntities.{ "$match" : { "$expr" : { "$gt" : [{ "$bitAnd" : ["$FlagsEnum", 8] }, 0] } } }
    """,
                //
                """
    NullableBasicTypesEntities.{ "$match" : { "$expr" : { "$gt" : [{ "$bitAnd" : ["$FlagsEnum", null] }, 0] } } }
    """);
    }
    // </generated>

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);
}

#endif
