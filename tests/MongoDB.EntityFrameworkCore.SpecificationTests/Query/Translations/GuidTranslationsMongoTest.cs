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

public class GuidTranslationsMongoTest : GuidTranslationsTestBase<MongoBasicTypesQueryFixture>
{
    public GuidTranslationsMongoTest(MongoBasicTypesQueryFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
    public override async Task NewGuid()
    {
        await base.NewGuid();

        AssertMql(
    """
    BasicTypesEntities.
    """);
    }

    public override async Task New_with_constant()
    {
        await base.New_with_constant();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "Guid" : { "$binary" : { "base64" : "3zb0k0Y/QSOD+WsTXe63ug==", "subType" : "04" } } } }
    """);
    }

    public override async Task New_with_parameter()
    {
        await base.New_with_parameter();

        AssertMql(
    """
    BasicTypesEntities.{ "$match" : { "Guid" : { "$binary" : { "base64" : "3zb0k0Y/QSOD+WsTXe63ug==", "subType" : "04" } } } }
    """);
    }

    public override async Task ToString_projection()
    {
        await base.ToString_projection();

        AssertMql(
    """
    BasicTypesEntities.{ "$project" : { "_v" : { "$toString" : "$Guid" }, "_id" : 0 } }
    """);
    }
    // </generated>

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);
}

#endif
