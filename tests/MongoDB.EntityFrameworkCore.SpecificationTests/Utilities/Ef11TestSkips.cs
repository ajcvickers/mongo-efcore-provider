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

#if EF11
using Xunit;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

/// <summary>
/// Skips named inherited tests at run time on EF11.
/// </summary>
/// <remarks>
/// On EF8 to EF10 an upstream test that is public but not virtual is hidden with a <c>new</c> method that asserts the known failure or
/// is skipped. xUnit v3 (EF11) rejects such a class ("has overloads, which is not supported"), so on EF11 the hiding method is not
/// compiled and the inherited test is skipped here instead, from a field initializer in the test class. The ticket is in the reason.
/// </remarks>
internal static class Ef11TestSkips
{
    internal static object? SkipIfListed(string reason, params string[] methodNames)
    {
        var current = TestContext.Current.TestMethod?.MethodName;
        if (current is not null && Array.IndexOf(methodNames, current) >= 0)
        {
            Assert.Skip(reason);
        }

        return null;
    }
}
#endif
