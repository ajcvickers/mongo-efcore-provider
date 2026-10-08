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
/// Stands in for EF's inline construction of one complex property's value inside an injected entity materializer
/// (<c>instance.Address = { new Address(); ...ValueBufferTryReadValue(...)... }</c>), which reads every leaf as if it
/// were a column of the entity's value buffer. <see cref="Visitors.ComplexTypeMaterializationBuilder.MarkComplexPropertyAssignments"/>
/// puts it there; the DOM shaper (<c>MongoProjectionBindingRemovingExpressionVisitor</c>) and the one-pass streaming
/// materializer (<c>MongoStreamingEntityMaterializerRewriter</c>) each replace it with
/// <see cref="Visitors.ComplexTypeMaterializationBuilder.Build"/> over the property's own stored element.
/// </summary>
/// <remarks>
/// Not reducible on purpose: a shaper visitor that doesn't know the node fails to compile loudly rather than reading
/// the leaves off the entity's own document.
/// </remarks>
internal sealed class ComplexPropertyMaterializationExpression(
    IComplexProperty complexProperty, ParameterExpression materializationContext, Type type)
    : Expression, IPrintableExpression
{
    /// <summary>The complex property declared on (or inherited by) the entity type being materialized.</summary>
    public IComplexProperty ComplexProperty { get; } = complexProperty;

    /// <summary>The entity's <c>MaterializationContext</c> variable, which identifies the entity's document.</summary>
    public ParameterExpression MaterializationContext { get; } = materializationContext;

    /// <inheritdoc />
    public override Type Type { get; } = type;

    /// <inheritdoc />
    public override ExpressionType NodeType => ExpressionType.Extension;

    /// <inheritdoc />
    protected override Expression VisitChildren(ExpressionVisitor visitor) => this;

    void IPrintableExpression.Print(ExpressionPrinter expressionPrinter)
        => expressionPrinter.Append($"ComplexPropertyMaterialization({ComplexProperty.DeclaringType.DisplayName()}.{ComplexProperty.Name})");
}
