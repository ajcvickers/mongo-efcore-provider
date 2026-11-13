// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Originally the baseline rewriting in EF Core TestSqlLoggerFactory.cs. Rewritten for https://github.com/ajcvickers/mongo-efcore-provider/issues/64:
// the original skipped over the old call by character count from whatever Roslyn node contained the line start, so a line number that did not
// start an AssertMql call (stale build, off by one, another process) silently damaged the file.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

/// <summary>
///     Result of a rewrite attempt. When <see cref="Rewritten" /> is false the source file was not touched.
/// </summary>
internal readonly record struct QueryBaselineRewriteResult(bool Rewritten, string? Message);

/// <summary>
///     Replaces the argument list of the <c>AssertMql(...)</c> call that starts on a given line of a test source file with the
///     captured MQL. It either produces a file that differs from the original only inside that argument list, or leaves the
///     original untouched and says why. It never throws for a problem with the target file.
/// </summary>
internal sealed class QueryBaselineRewriter
{
    private readonly CSharpParseOptions _parseOptions;
    private readonly DateTime? _builtUtc;
    private readonly ConcurrentDictionary<string, FileState> _files = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="parseOptions">Must define the same <c>#if</c> symbols the test assembly was compiled with.</param>
    /// <param name="assemblyBuiltUtc">
    ///     When the running test assembly was built. A source file modified after this is not the file the line numbers
    ///     in the stack trace refer to, so it is not rewritten. Null disables the check.
    /// </param>
    public QueryBaselineRewriter(CSharpParseOptions parseOptions, DateTime? assemblyBuiltUtc)
    {
        _parseOptions = parseOptions;
        _builtUtc = assemblyBuiltUtc;
    }

    public QueryBaselineRewriteResult Rewrite(string fileName, int lineNumber, IReadOnlyList<string> mqlStatements)
    {
        try
        {
            var fullPath = Path.GetFullPath(fileName);
            var state = _files.GetOrAdd(fullPath, _ => new FileState());

            lock (state)
            {
                var mutex = AcquireCrossProcessLock(fullPath);
                if (mutex is null)
                {
                    return Refused(fullPath, lineNumber, "timed out waiting for another process that is rewriting the same file");
                }

                try
                {
                    return RewriteLocked(fullPath, lineNumber, mqlStatements, state);
                }
                finally
                {
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }
            }
        }
        catch (Exception exception)
        {
            return Refused(fileName, lineNumber, $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private QueryBaselineRewriteResult RewriteLocked(string path, int lineNumber, IReadOnlyList<string> mqlStatements, FileState state)
    {
        // The same line is reported once per theory row.
        if (!state.ProcessedLines.Add(lineNumber))
        {
            return new QueryBaselineRewriteResult(false, null);
        }

        var originalBytes = File.ReadAllBytes(path);
        var originalHash = SHA256.HashData(originalBytes);

        if (state.LastWrittenHash is null)
        {
            // First time this process touches the file: the line number comes from the PDB of the build that is running.
            if (_builtUtc is { } built && File.GetLastWriteTimeUtc(path) > built)
            {
                return Refused(
                    path, lineNumber,
                    "the file was modified after this test assembly was built, so the line number is stale; rebuild and run again");
            }
        }
        else if (!originalHash.AsSpan().SequenceEqual(state.LastWrittenHash))
        {
            return Refused(
                path, lineNumber,
                "the file was changed by something other than this rewriter since its last rewrite in this run (another process or an editor); line numbers can no longer be trusted");
        }

        // Account for lines added or removed by earlier rewrites in this run (keys are original line numbers).
        var currentLine = lineNumber;
        foreach (var displacement in state.LineDisplacements)
        {
            if (displacement.Key < lineNumber)
            {
                currentLine += displacement.Value;
            }
            else
            {
                break;
            }
        }

        var hasBom = originalBytes.AsSpan().StartsWith(Utf8Bom);
        string text;
        try
        {
            text = new UTF8Encoding(false, throwOnInvalidBytes: true)
                .GetString(originalBytes, hasBom ? Utf8Bom.Length : 0, originalBytes.Length - (hasBom ? Utf8Bom.Length : 0));
        }
        catch (DecoderFallbackException)
        {
            return Refused(path, lineNumber, "the file is not valid UTF-8");
        }

        var oldTree = CSharpSyntaxTree.ParseText(text, _parseOptions);
        var root = oldTree.GetRoot();

        var matches = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(
                i => i.Expression is IdentifierNameSyntax { Identifier.ValueText: var name }
                    && name.StartsWith("AssertMql", StringComparison.Ordinal)
                    && StartLine(oldTree, i) == currentLine)
            .ToList();

        if (matches.Count != 1)
        {
            return Refused(
                path, lineNumber,
                matches.Count == 0
                    ? $"no AssertMql call starts at line {currentLine}{(currentLine == lineNumber ? "" : $" (line {lineNumber} in the stack trace)")}; "
                    + "the line is stale, or the call is in an #if arm this build does not compile"
                    : $"{matches.Count} AssertMql calls start at line {currentLine}");
        }

        var invocation = matches[0];
        var argumentList = invocation.ArgumentList;

        var eol = text.Contains("\r\n") ? "\r\n" : "\n";
        var callIndent = LeadingWhitespace(oldTree, currentLine);

        // Keep the layout of the baseline being replaced so that regenerating an unchanged baseline changes nothing: committed baselines
        // have the opening """ either indented or in column 0. A call with no arguments yet gets the (more common) indented form.
        var argumentIndent = callIndent + "    ";
        var separatorIndent = argumentIndent;
        var arguments = argumentList.Arguments;
        if (arguments.Count > 0 && StartLine(oldTree, arguments[0]) > currentLine)
        {
            argumentIndent = LeadingWhitespace(oldTree, StartLine(oldTree, arguments[0]));
            separatorIndent = argumentIndent;

            var comment = arguments.Skip(1)
                .SelectMany(a => a.GetLeadingTrivia())
                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia));
            if (comment != default)
            {
                separatorIndent = LeadingWhitespace(oldTree, oldTree.GetLineSpan(comment.Span).StartLinePosition.Line + 1);
            }
        }

        var newArguments = mqlStatements.Count == 0
            ? "()"
            : "(" + eol
            + string.Join(
                "," + eol + separatorIndent + "//" + eol,
                mqlStatements.Select(
                    mql => argumentIndent + "\"\"\"" + eol + NormalizeEol(mql, eol) + eol + "\"\"\""))
            + ")";

        var newText = string.Concat(
            text.AsSpan(0, argumentList.Span.Start),
            newArguments,
            text.AsSpan(argumentList.Span.End));

        // Validate before touching the file.
        var newTree = CSharpSyntaxTree.ParseText(newText, _parseOptions);
        if (CountErrors(newTree) > CountErrors(oldTree))
        {
            return Refused(path, lineNumber, "the rewritten file would not parse");
        }

        if (CountAssertMqlCalls(newTree) != CountAssertMqlCalls(oldTree)
            || !newText.StartsWith(text[..argumentList.Span.Start], StringComparison.Ordinal)
            || !newText.EndsWith(text[argumentList.Span.End..], StringComparison.Ordinal))
        {
            return Refused(path, lineNumber, "the rewritten file would differ from the original outside the AssertMql arguments");
        }

        var newBytes = (hasBom ? Utf8Bom : [])
            .Concat(new UTF8Encoding(false).GetBytes(newText))
            .ToArray();

        // Write beside the target, then swap, so a failure can never leave a half-written source file.
        var tempPath = $"{path}.{Environment.ProcessId}.tmp";
        try
        {
            File.WriteAllBytes(tempPath, newBytes);

            if (!SHA256.HashData(File.ReadAllBytes(path)).AsSpan().SequenceEqual(originalHash))
            {
                return Refused(path, lineNumber, "the file changed while it was being rewritten");
            }

            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }

        state.LastWrittenHash = SHA256.HashData(newBytes);

        var lineDiff = newTree.GetText().Lines.Count - oldTree.GetText().Lines.Count;
        if (lineDiff != 0)
        {
            state.LineDisplacements[lineNumber] = lineDiff;
        }

        return new QueryBaselineRewriteResult(true, null);
    }

    private static int StartLine(SyntaxTree tree, SyntaxNode node)
        => tree.GetLineSpan(new Microsoft.CodeAnalysis.Text.TextSpan(node.SpanStart, 0)).StartLinePosition.Line + 1;

    private static string LeadingWhitespace(SyntaxTree tree, int lineNumber)
    {
        var line = tree.GetText().Lines[lineNumber - 1].ToString();
        return new string(line.TakeWhile(c => c is ' ' or '\t').ToArray());
    }

    private static int CountErrors(SyntaxTree tree)
        => tree.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error);

    private static int CountAssertMqlCalls(SyntaxTree tree)
        => tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Count(i => i.Expression is IdentifierNameSyntax { Identifier.ValueText: var n } && n.StartsWith("AssertMql", StringComparison.Ordinal));

    private static string NormalizeEol(string value, string eol)
        => value.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", eol);

    private static QueryBaselineRewriteResult Refused(string path, int lineNumber, string reason)
        => new(false, $"Baseline NOT rewritten ({Path.GetFileName(path)}:{lineNumber}); the file was left unchanged: {reason}.");

    private static Mutex? AcquireCrossProcessLock(string path)
    {
        // Several test processes (one per EF version) may be asked to rewrite the same source file.
        var name = "ef-mongo-baseline-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path.ToUpperInvariant())))[..32];
        var mutex = new Mutex(false, name);
        try
        {
            if (mutex.WaitOne(TimeSpan.FromSeconds(60)))
            {
                return mutex;
            }
        }
        catch (AbandonedMutexException)
        {
            return mutex;
        }

        mutex.Dispose();
        return null;
    }

    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private sealed class FileState
    {
        /// <summary>Lines already rewritten (theories run the same line once per row).</summary>
        public HashSet<int> ProcessedLines { get; } = [];

        /// <summary>Original line number -> lines added (or removed, if negative) by the rewrite there.</summary>
        public SortedDictionary<int, int> LineDisplacements { get; } = new();

        /// <summary>Hash of the file as this rewriter last wrote it.</summary>
        public byte[]? LastWrittenHash { get; set; }
    }
}
