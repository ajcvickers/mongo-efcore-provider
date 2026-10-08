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
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace MongoDB.EntityFrameworkCore.Query.Expressions;

/// <summary>
/// A projected whole complex value (<c>c.Address</c>, <c>c.Address.Location</c>, <c>EF.Property&lt;A&gt;(c, "Address")</c>,
/// a complex collection) in a shaper: <see cref="Binding"/> is the projection member it was bound to, and the shaper
/// materializes it with <see cref="Visitors.ComplexTypeMaterializationBuilder.Build"/>, never through a serializer.
/// </summary>
/// <remarks>
/// Created by <c>MongoProjectionBindingExpressionVisitor</c> for every complex-valued leaf. Being an unknown node to
/// <c>ProjectionAnalyzer.CanPushDown</c>, it keeps such a projection off the driver-LINQ typed push-down (whose
/// <c>ComplexTypeSerializer</c> can't deserialize), so a non-native run uses the mixed shaper over whole documents.
/// </remarks>
internal sealed class ComplexValueProjectionExpression(ProjectionBindingExpression binding, IComplexProperty complexProperty)
    : Expression, IPrintableExpression
{
    /// <summary>The projection member the value was bound to.</summary>
    public ProjectionBindingExpression Binding { get; } = binding;

    /// <summary>The projected complex property.</summary>
    public IComplexProperty ComplexProperty { get; } = complexProperty;

    /// <inheritdoc />
    public override Type Type => Binding.Type;

    /// <inheritdoc />
    public override ExpressionType NodeType => ExpressionType.Extension;

    /// <inheritdoc />
    protected override Expression VisitChildren(ExpressionVisitor visitor) => this;

    void IPrintableExpression.Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.Append($"ComplexValue({ComplexProperty.DeclaringType.DisplayName()}.{ComplexProperty.Name}, ");
        expressionPrinter.Visit(Binding);
        expressionPrinter.Append(")");
    }
}
