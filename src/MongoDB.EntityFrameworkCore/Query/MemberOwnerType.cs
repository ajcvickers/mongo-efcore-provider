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

namespace MongoDB.EntityFrameworkCore.Query;

/// <summary>
/// The CLR type a member access's receiver denotes for resolving the member against the model: the receiver with its
/// cast layers (<see cref="ExpressionType.Convert"/>, <see cref="ExpressionType.ConvertChecked"/>,
/// <see cref="ExpressionType.TypeAs"/>) peeled, keeping the MOST DERIVED type seen on the way. A downcast
/// (<c>((Derived)b).Detours</c>, <c>(a as Cat)!.Vet</c>) therefore resolves against the cast type, which declares the
/// member; an upcast or a nullable widening resolves against the operand's (more specific) type, as stripping did before.
/// </summary>
/// <remarks>
/// The one resolver for every walker that looks a member up by its receiver's CLR type (the R20/R25 scanner and the bulk
/// allow-list in <c>ComplexElementNullGuardRefusal</c>, the bridge's EF-337 stored-ordering walkers): stripping the cast
/// looked a derived member up on the base type, found nothing, and let a wrong shape through. Two unrelated types (an
/// interface cast) keep the innermost operand's type, the stripping behaviour.
/// </remarks>
internal static class MemberOwnerType
{
    /// <summary>The CLR type to resolve a member of <paramref name="receiver"/> against.</summary>
    public static Type Of(Expression receiver)
    {
        var type = receiver.Type;
        var current = receiver;
        while (current is UnaryExpression
               {
                   NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs
               } cast)
        {
            current = cast.Operand;
            // A downcast keeps the cast type (the operand's type is a base of it); anything else takes the operand's.
            if (!current.Type.IsAssignableFrom(type) || current.Type == type)
            {
                type = current.Type;
            }
        }

        return type;
    }

    /// <summary>
    /// <paramref name="expression"/> without its cast layers (<see cref="ExpressionType.Convert"/>,
    /// <see cref="ExpressionType.ConvertChecked"/>, <see cref="ExpressionType.TypeAs"/>), for walking a member chain to its
    /// root; resolve the member's owner with <see cref="Of"/>, never with the stripped expression's type.
    /// </summary>
    public static Expression StripCasts(Expression expression)
    {
        while (expression is UnaryExpression
               {
                   NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs
               } cast)
        {
            expression = cast.Operand;
        }

        return expression;
    }

    /// <summary>
    /// Whether <paramref name="receiver"/> is a (possibly multi-layer) downcast of its innermost operand: the type
    /// <see cref="Of"/> answers is a strict subtype of the operand's.
    /// </summary>
    public static bool IsDowncast(Expression receiver, out Expression operand, out Type castType)
    {
        castType = Of(receiver);
        operand = StripCasts(receiver);

        return castType != operand.Type && operand.Type.IsAssignableFrom(castType);
    }
}
