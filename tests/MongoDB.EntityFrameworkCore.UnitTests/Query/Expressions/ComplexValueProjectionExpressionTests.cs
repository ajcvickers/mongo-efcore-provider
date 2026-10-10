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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.Expressions;

/// <summary>
/// <see cref="ComplexValueProjectionExpression.IsContainedIn"/>: the walker that sets the complex-value refusal flag after
/// every projection arm. It must find the node through standard nodes and the provider's shaper wrappers, and must never
/// throw on (or walk into) other extension nodes, which every Select arm of every query passes it.
/// </summary>
public class ComplexValueProjectionExpressionTests
{
    class Address
    {
        public string City { get; set; } = null!;
    }

    class Customer
    {
        public ObjectId Id { get; set; }
        public Address Address { get; set; } = null!;
    }

    class Db : DbContext
    {
        public DbSet<Customer> Customers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Customer>().ComplexProperty(c => c.Address);

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
    }

    // A non-reducible extension node of some other visitor whose VisitChildren throws (as ShapedQueryExpression's does).
    private sealed class ForeignNode(Expression child) : Expression
    {
        public Expression Child { get; } = child;
        public override Type Type => Child.Type;
        public override ExpressionType NodeType => ExpressionType.Extension;
        protected override Expression VisitChildren(ExpressionVisitor visitor)
            => throw new InvalidOperationException("must not be walked");
    }

    private static ComplexValueProjectionExpression Node()
    {
        using var db = new Db();
        var entityType = db.Model.FindEntityType(typeof(Customer))!;
        var queryExpression = new MongoQueryExpression(entityType);
        return new ComplexValueProjectionExpression(
            new ProjectionBindingExpression(queryExpression, new ProjectionMember(), typeof(Address)),
            entityType.FindComplexProperty(nameof(Customer.Address))!,
            Expression.Default(typeof(Address)));
    }

    [Fact]
    public void Found_directly_and_through_standard_nodes()
    {
        var node = Node();
        Assert.True(ComplexValueProjectionExpression.IsContainedIn(node));
        Assert.True(ComplexValueProjectionExpression.IsContainedIn(
            Expression.Condition(Expression.Constant(true), Expression.Convert(node, typeof(object)), Expression.Constant(null))));
    }

    [Fact]
    public void Unknown_extension_node_is_a_leaf_and_is_never_walked()
    {
        // Doesn't throw (the walker never calls its VisitChildren) and reports nothing inside it: only the projection binder
        // creates the node, inside standard nodes or its own shaper wrappers.
        var foreign = new ForeignNode(Node());
        Assert.False(ComplexValueProjectionExpression.IsContainedIn(foreign));
        Assert.False(ComplexValueProjectionExpression.IsContainedIn(Expression.Convert(foreign, typeof(object))));
    }

    [Fact]
    public void A_shaper_without_the_node_reports_nothing()
        => Assert.False(ComplexValueProjectionExpression.IsContainedIn(Expression.Constant(1)));
}
