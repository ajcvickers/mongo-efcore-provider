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

// The self-referencing setter renderer rebinds parameters by type because translation discards parameter identity. That is only sound
// while the value body has exactly one parameter of the entity type. This pins the guard: two distinct entity-typed parameters must be
// rejected, not silently merged.

#if !EF8

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Visitors;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query;

public static class BulkSetterRebindingTests
{
    private class Order
    {
        public ObjectId _id { get; set; }
        public int Quantity { get; set; }
    }

    private static IProperty QuantityProperty()
    {
        var model = new ModelBuilder().Model;
        var entityType = model.AddEntityType(typeof(Order));
        return (IProperty)entityType.AddProperty(nameof(Order.Quantity), typeof(int));
    }

    [Fact]
    public static void Rebinds_a_single_entity_parameter_onto_the_target()
    {
        var target = Expression.Parameter(typeof(Order), "e");
        var other = Expression.Parameter(typeof(Order), "o");
        var body = Expression.Add(Expression.Property(other, nameof(Order.Quantity)), Expression.Constant(1));

        var actual = MongoBulkPlanBuilder.RebindSetterValueParameters(body, target, QuantityProperty());

        var rebound = Assert.IsAssignableFrom<BinaryExpression>(actual);
        Assert.Same(target, ((MemberExpression)rebound.Left).Expression);
    }

    [Fact]
    public static void Rebinds_repeated_references_to_the_same_parameter()
    {
        // The same ParameterExpression twice is one parameter, not two: the guard counts distinct parameters, not occurrences.
        var target = Expression.Parameter(typeof(Order), "e");
        var other = Expression.Parameter(typeof(Order), "o");
        var body = Expression.Add(
            Expression.Property(other, nameof(Order.Quantity)), Expression.Property(other, nameof(Order.Quantity)));

        var actual = MongoBulkPlanBuilder.RebindSetterValueParameters(body, target, QuantityProperty());

        var rebound = Assert.IsAssignableFrom<BinaryExpression>(actual);
        Assert.Same(target, ((MemberExpression)rebound.Left).Expression);
        Assert.Same(target, ((MemberExpression)rebound.Right).Expression);
    }

    [Fact]
    public static void Leaves_parameters_of_other_types_alone()
    {
        var target = Expression.Parameter(typeof(Order), "e");
        var other = Expression.Parameter(typeof(Order), "o");
        var unrelated = Expression.Parameter(typeof(int), "i");
        var body = Expression.Add(Expression.Property(other, nameof(Order.Quantity)), unrelated);

        var actual = MongoBulkPlanBuilder.RebindSetterValueParameters(body, target, QuantityProperty());

        var rebound = Assert.IsAssignableFrom<BinaryExpression>(actual);
        Assert.Same(target, ((MemberExpression)rebound.Left).Expression);
        Assert.Same(unrelated, rebound.Right);
    }

    [Fact]
    public static void Throws_when_the_body_has_two_distinct_entity_parameters()
    {
        var target = Expression.Parameter(typeof(Order), "e");
        var first = Expression.Parameter(typeof(Order), "a");
        var second = Expression.Parameter(typeof(Order), "b");
        var body = Expression.Add(
            Expression.Property(first, nameof(Order.Quantity)), Expression.Property(second, nameof(Order.Quantity)));

        var ex = Assert.Throws<InvalidOperationException>(
            () => MongoBulkPlanBuilder.RebindSetterValueParameters(body, target, QuantityProperty()));

        Assert.Contains("more than one", ex.Message);
        Assert.Contains("Order", ex.Message);
        Assert.Contains("Quantity", ex.Message);
    }
}

#endif
