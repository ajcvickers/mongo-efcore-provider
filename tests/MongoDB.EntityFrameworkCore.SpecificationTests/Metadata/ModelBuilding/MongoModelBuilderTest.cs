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

#if !EF8

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ModelBuilding;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Metadata.ModelBuilding;

public class MongoModelBuilderTest : ModelBuilderTest
{
    public abstract class MongoComplexType(MongoModelBuilderFixture fixture)
        : ComplexTypeTestBase(fixture), IClassFixture<MongoModelBuilderFixture>;

#if EF10
    public abstract class MongoComplexCollection(MongoModelBuilderFixture fixture)
        : ComplexCollectionTestBase(fixture), IClassFixture<MongoModelBuilderFixture>;
#endif

    public class MongoModelBuilderFixture : ModelBuilderFixtureBase
    {
        public override TestHelpers TestHelpers
            => MongoTestHelpers.Instance;

        public override bool ForeignKeysHaveIndexes
            => false;
    }
}

#endif
