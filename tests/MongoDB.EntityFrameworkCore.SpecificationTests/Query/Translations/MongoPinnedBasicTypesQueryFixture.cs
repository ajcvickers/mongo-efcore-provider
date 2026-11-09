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

using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query.Translations;

/// <summary>
/// The basic-types fixture for the translation suites ported in #35. Those suites are baselined in the default query mode
/// only, so this fixture pins it (the NativeOnly baselines are a follow-up), and it has its own database so that it never
/// shares a store with the unpinned fixture used by the Math and String suites.
/// </summary>
public class MongoPinnedBasicTypesQueryFixture : MongoBasicTypesQueryFixture
{
    private static readonly string UniquePinnedStoreName = TestDatabaseNamer.GetUniqueDatabaseName("BasicTypesPinned");

    protected override string StoreName
        => UniquePinnedStoreName;

    protected override bool PinDefaultQueryMode
        => true;
}

#endif
