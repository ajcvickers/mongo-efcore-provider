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

using System.Runtime.ExceptionServices;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

/// <summary>
/// Lets a fixture whose model, seed data or store creation currently fails still be constructed, remembering the
/// failure and re-throwing it from the first context the test creates. That turns an otherwise class-wide fixture
/// failure into a per-test failure that <see cref="SpecFails"/> can assert on.
/// </summary>
public sealed class InitFailureGuard
{
    private Exception? _failure;

    public async LifetimeTask RunAsync(Func<LifetimeTask> initialize)
    {
        try
        {
            await initialize().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _failure = e;
        }
    }

    public void ThrowIfFailed()
    {
        if (_failure != null)
        {
            ExceptionDispatchInfo.Capture(_failure).Throw();
        }
    }
}
