// TEMPORARY harvest-mode replacement for SpecFails.cs. NEVER COMMIT. Same public API; every call runs the test body,
// appends "<file>\t<member>\tPASS" or "<file>\t<member>\tTHROW\t<type>\t<class>\t<message>" to $SPEC_FAILS_HARVEST
// and never asserts. Used to learn what each overridden base test really does on the current provider.

using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit.Sdk;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

public static class SpecFails
{
    private static readonly object Gate = new();
    private static readonly string? Path = Environment.GetEnvironmentVariable("SPEC_FAILS_HARVEST");

    public static void Throws(Action query, string? messageFragment = null, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        => HarvestSync(query, m, f);

    public static Task ThrowsAsync(Func<Task> query, string? messageFragment = null, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        => HarvestAsync(query, m, f);

    public static void Throws<TException>(Action test, string? messageFragment = null, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        where TException : Exception
        => HarvestSync(test, m, f);

    public static Task ThrowsAsync<TException>(Func<Task> test, string? messageFragment = null, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        where TException : Exception
        => HarvestAsync(test, m, f);

    // Capture sites inspect the exception, so they are not harvested: they keep asserting.
    public static TException Capture<TException>(Action test)
        where TException : Exception
        => Assert.ThrowsAny<TException>(test);

    public static Task<TException> CaptureAsync<TException>(Func<Task> test)
        where TException : Exception
        => Assert.ThrowsAnyAsync<TException>(test);

    public static void TranslationFailed(Action test, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        => HarvestSync(test, m, f);

    public static Task TranslationFailedAsync(Func<Task> test, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        => HarvestAsync(test, m, f);

    public static Task Async<TException>(Func<Task> test, string messageFragment, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        where TException : Exception
        => HarvestAsync(test, m, f);

    public static void Sync<TException>(Action test, string messageFragment, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        where TException : Exception
        => HarvestSync(test, m, f);

    public static Task AsyncSomeRows<TException>(Func<Task> test, string messageFragment, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        where TException : Exception
        => HarvestAsync(test, m, f);

    public static void SyncSomeRows<TException>(Action test, string messageFragment, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        where TException : Exception
        => HarvestSync(test, m, f);

    public static void Either(Action test, string? reason = null, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        => HarvestSync(test, m, f);

    public static Task EitherAsync(Func<Task> test, string? reason = null, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        => HarvestAsync(test, m, f);

    public static void FailsInUtc(Action test, string? reason = null, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        => HarvestSync(test, m, f);

    public static Task FailsInUtcAsync(Func<Task> test, string? reason = null, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        => HarvestAsync(test, m, f);

    public static void AnyOutcome(Action test, string? reason = null, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        => HarvestSync(test, m, f);

    public static Task AnyOutcomeAsync(Func<Task> test, string? reason = null, [CallerMemberName] string? m = null, [CallerFilePath] string? f = null)
        => HarvestAsync(test, m, f);

    private static void HarvestSync(Action test, string? member, string? file)
    {
        try
        {
            test();
            Record(file, member, "PASS");
        }
        catch (Exception e)
        {
            Record(file, member, Describe(e));
        }
    }

    private static async Task HarvestAsync(Func<Task> test, string? member, string? file)
    {
        try
        {
            await test().ConfigureAwait(false);
            Record(file, member, "PASS");
        }
        catch (Exception e)
        {
            Record(file, member, Describe(e));
        }
    }

    private static string Describe(Exception e)
    {
        while (e is TargetInvocationException { InnerException: not null } or AggregateException { InnerException: not null })
        {
            e = e.InnerException!;
        }

        var message = (e.Message ?? "").Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        if (message.Length > 400)
        {
            message = message[..400];
        }

        var full = e.ToString();
        var cls = IsTranslationFailure(e) ? "TRANSLATION"
            : e is XunitException ? "XUNIT"
            : "OTHER";
        // a quick stable key for wrapped failures: the first inner exception type, when different
        var inner = e.InnerException?.GetType().FullName;
        return $"THROW\t{e.GetType().FullName}\t{cls}\t{message}\t{inner}";
    }

    // Mirrors MongoSpecTestHelpers.IsTranslationFailure (private there).
    private static bool IsTranslationFailure(Exception exception)
        => exception.GetType().Name is "NativeTranslationNotSupportedException" or "ExpressionNotSupportedException"
            || exception is InvalidOperationException { Message: var message }
            && (message.StartsWith("The LINQ expression ", StringComparison.Ordinal)
                || message.StartsWith("The following 'ExecuteUpdate' or 'ExecuteDelete' expression could not be translated", StringComparison.Ordinal)
                || message.Contains("ShapedQueryExpression.VisitChildren", StringComparison.Ordinal)
                || message.Contains("Unsupported cross-DbSet query", StringComparison.Ordinal));

    private static void Record(string? file, string? member, string outcome)
    {
        if (string.IsNullOrEmpty(Path))
        {
            return;
        }

        var name = System.IO.Path.GetFileNameWithoutExtension(file ?? "?");
        lock (Gate)
        {
            File.AppendAllText(Path, $"{name}\t{member}\t{outcome}\n");
        }
    }
}
