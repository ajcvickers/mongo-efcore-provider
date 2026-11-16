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
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace MongoDB.EntityFrameworkCore.Query.Expressions;

/// <summary>
/// An element <c>Select</c> over a root-declared owned collection whose selector only reads mapped scalar members of the element
/// (<c>e.Kids.Select(k =&gt; k.W).ToList()</c>, <see cref="NativeTranslation.ElementSelectorKind.ScalarMembersOnly"/>): the selector is evaluated
/// over the stored element documents, each member read through its property's serializer, and yields the <see cref="List{T}"/> of its results.
/// </summary>
/// <remarks>
/// <para>
/// No owned entity is constructed, resolved against the change tracker or tracked, so the result is the stored values whatever the query's
/// tracking behavior, and a property getter or constructor of the element type is not run. That is why the node holds no element entity shaper:
/// EF's materializer injector refuses a tracking query that projects an owned entity shaper without its owner, before any provider code runs.
/// </para>
/// <para>
/// <see cref="ArrayProjection"/> is how the shaper reaches the stored array: on the native projection route the
/// <see cref="ProjectionBindingExpression"/> of the array leaf the emit side staged (<c>NativeProjectionBinder.TryMatchOwnedArraySelectLeaf</c>), on
/// a route that reads whole documents the navigation's <see cref="IArrayProjectionExpression"/>. The DOM shapers lower the node in
/// <see cref="Visitors.MongoProjectionBindingRemovingExpressionVisitor"/>; the streaming one fuses it in <c>StreamedCollectionSubstituter</c>.
/// </para>
/// </remarks>
internal sealed class OwnedElementStoredSelectExpression : Expression, IPrintableExpression
{
    public OwnedElementStoredSelectExpression(Expression arrayProjection, INavigation navigation, LambdaExpression selector)
    {
        ArrayProjection = arrayProjection;
        Navigation = navigation;
        Selector = selector;
        Type = typeof(List<>).MakeGenericType(selector.ReturnType);
    }

    /// <summary>The stored array: a projection binding of the array leaf, or the array's own projection expression.</summary>
    public Expression ArrayProjection { get; }

    /// <summary>The owned collection navigation the array holds.</summary>
    public INavigation Navigation { get; }

    /// <summary>The element selector, over the owned element type, whose every use of the element is the read of a mapped scalar property.</summary>
    public LambdaExpression Selector { get; }

    public override ExpressionType NodeType
        => ExpressionType.Extension;

    public override Type Type { get; }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
        => Update(visitor.Visit(ArrayProjection));

    public OwnedElementStoredSelectExpression Update(Expression arrayProjection)
        => arrayProjection == ArrayProjection ? this : new OwnedElementStoredSelectExpression(arrayProjection, Navigation, Selector);

    void IPrintableExpression.Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.Append("StoredSelect(");
        expressionPrinter.Visit(ArrayProjection);
        expressionPrinter.Append(", ");
        expressionPrinter.Visit(Selector);
        expressionPrinter.Append(")");
    }

    public override bool Equals(object? obj)
        => obj != null
           && (ReferenceEquals(this, obj)
               || obj is OwnedElementStoredSelectExpression other
               && ArrayProjection.Equals(other.ArrayProjection)
               && Navigation == other.Navigation
               && ExpressionEqualityComparer.Instance.Equals(Selector, other.Selector));

    public override int GetHashCode()
        => HashCode.Combine(ArrayProjection, Navigation, ExpressionEqualityComparer.Instance.GetHashCode(Selector));
}
