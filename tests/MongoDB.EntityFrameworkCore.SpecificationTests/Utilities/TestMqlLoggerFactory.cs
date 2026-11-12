// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Originally EF Core TestSqlLoggerFactory.cs

// https://github.com/dotnet/efcore/issues/36413 submitted to consolidate this upstream

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Diagnostics;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

public class TestMqlLoggerFactory : ListLoggerFactory
{
    private const string FileNewLine = @"
";

    private static readonly string Eol = Environment.NewLine;
    private static readonly QueryBaselineRewriter Rewriter = new(
        BuildParseOptions(),
        File.GetLastWriteTimeUtc(typeof(TestMqlLoggerFactory).Assembly.Location));

    public TestMqlLoggerFactory()
        : this(_ => true)
    {
    }

    public TestMqlLoggerFactory(Func<string, bool> shouldLogCategory)
        : base(c => shouldLogCategory(c) || c == DbLoggerCategory.Database.Command.Name)
        => Logger = new TestMqlLogger();

    public IReadOnlyList<string> MqlStatements
        => ((TestMqlLogger)Logger).MqlStatements;

    public void AssertBaseline(string[] expected, bool assertOrder = true)
    {
        // Differential parity runs only: MQL legitimately differs between the native and driver-LINQ paths.
        if (Environment.GetEnvironmentVariable("MONGODB_EF_SKIP_MQL_ASSERTIONS") == "1")
        {
            return;
        }

        try
        {
            if (assertOrder)
            {
                for (var i = 0; i < expected.Length; i++)
                {
                    Assert.Equal(expected[i], MqlStatements[i], ignoreLineEndingDifferences: true);
                }

                Assert.Empty(MqlStatements.Skip(expected.Length));
            }
            else
            {
                foreach (var expectedFragment in expected)
                {
                    var normalizedExpectedFragment = expectedFragment.Replace("\r", string.Empty).Replace("\n", Eol);
                    Assert.Contains(
                        normalizedExpectedFragment,
                        MqlStatements);
                }
            }
        }
        catch
        {
            var methodCallLine = Environment.StackTrace.Split(
                [Eol],
                StringSplitOptions.RemoveEmptyEntries)[3][6..];

            var indexMethodEnding = methodCallLine.IndexOf(')') + 1;
            var testName = methodCallLine.Substring(0, indexMethodEnding);
            var parts = methodCallLine[indexMethodEnding..].Split(" ", StringSplitOptions.RemoveEmptyEntries);
            var fileName = parts[1][..^5];
            var lineNumber = int.Parse(parts[2]);

            var currentDirectory = Directory.GetCurrentDirectory();
            var logFile = currentDirectory.Substring(
                              0,
                              currentDirectory.LastIndexOf(
                                  $"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}",
                                  StringComparison.Ordinal)
                              + 1)
                          + "QueryBaseline.txt";

            var testInfo = testName + " : " + lineNumber + FileNewLine;
            const string indent = FileNewLine + "                ";

            if (Environment.GetEnvironmentVariable("EF_TEST_REWRITE_BASELINES")?.ToUpper() is "1" or "TRUE")
            {
                RewriteSourceWithNewBaseline(fileName, lineNumber);
            }

            var mql = string.Join(
                "," + indent + "//" + indent,
                MqlStatements.Take(9).Select(mql => "\"\"\"" + FileNewLine + mql + FileNewLine + "\"\"\""));

            var newBaseLine = $@"        AssertMql(
{mql});

";

            if (MqlStatements.Count > 9)
            {
                newBaseLine += "Output truncated.";
            }

            Logger.TestOutputHelper?.WriteLine("---- New Baseline -------------------------------------------------------------------");
            Logger.TestOutputHelper?.WriteLine(newBaseLine);

            var contents = testInfo + newBaseLine + FileNewLine + "--------------------" + FileNewLine;

            File.AppendAllText(logFile, contents);

            throw;
        }

        Clear();

        void RewriteSourceWithNewBaseline(string fileName, int lineNumber)
        {
            var result = Rewriter.Rewrite(fileName, lineNumber, MqlStatements);
            if (result.Message is not null)
            {
                // Never throw from here: the caller rethrows the assertion failure, which is the signal that matters.
                Logger.TestOutputHelper?.WriteLine(result.Message);
            }
        }
    }

    protected class TestMqlLogger : ListLogger
    {
        public List<string> MqlStatements { get; } = [];

        protected override void UnsafeClear()
        {
            base.UnsafeClear();
            MqlStatements.Clear();
        }

        protected override void UnsafeLog<TState>(
            LogLevel logLevel,
            EventId eventId,
            string? message,
            TState state,
            Exception? exception)
        {
            if (eventId.Id == MongoEventId.ExecutedMqlQuery)
            {
                if (message != null)
                {
                    var structure = (IReadOnlyList<KeyValuePair<string, object>>?)state
                                    ?? throw new InvalidOperationException("Expected a structured log state.");
                    var collectionName = structure.Where(i => i.Key == "collectionNamespace")
                        .Select(i => (CollectionNamespace)i.Value).First().CollectionName;

                    var mql = structure.Where(i => i.Key == "queryMql").Select(i => (string)i.Value).First();

                    MqlStatements.Add($"{collectionName}.{mql}");
                }
            }

            base.UnsafeLog(logLevel, eventId, message, state, exception);
        }
    }

    // Parse with this build's EF-version symbol so an AssertMql inside an active `#if EF10` arm is part of the syntax tree.
    private static CSharpParseOptions BuildParseOptions()
        => CSharpParseOptions.Default.WithPreprocessorSymbols(
#if EF11
            "EF11"
#elif EF10
            "EF10"
#elif EF9
            "EF9"
#else
            "EF8"
#endif
        );
}
