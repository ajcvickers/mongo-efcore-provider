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
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;

namespace MongoDB.EntityFrameworkCore.Query;

/// <summary>
/// Prints the SHAPE of an expression for an exception message: member names, operators and node kinds, with every
/// inlined constant value replaced by <c>?</c>, so a literal from user code (<c>s.City == "secret"</c>) never lands in
/// exception text regardless of <c>EnableSensitiveDataLogging</c>.
/// </summary>
/// <remarks>
/// <see langword="null"/> constants, <see cref="Type"/> constants, C# closure display-class objects (which print as
/// <c>value(Closure)</c>, never their captured values; anonymous-type instances are redacted like any literal) and the member
/// name of <c>EF.Property</c> are kept, so the shape stays informative.
/// </remarks>
internal static class ExpressionShapePrinter
{
    /// <summary>The printed shape of <paramref name="expression"/>.</summary>
    public static string Print(Expression expression)
    {
        try
        {
            return new Redactor(efFormat: false).Visit(expression)!.ToString();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            // A node that can't be rebuilt with a placeholder operand: say only what kind of node it is.
            return $"<{expression.NodeType} expression>";
        }
    }

    /// <summary>
    /// <paramref name="expression"/> in EF Core's query print format (<c>ExpressionPrinter</c>, as
    /// <c>CoreStrings.TranslationFailed</c>/<c>NonQueryTranslationFailedWithDetails</c> print it) with every inlined
    /// literal replaced by <c>?</c>: for the captured query an exception message wraps, which may hold user literals in a
    /// filter or a setter value. Query roots and query parameters print as EF prints them.
    /// </summary>
    public static string? PrintQuery(Expression? expression)
    {
        if (expression is null)
        {
            return null;
        }

        try
        {
            return new Redactor(efFormat: true).Visit(expression)!.Print();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return $"<{expression.NodeType} expression>";
        }
    }

    private sealed class Redactor(bool efFormat) : ExpressionVisitor
    {
        // A closure prints as `value(DisplayClass).member` (never its values); anything else, an anonymous-type instance
        // included (also [CompilerGenerated], but printed with its values), is a literal and is redacted.
        protected override Expression VisitConstant(ConstantExpression node)
            => node.Value is null or Type || IsClosure(node.Value.GetType())
                ? node
                : Expression.Parameter(node.Type, "?");

        // EF's printer prints a captured closure member's VALUE (`value(Closure).secret` becomes the value); the shape
        // printer's ToString prints the member path. In EF format, redact a member read off a closure too.
        protected override Expression VisitMember(MemberExpression node)
            => efFormat && node.Expression is ConstantExpression { Value: { } owner } && IsClosure(owner.GetType())
                ? Expression.Parameter(node.Type, "?")
                : base.VisitMember(node);

        // `EF.Property(x, "Name")`: the member name is part of the shape, not data.
        protected override Expression VisitMethodCall(MethodCallExpression node)
            => node.Method.IsEFPropertyMethod()
                ? node.Update(node.Object, [Visit(node.Arguments[0]), node.Arguments[1]])
                : base.VisitMethodCall(node);

        // Query roots, query parameters and the like print safely on their own (a parameter prints as its name); they are
        // not rebuilt, so a node whose VisitChildren isn't allowed is never walked.
        protected override Expression VisitExtension(Expression node)
            => node;

        // Only C# closure display classes (`<>c__DisplayClass...`), not every [CompilerGenerated] type: anonymous types
        // (`<>f__AnonymousType...`) print their member values.
        private static bool IsClosure(Type type)
            => Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute))
               && type.Name.Contains("<>c__DisplayClass", StringComparison.Ordinal);
    }
}
