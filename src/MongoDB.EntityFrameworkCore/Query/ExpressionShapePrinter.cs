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

namespace MongoDB.EntityFrameworkCore.Query;

/// <summary>
/// Prints the SHAPE of an expression for an exception message: member names, operators and node kinds, with every
/// inlined constant value replaced by <c>?</c>, so a literal from user code (<c>s.City == "secret"</c>) never lands in
/// exception text regardless of <c>EnableSensitiveDataLogging</c>.
/// </summary>
/// <remarks>
/// <see langword="null"/> constants, <see cref="Type"/> constants, compiler-generated closure objects (which print as
/// <c>value(Closure)</c>, never their captured values) and the member name of <c>EF.Property</c> are kept, so the shape
/// stays informative.
/// </remarks>
internal static class ExpressionShapePrinter
{
    /// <summary>The printed shape of <paramref name="expression"/>.</summary>
    public static string Print(Expression expression)
    {
        try
        {
            return new Redactor().Visit(expression)!.ToString();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            // A node that can't be rebuilt with a placeholder operand: say only what kind of node it is.
            return $"<{expression.NodeType} expression>";
        }
    }

    private sealed class Redactor : ExpressionVisitor
    {
        protected override Expression VisitConstant(ConstantExpression node)
            => node.Value is null or Type || IsClosure(node.Value.GetType())
                ? node
                : Expression.Parameter(node.Type, "?");

        // `EF.Property(x, "Name")`: the member name is part of the shape, not data.
        protected override Expression VisitMethodCall(MethodCallExpression node)
            => node.Method.IsEFPropertyMethod()
                ? node.Update(node.Object, [Visit(node.Arguments[0]), node.Arguments[1]])
                : base.VisitMethodCall(node);

        // Query roots, query parameters and the like print safely on their own (a parameter prints as its name); they are
        // not rebuilt, so a node whose VisitChildren isn't allowed is never walked.
        protected override Expression VisitExtension(Expression node)
            => node;

        private static bool IsClosure(Type type)
            => Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute));
    }
}
