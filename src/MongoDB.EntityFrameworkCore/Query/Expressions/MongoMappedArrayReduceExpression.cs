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

namespace MongoDB.EntityFrameworkCore.Query.Expressions;

/// <summary>
/// <c>{ &lt;Operator&gt;: { $map: { input: "$array", as: "e", in: &lt;element expression&gt; } } }</c>: <c>$sum</c>/<c>$min</c>/<c>$max</c>/<c>$avg</c>
/// of a COMPUTED value per element of an array field (<c>c.Orders.Sum(o =&gt; o.Price * o.Qty)</c>).
/// </summary>
/// <remarks>
/// A sibling of <see cref="MongoArrayReduceExpression"/>, which reduces a plain array of values (the path through the array to one
/// property), not a per-element expression. <see cref="ElementExpression"/> is element-relative, like the predicate of
/// <see cref="MongoFilteredSizeExpression"/>, so a field prefix rewrite moves only <see cref="ArrayPath"/>. The array is wrapped in
/// <c>$ifNull</c>: <c>$map</c> over a missing or null array is a server error. An empty array reduces to <c>0</c> for <c>$sum</c> and
/// <c>null</c> for the others, as LINQ answers for a nullable result.
/// <para>
/// <see cref="MinByOperator"/> and <see cref="MaxByOperator"/> are not MQL operators: they select the element whose
/// <see cref="ElementExpression"/> (the key) is the smallest or largest, the first of them on a tie as <c>MinBy</c>/<c>MaxBy</c> answer,
/// and the value is <see cref="ResultExpression"/> (element-relative) of that element. An empty array answers <c>null</c>.
/// </para>
/// </remarks>
internal sealed class MongoMappedArrayReduceExpression : MongoExpression
{
    /// <summary>The <see cref="OperatorName"/> of <c>MinBy(key).Member</c>: the member of the element with the smallest key.</summary>
    public const string MinByOperator = "$minBy";

    /// <summary>The <see cref="OperatorName"/> of <c>MaxBy(key).Member</c>: the member of the element with the largest key.</summary>
    public const string MaxByOperator = "$maxBy";

    public MongoMappedArrayReduceExpression(
        string operatorName, string arrayPath, MongoExpression elementExpression, Type type, MongoExpression? resultExpression = null)
    {
        OperatorName = operatorName;
        ArrayPath = arrayPath;
        ElementExpression = elementExpression;
        Type = type;
        ResultExpression = resultExpression;
    }

    /// <summary>Whether this selects an element by its key (<see cref="MinByOperator"/>, <see cref="MaxByOperator"/>) rather than reducing the per-element values.</summary>
    public bool IsElementSelection => OperatorName is MinByOperator or MaxByOperator;

    /// <summary>For an element selection, the element-relative value read from the selected element; otherwise <see langword="null"/>.</summary>
    public MongoExpression? ResultExpression { get; }

    /// <summary>The MQL array-reduce operator: <c>"$avg"</c>, <c>"$max"</c>, <c>"$min"</c>, or <c>"$sum"</c>.</summary>
    public string OperatorName { get; }

    /// <summary>The array's dotted document path, relative to the enclosing scope.</summary>
    public string ArrayPath { get; }

    /// <summary>The per-element value, with element-relative field paths.</summary>
    public MongoExpression ElementExpression { get; }

    /// <inheritdoc />
    public override Type Type { get; }
}
