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

using System.Reflection;
using Xunit.Sdk;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

/// <summary>
/// Helpers for specification-test overrides that assert the provider's CURRENT (failing) behavior, so the suite
/// stays green and any change in behavior (including the test starting to pass) is detected. Use together with
/// a <c>// Fails: reason ticket</c> comment; see docs/failing-spec-tests.md.
/// </summary>
/// <remarks>
/// Pick by what the override needs to check:
/// <list type="bullet">
/// <item><see cref="Throws"/> / <see cref="ThrowsAsync(Func{Task}, string?)"/>: any exception, optional fragment matched against the whole
/// exception (including inner exceptions and stack trace).</item>
/// <item><see cref="Throws{TException}"/> / <see cref="ThrowsAsync{TException}"/>: exactly this exception type, fragment matched
/// against its message; a failure reports that the known failure changed or that the test now passes.</item>
/// <item><see cref="Sync{TException}"/> / <see cref="Async{TException}"/>: like the typed variants, but unwraps
/// <see cref="TargetInvocationException"/> / <see cref="AggregateException"/> first (reflection and theory data rows).</item>
/// <item><see cref="SyncSomeRows{TException}"/> / <see cref="AsyncSomeRows{TException}"/>: theories where only some data rows
/// fail (or fail only in some environments): accepts success or the expected failure.</item>
/// </list>
/// </remarks>
public static class SpecFails
{
    public static void Throws(Action query, string? messageFragment = null)
    {
        var exception = Assert.ThrowsAny<Exception>(query);
        CheckAnyException(exception, messageFragment);
    }

    public static async Task ThrowsAsync(Func<Task> query, string? messageFragment = null)
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(query);
        CheckAnyException(exception, messageFragment);
    }

    public static void Throws<TException>(Action test, string messageFragment)
        where TException : Exception
    {
        try
        {
            test();
        }
        catch (TException e)
        {
            CheckMessage(e, messageFragment);
            return;
        }

        throw new XunitException(
            $"Expected the known failure {typeof(TException).Name} ('{messageFragment}') but the test passed. Remove the override and its // Fails tag.");
    }

    public static async Task ThrowsAsync<TException>(Func<Task> test, string messageFragment)
        where TException : Exception
    {
        try
        {
            await test().ConfigureAwait(false);
        }
        catch (TException e)
        {
            CheckMessage(e, messageFragment);
            return;
        }

        throw new XunitException(
            $"Expected the known failure {typeof(TException).Name} ('{messageFragment}') but the test passed. Remove the override and its // Fails tag.");
    }

    public static async Task Async<TException>(Func<Task> test, string messageFragment)
        where TException : Exception
    {
        try
        {
            await test();
        }
        catch (Exception e)
        {
            CheckUnwrapped<TException>(e, messageFragment);
            return;
        }

        Assert.Fail($"Expected {typeof(TException).Name} containing '{messageFragment}' but the test passed.");
    }

    public static void Sync<TException>(Action test, string messageFragment)
        where TException : Exception
    {
        try
        {
            test();
        }
        catch (Exception e)
        {
            CheckUnwrapped<TException>(e, messageFragment);
            return;
        }

        Assert.Fail($"Expected {typeof(TException).Name} containing '{messageFragment}' but the test passed.");
    }

    /// <summary>
    /// Exactly this exception type (any message), unwrapping reflection and aggregate wrappers. For failures whose message
    /// is not stable across query modes or EF versions.
    /// </summary>
    public static void Throws<TException>(Action test)
        where TException : Exception
        => Sync<TException>(test, string.Empty);

    /// <inheritdoc cref="Throws{TException}(Action)"/>
    public static Task ThrowsAsync<TException>(Func<Task> test)
        where TException : Exception
        => Async<TException>(test, string.Empty);

    /// <summary>
    /// The query fails to translate, in whichever form the active query mode reports it (see
    /// <c>MongoSpecTestHelpers.AssertNativeTranslationFailed</c>). A wrong-data failure from the test body is not accepted.
    /// </summary>
    public static void TranslationFailed(Action test, params Type[] additionalAcceptedTypes)
        => Query.MongoSpecTestHelpers.AssertNativeTranslationFailed(test, additionalAcceptedTypes);

    /// <inheritdoc cref="TranslationFailed(Action, Type[])"/>
    public static Task TranslationFailedAsync(Func<Task> test, params Type[] additionalAcceptedTypes)
        => Query.MongoSpecTestHelpers.AssertNativeTranslationFailedAsync(test, additionalAcceptedTypes);

    /// <summary>For theories where only some data rows fail: accepts success or the expected failure.</summary>
    public static async Task AsyncSomeRows<TException>(Func<Task> test, string messageFragment)
        where TException : Exception
    {
        try
        {
            await test();
        }
        catch (Exception e)
        {
            CheckUnwrapped<TException>(e, messageFragment);
        }
    }

    /// <summary>For theories where only some data rows fail: accepts success or the expected failure.</summary>
    public static void SyncSomeRows<TException>(Action test, string messageFragment)
        where TException : Exception
    {
        try
        {
            test();
        }
        catch (Exception e)
        {
            CheckUnwrapped<TException>(e, messageFragment);
        }
    }

    private static void CheckAnyException(Exception exception, string? messageFragment)
    {
        if (messageFragment != null)
        {
            Assert.Contains(messageFragment, exception.ToString());
        }
    }

    private static void CheckMessage(Exception e, string messageFragment)
    {
        if (!e.Message.Contains(messageFragment, StringComparison.Ordinal))
        {
            throw new XunitException(
                $"The known failure changed. Expected message to contain '{messageFragment}' but got: {e.Message}");
        }
    }

    private static void CheckUnwrapped<TException>(Exception exception, string messageFragment)
        where TException : Exception
    {
        while (exception is TargetInvocationException { InnerException: not null } or AggregateException { InnerException: not null }
               && exception is not TException)
        {
            exception = exception.InnerException!;
        }

        Assert.IsAssignableFrom<TException>(exception);
        Assert.Contains(messageFragment, exception.Message);
    }
}
