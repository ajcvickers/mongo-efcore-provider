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

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>How the provider chooses between native MQL generation and the driver-LINQ path.</summary>
internal enum NativeQueryMode
{
    /// <summary>Try native; silently fall back to the driver-LINQ path on anything unsupported.</summary>
    Auto,

    /// <summary>Use native or throw (no fallback) — surfaces native coverage gaps in tests.</summary>
    Force,

    /// <summary>Always use the driver-LINQ path.</summary>
    Off
}

internal static class NativeQuery
{
    // Test-only override (null when MONGODB_EF_NATIVE_QUERY is unset/unrecognized).
    private static readonly NativeQueryMode? EnvOverride = ParseEnv(Environment.GetEnvironmentVariable("MONGODB_EF_NATIVE_QUERY"));

    private static NativeQueryMode? ParseEnv(string? value)
        => value?.Trim().ToLowerInvariant() switch
        {
            "force" => NativeQueryMode.Force,
            "off" => NativeQueryMode.Off,
            "auto" => NativeQueryMode.Auto,
            _ => null
        };

    /// <summary>Effective mode for a query: the per-context <c>UseNativeQuery</c> option, overridden by the test-only env var.</summary>
    public static NativeQueryMode EffectiveMode(bool optionEnabled)
        => !optionEnabled ? NativeQueryMode.Off : (EnvOverride ?? NativeQueryMode.Auto);
}
