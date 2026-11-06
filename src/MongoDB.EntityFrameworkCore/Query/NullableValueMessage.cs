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

using System;

namespace MongoDB.EntityFrameworkCore.Query;

/// <summary>
/// The text the runtime puts on the exception for reading <c>.Value</c> of a null <see cref="Nullable{T}"/>. The native read side
/// throws it where driver-LINQ (via the compiled <c>.Value</c>) did, so the two answer with the same message. It was
/// "Nullable object must have a value." up to .NET 10 and is worded differently from .NET 11, so take it from the runtime.
/// </summary>
internal static class NullableValueMessage
{
    public static string Text { get; } = Create();

    private static string Create()
    {
        try
        {
            int? nothing = null;
            _ = nothing!.Value;
        }
        catch (InvalidOperationException exception)
        {
            return exception.Message;
        }

        return "Nullable object must have a value.";
    }
}
