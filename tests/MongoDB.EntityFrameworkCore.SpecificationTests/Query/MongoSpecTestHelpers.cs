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
using System.Threading.Tasks;
using MongoDB.Driver.Linq;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;
using Xunit;
using Xunit.Sdk;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

/// <summary>
/// Shared assertion helpers for the Northwind specification-test overrides.
/// </summary>
internal static class MongoSpecTestHelpers
{
    /// <summary>
    /// True when <c>MONGODB_EF_QUERY_MODE=NativeOnly</c> (or the alias <c>MONGODB_EF_NATIVE_ONLY=1</c>) flips every
    /// spec context to <c>MongoQueryMode.NativeOnly</c> (see <see cref="Utilities.MongoTestStore.AddProviderOptions"/>).
    /// Translation-failure baselines can differ: the driver-LINQ fallback may log a partial pipeline before failing,
    /// native-only may reject before logging. <c>MONGODB_EF_DIFFERENTIAL=1</c> (set only by the differential runner)
    /// makes this false so default-mode expectations are evaluated under both paths.
    /// </summary>
    internal static bool IsNativeOnly
        => FunctionalTests.Utilities.TestQueryMode.IsNativeOnly;

    /// <summary>
    /// True for the exceptions a not-yet-translatable query legitimately throws: the native translator's
    /// <see cref="NativeTranslationNotSupportedException"/>, the driver's <see cref="ExpressionNotSupportedException"/>,
    /// or an <see cref="InvalidOperationException"/> that is EF's "The LINQ expression ... could not be translated"
    /// failure, EF's "Calling 'ShapedQueryExpression.VisitChildren' is not allowed" (the projection binder could not
    /// handle a shaper) or the provider's "Unsupported cross-DbSet query" guard. Any other
    /// <see cref="InvalidOperationException"/> (e.g. a materialization bug) is not accepted.
    /// </summary>
    private static bool IsTranslationFailure(Exception exception)
        => exception is NativeTranslationNotSupportedException or ExpressionNotSupportedException
            || exception is InvalidOperationException
            {
                Message: var message
            }
            && (message.StartsWith("The LINQ expression ", StringComparison.Ordinal)
#if EF11
                // EF11's wording for an ExecuteUpdate/ExecuteDelete that cannot be translated; the details are the inner exception.
                || message.StartsWith("The following 'ExecuteUpdate' or 'ExecuteDelete' expression could not be translated", StringComparison.Ordinal)
#endif
                || message.Contains("ShapedQueryExpression.VisitChildren", StringComparison.Ordinal)
                || message.Contains("Unsupported cross-DbSet query", StringComparison.Ordinal));

    /// <summary>
    /// Asserts that <paramref name="query"/> fails as a translation failure rather than returning (possibly wrong)
    /// data: <see cref="NativeTranslationNotSupportedException"/> under <c>NativeOnly</c>; EF's translation-failure
    /// <see cref="InvalidOperationException"/> (see <see cref="IsTranslationFailure"/>) or driver
    /// <see cref="ExpressionNotSupportedException"/> under <c>Native</c>. <paramref name="additionalAcceptedTypes"/> adds types a suite's fallback genuinely throws (e.g.
    /// <see cref="ArgumentException"/>/<see cref="FormatException"/> for GroupBy). xUnit assertion exceptions are never
    /// accepted, so a wrong-data regression still fails.
    /// </summary>
    internal static async Task AssertNativeTranslationFailedAsync(
        Func<Task> query, params Type[] additionalAcceptedTypes)
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(query);
        if (IsTranslationFailure(exception))
        {
            return;
        }

        foreach (var acceptedType in additionalAcceptedTypes)
        {
            if (acceptedType.IsInstanceOfType(exception))
            {
                return;
            }
        }

        throw new XunitException(
            $"Expected a translation failure but the query threw {exception.GetType()}: {exception.Message}");
    }

    /// <summary>
    /// Asserts that a spec test with a known, ticketed gap (tagged with a <c>// Fails:</c> comment) still fails, and fails as a
    /// <em>refusal</em>: a provider/EF <see cref="InvalidOperationException"/>, <see cref="NotSupportedException"/>,
    /// <see cref="NativeTranslationNotSupportedException"/> or driver <see cref="ExpressionNotSupportedException"/>. xUnit assertion
    /// failures (wrong data) and <see cref="NullReferenceException"/> are not refusals and are rejected, so a gap that starts
    /// returning wrong rows is noticed. A gap that really is wrong data or a crash passes <paramref name="expectedType"/> (exact
    /// type) so that is stated at the site; <paramref name="messageContains"/> optionally pins the message. When the gap is
    /// closed the test throws nothing, this assertion fails, and the override should be replaced by the plain base call and its
    /// MQL baseline.
    /// </summary>
    internal static async Task AssertKnownGapAsync(Func<Task> test, Type? expectedType = null, string? messageContains = null)
        => CheckKnownGap(await Assert.ThrowsAnyAsync<Exception>(test), expectedType, messageContains);

    /// <summary>Synchronous counterpart of <see cref="AssertKnownGapAsync"/> (model-building tests).</summary>
    internal static void AssertKnownGap(Action test, Type? expectedType = null, string? messageContains = null)
        => CheckKnownGap(Assert.ThrowsAny<Exception>(test), expectedType, messageContains);

    private static void CheckKnownGap(Exception exception, Type? expectedType, string? messageContains)
    {
        var accepted = expectedType is null
            ? exception is InvalidOperationException or NotSupportedException or NativeTranslationNotSupportedException
                or ExpressionNotSupportedException
            : expectedType == typeof(Exception) || exception.GetType() == expectedType;
        if (!accepted)
        {
            throw new XunitException(
                $"Expected the known gap to fail as {expectedType?.ToString() ?? "a refusal"} but it threw "
                + $"{exception.GetType()}: {exception.Message}");
        }

        if (messageContains is not null && !exception.Message.Contains(messageContains, StringComparison.Ordinal))
        {
            throw new XunitException(
                $"The known gap threw {exception.GetType()} but its message does not contain '{messageContains}': {exception.Message}");
        }
    }

    /// <summary>
    /// Synchronous counterpart of <see cref="AssertNativeTranslationFailedAsync"/>, for the handful of
    /// non-async Northwind test overrides.
    /// </summary>
    internal static void AssertNativeTranslationFailed(Action query, params Type[] additionalAcceptedTypes)
    {
        var exception = Assert.ThrowsAny<Exception>(query);
        if (IsTranslationFailure(exception))
        {
            return;
        }

        foreach (var acceptedType in additionalAcceptedTypes)
        {
            if (acceptedType.IsInstanceOfType(exception))
            {
                return;
            }
        }

        throw new XunitException(
            $"Expected a translation failure but the query threw {exception.GetType()}: {exception.Message}");
    }

    /// <summary>
    /// Asserts that <paramref name="query"/> is rejected as an unsupported cross-<c>DbSet</c> query: an
    /// <see cref="InvalidOperationException"/> ("Unsupported cross-DbSet query between") under driver-LINQ, or
    /// <see cref="NativeTranslationNotSupportedException"/> under native-only.
    /// </summary>
    internal static async Task AssertNoMultiCollectionQuerySupportAsync(Func<Task> query)
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(query);
        if (exception is NativeTranslationNotSupportedException)
        {
            return;
        }

        Assert.Contains("Unsupported cross-DbSet query between", Assert.IsType<InvalidOperationException>(exception).Message);
    }
}
