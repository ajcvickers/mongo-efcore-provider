// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

/// <summary>
///     Tests for <see cref="QueryBaselineRewriter" /> (EF_TEST_REWRITE_BASELINES), run against copies of a small fixture source.
///     Guards https://github.com/ajcvickers/mongo-efcore-provider/issues/64: the rewriter must either make exactly the requested
///     change or leave the file untouched.
/// </summary>
public sealed class QueryBaselineRewriterTests : IDisposable
{
    private static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols("EF10");

    // Original argument lists (LF); '\n' is converted to the eol of the variant under test.
    private const string OrigA = "(\n\"\"\"\nold a\n\"\"\")";
    private const string OrigB = "()";
    private const string OrigC = "(\n\"\"\"\nold c\n\"\"\",\n//\n\"\"\"\nold c2\n\"\"\")";

    // The same, in the other layout used by committed baselines: opening """ and // indented under the call.
    private const string IndentedA = "(\n            \"\"\"\nold a\n\"\"\")";
    private const string IndentedC = "(\n            \"\"\"\nold c\n\"\"\",\n            //\n            \"\"\"\nold c2\n\"\"\")";

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "baseline-rewriter-" + Guid.NewGuid().ToString("N"));

    public QueryBaselineRewriterTests()
        => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    // Three targets (A, B, C) in order; the AssertMql in the inactive #else arm is not part of this build.
    private static string Fixture(string a, string b, string c)
        => Lf($$"""
             namespace Fixture;

             public class T
             {
                 private void AssertMql(params string[] expected) { }

                 public void A()
                 {
                     AssertMql{{a}};
                 }

             #if EF10 || EF11
                 public async Task B()
                 {
                     await Task.CompletedTask;

                     // A comment above the call.
                     AssertMql{{b}};
                 }
             #else
                 public void Inactive()
                 {
                     AssertMql("inactive");
                 }
             #endif

                 public void C()
                 {
                     AssertMql{{c}};
                 }
             }
             """);

    // The checkout may have CRLF in this source file; the fixture is always LF here.
    private static string Lf(string value)
        => value.Replace("\r\n", "\n");

    // The argument list the rewriter is expected to write, with the opening """ (and //) indented by 'inner' spaces.
    private static string NewArgs(int inner, string eol, params string[] mql)
    {
        var pad = new string(' ', inner);
        return "(" + eol + string.Join(
            "," + eol + pad + "//" + eol,
            mql.Select(m => pad + "\"\"\"" + eol + m + eol + "\"\"\"")) + ")";
    }

    private string Write(string text, string eol, bool bom)
    {
        var path = Path.Combine(_dir, "T.cs");
        var bytes = new UTF8Encoding(false).GetBytes(text.Replace("\n", eol));
        File.WriteAllBytes(path, bom ? [0xEF, 0xBB, 0xBF, .. bytes] : bytes);
        return path;
    }

    private static QueryBaselineRewriter Create(DateTime? builtUtc = null)
        => new(ParseOptions, builtUtc);

    public static IEnumerable<object[]> Variants()
        => from eol in new[] { "\n", "\r\n" }
            from bom in new[] { false, true }
            select new object[] { eol, bom };

    [Theory]
    [MemberData(nameof(Variants))]
    public void Rewrites_each_target_exactly(string eol, bool bom)
    {
        var original = Fixture(OrigA, OrigB, OrigC);
        var lines = original.Split('\n');
        // 1-based lines of the three calls: A is the first "AssertMql(" after the declaration, B has the comment above it, C is last.
        var lineA = Array.FindIndex(lines, l => l.Trim().StartsWith("AssertMql(")) + 1;
        var lineB = Array.FindIndex(lines, l => l.Contains("A comment above")) + 2;
        var lineC = Array.FindLastIndex(lines, l => l.Trim().StartsWith("AssertMql(")) + 1;

        // (line, mql, expected text)
        var cases = new (int Line, string[] Mql, string Expected)[]
        {
            // A and C already have the opening """ in column 0, which is kept; B has no arguments yet and gets the indented layout.
            (lineA, ["a.find"], Fixture(NewArgs(0, "\n", "a.find"), OrigB, OrigC)),
            (lineB, ["b1", "b2"], Fixture(OrigA, NewArgs(12, "\n", "b1", "b2"), OrigC)),
            (lineC, ["c1", "c2", "c3"], Fixture(OrigA, OrigB, NewArgs(0, "\n", "c1", "c2", "c3")))
        };

        foreach (var (line, mql, expected) in cases)
        {
            var path = Write(original, eol, bom);
            var result = Create().Rewrite(path, line, mql);

            Assert.True(result.Rewritten, result.Message);
            Assert.Equal(Expected(expected, eol, bom), File.ReadAllBytes(path));
            AssertNoTempFiles();
        }
    }

    private static byte[] Expected(string text, string eol, bool bom)
    {
        var bytes = new UTF8Encoding(false).GetBytes(text.Replace("\n", eol));
        return bom ? [0xEF, 0xBB, 0xBF, .. bytes] : bytes;
    }

    private void AssertNoTempFiles()
        => Assert.Equal("T.cs", Path.GetFileName(Assert.Single(Directory.GetFiles(_dir))));

    [Theory]
    [MemberData(nameof(Variants))]
    public void Two_rewrites_in_one_run_in_either_order_account_for_the_lines_added_or_removed(string eol, bool bom)
    {
        var original = Fixture(OrigA, OrigB, OrigC);
        var lines = original.Split('\n');
        var lineA = Array.FindIndex(lines, l => l.Trim().StartsWith("AssertMql(")) + 1;
        var lineB = Array.FindIndex(lines, l => l.Contains("A comment above")) + 2;
        var lineC = Array.FindLastIndex(lines, l => l.Trim().StartsWith("AssertMql(")) + 1;

        // A grows by many lines, C shrinks to a single statement, B has the same line count: all three orders must agree.
        var newA = NewArgs(0, "\n", "a1", "a2", "a3", "a4");
        var newB = NewArgs(12, "\n", "b1");
        var newC = NewArgs(0, "\n", "c1");
        var expected = Fixture(newA, newB, newC);

        foreach (var order in new[] { new[] { 0, 1, 2 }, [2, 1, 0], [1, 2, 0], [0, 2, 1] })
        {
            var path = Write(original, eol, bom);
            var rewriter = Create();
            foreach (var target in order)
            {
                var result = target switch
                {
                    0 => rewriter.Rewrite(path, lineA, ["a1", "a2", "a3", "a4"]),
                    1 => rewriter.Rewrite(path, lineB, ["b1"]),
                    _ => rewriter.Rewrite(path, lineC, ["c1"])
                };

                Assert.True(result.Rewritten, $"order {string.Join(",", order)}, target {target}: {result.Message}");
            }

            Assert.Equal(Expected(expected, eol, bom), File.ReadAllBytes(path));
            AssertNoTempFiles();
        }
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void A_line_that_does_not_start_an_AssertMql_call_leaves_the_file_untouched(string eol, bool bom)
    {
        var original = Fixture(OrigA, OrigB, OrigC);
        var lines = original.Split('\n');
        var targets = new[]
        {
            Array.FindIndex(lines, l => l.Trim().StartsWith("AssertMql(")) + 1,
            Array.FindIndex(lines, l => l.Contains("A comment above")) + 2,
            Array.FindLastIndex(lines, l => l.Trim().StartsWith("AssertMql(")) + 1
        };

        var expectedBytes = Expected(original, eol, bom);
        var path = Write(original, eol, bom);
        var untouched = 0;

        // Every line, including blank lines, comments, #if/#else/#endif directives, the AssertMql in the inactive arm, lines inside the
        // raw string literals, and lines past the end of the file. Before the fix, many of these damaged the file.
        for (var line = 0; line <= lines.Length + 3; line++)
        {
            File.WriteAllBytes(path, expectedBytes);
            var result = Create().Rewrite(path, line, ["x.find"]);

            if (targets.Contains(line))
            {
                Assert.True(result.Rewritten, result.Message);
            }
            else
            {
                Assert.False(result.Rewritten);
                Assert.NotNull(result.Message);
                Assert.Equal(expectedBytes, File.ReadAllBytes(path));
                untouched++;
            }

            AssertNoTempFiles();
        }

        Assert.True(untouched > lines.Length - 5);
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void The_layout_of_the_replaced_baseline_is_kept_so_an_unchanged_baseline_is_a_no_op(string eol, bool bom)
    {
        foreach (var (a, c) in new[] { (OrigA, OrigC), (IndentedA, IndentedC) })
        {
            var original = Fixture(a, OrigB, c);
            var lines = original.Split('\n');
            var lineA = Array.FindIndex(lines, l => l.Trim().StartsWith("AssertMql(")) + 1;
            var lineC = Array.FindLastIndex(lines, l => l.Trim().StartsWith("AssertMql(")) + 1;

            var path = Write(original, eol, bom);
            var before = File.ReadAllBytes(path);

            var rewriter = Create();
            Assert.True(rewriter.Rewrite(path, lineA, ["old a"]).Rewritten);
            Assert.True(rewriter.Rewrite(path, lineC, ["old c", "old c2"]).Rewritten);

            Assert.Equal(before, File.ReadAllBytes(path));
        }
    }

    [Fact]
    public void An_indented_baseline_stays_indented_when_its_content_changes()
    {
        var path = Write(Fixture(IndentedA, OrigB, IndentedC), "\n", false);
        var lines = File.ReadAllLines(path);
        var lineC = Array.FindLastIndex(lines, l => l.Trim().StartsWith("AssertMql(")) + 1;

        Assert.True(Create().Rewrite(path, lineC, ["c1", "c2", "c3"]).Rewritten);

        Assert.Equal(Fixture(IndentedA, OrigB, NewArgs(12, "\n", "c1", "c2", "c3")), File.ReadAllText(path));
    }

    [Fact]
    public void The_same_line_is_only_rewritten_once_per_run()
    {
        var path = Write(Fixture(OrigA, OrigB, OrigC), "\n", false);
        var line = Array.FindIndex(File.ReadAllLines(path), l => l.Trim().StartsWith("AssertMql(")) + 1;
        var rewriter = Create();

        Assert.True(rewriter.Rewrite(path, line, ["first"]).Rewritten);
        var afterFirst = File.ReadAllBytes(path);

        var second = rewriter.Rewrite(path, line, ["second"]);
        Assert.False(second.Rewritten);
        Assert.Null(second.Message);
        Assert.Equal(afterFirst, File.ReadAllBytes(path));
    }

    [Fact]
    public void A_source_file_newer_than_the_test_assembly_is_not_rewritten()
    {
        var path = Write(Fixture(OrigA, OrigB, OrigC), "\r\n", true);
        var before = File.ReadAllBytes(path);
        var line = Array.FindIndex(File.ReadAllLines(path), l => l.Trim().StartsWith("AssertMql(")) + 1;

        var stale = Create(builtUtc: DateTime.UtcNow.AddHours(-1)).Rewrite(path, line, ["x"]);
        Assert.False(stale.Rewritten);
        Assert.Contains("rebuild", stale.Message);
        Assert.Equal(before, File.ReadAllBytes(path));

        Assert.True(Create(builtUtc: DateTime.UtcNow.AddHours(1)).Rewrite(path, line, ["x"]).Rewritten);
    }

    [Fact]
    public void A_file_changed_by_someone_else_between_rewrites_is_not_rewritten_again()
    {
        var original = Fixture(OrigA, OrigB, OrigC);
        var path = Write(original, "\n", false);
        var lines = original.Split('\n');
        var lineA = Array.FindIndex(lines, l => l.Trim().StartsWith("AssertMql(")) + 1;
        var lineC = Array.FindLastIndex(lines, l => l.Trim().StartsWith("AssertMql(")) + 1;
        var rewriter = Create();

        Assert.True(rewriter.Rewrite(path, lineA, ["a"]).Rewritten);

        // Another process / an editor shifts the lines.
        File.WriteAllText(path, "// inserted\n// lines\n" + File.ReadAllText(path));
        var before = File.ReadAllBytes(path);

        var result = rewriter.Rewrite(path, lineC, ["c"]);
        Assert.False(result.Rewritten);
        Assert.Contains("changed by something other than this rewriter", result.Message);
        Assert.Equal(before, File.ReadAllBytes(path));
        AssertNoTempFiles();
    }

    [Fact]
    public void Baseline_text_that_would_break_the_source_is_refused()
    {
        var path = Write(Fixture(OrigA, OrigB, OrigC), "\n", false);
        var before = File.ReadAllBytes(path);
        var line = Array.FindIndex(File.ReadAllLines(path), l => l.Trim().StartsWith("AssertMql(")) + 1;

        // A raw string literal opened with """ ends at the first """ in its content.
        var result = Create().Rewrite(path, line, ["coll.find({ \"a\" : \"\"\" })"]);

        Assert.False(result.Rewritten);
        Assert.Contains("would not parse", result.Message);
        Assert.Equal(before, File.ReadAllBytes(path));
        AssertNoTempFiles();
    }

    [Fact]
    public void A_missing_or_non_utf8_file_is_reported_not_thrown()
    {
        Assert.False(Create().Rewrite(Path.Combine(_dir, "missing.cs"), 5, ["x"]).Rewritten);

        var path = Path.Combine(_dir, "T.cs");
        File.WriteAllBytes(path, [(byte)'/', (byte)'/', 0xE9, 0xE8, 0xFF, 0xFE]);
        var result = Create().Rewrite(path, 1, ["x"]);

        Assert.False(result.Rewritten);
        Assert.Contains("not valid UTF-8", result.Message);
        Assert.Equal(new byte[] { (byte)'/', (byte)'/', 0xE9, 0xE8, 0xFF, 0xFE }, File.ReadAllBytes(path));
    }

    [Fact]
    public void Line_endings_stay_consistent_in_a_crlf_file()
    {
        var original = Fixture(OrigA, OrigB, OrigC);
        var path = Write(original, "\r\n", false);
        var line = Array.FindIndex(original.Split('\n'), l => l.Trim().StartsWith("AssertMql(")) + 1;

        Assert.True(Create().Rewrite(path, line, ["multi\nline\nmql"]).Rewritten);

        Assert.DoesNotMatch(new Regex("(?<!\r)\n"), File.ReadAllText(path));
    }
}
