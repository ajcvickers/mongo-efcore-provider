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
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;
using MongoDB.EntityFrameworkCore.UnitTests.TestUtilities;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation;

/// <summary>
/// Member paths through complex properties resolve to dotted field paths for filters and sort keys; a whole
/// complex value (which has no scalar representation) declines loudly rather than falling back to driver-LINQ,
/// which has no complex-type oracle.
/// </summary>
public class ComplexMemberPathTests
{
    private class Address
    {
        public string City { get; set; } = "";
        public string Street { get; set; } = "";
    }

    private class Contact
    {
        public Address Address { get; set; } = new();
    }

    private class Customer
    {
        public ObjectId Id { get; set; }
        public Address Address { get; set; } = new();
        public Contact Contact { get; set; } = new();
    }

    private static MongoExpressionTranslator BuildTranslator(Action<ModelBuilder>? configure = null)
    {
        using var db = SingleEntityDbContext.Create<Customer>(mb => configure?.Invoke(mb));
        var entityType = db.Model.FindEntityType(typeof(Customer))!;
        return new MongoExpressionTranslator(entityType);
    }

    // The complex property renamed to "shipping" — element names must flow into every emitted path.
    private static MongoExpressionTranslator BuildShippingTranslator()
        => BuildTranslator(mb => mb.Entity<Customer>().ComplexProperty(x => x.Address)
            .Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "shipping"));

    // Customer.Contact (complex) containing Customer.Contact.Address (nested complex).
    private static MongoExpressionTranslator BuildNestedTranslator()
        => BuildTranslator(mb => mb.Entity<Customer>()
            .ComplexProperty(x => x.Contact, contact => contact.ComplexProperty(y => y.Address)));

    [Fact]
    public void Filter_on_complex_leaf_translates_to_dotted_path()
    {
        Expression<Func<Customer, bool>> predicate = c => c.Address.City == "Seattle";

        Assert.True(BuildShippingTranslator().TryTranslate(predicate.Body, out var result));
        var binary = Assert.IsType<MongoBinaryExpression>(result);
        Assert.Equal(MongoBinaryOperator.Equal, binary.Operator);
        var field = Assert.IsType<MongoFieldExpression>(binary.Left);
        Assert.Equal("shipping.City", field.ElementName);
        Assert.Equal("City", field.Property.Name);
        Assert.Equal("Seattle", Assert.IsType<MongoConstantExpression>(binary.Right).Value);
    }

    [Fact]
    public void Filter_on_nested_complex_leaf_translates_to_deeper_dotted_path()
    {
        Expression<Func<Customer, bool>> predicate = c => c.Contact.Address.City == "Portland";

        Assert.True(BuildNestedTranslator().TryTranslate(predicate.Body, out var result));
        var field = Assert.IsType<MongoFieldExpression>(Assert.IsType<MongoBinaryExpression>(result).Left);
        Assert.Equal("Contact.Address.City", field.ElementName);
        Assert.Equal("City", field.Property.Name);
    }

    [Fact]
    public void Ordering_by_complex_leaf_translates()
    {
        Expression<Func<Customer, string>> keySelector = c => c.Address.City;

        Assert.True(
            BuildTranslator(mb => mb.Entity<Customer>().ComplexProperty(x => x.Address))
                .TryTranslateField(keySelector.Body, out var field));
        Assert.Equal("Address.City", field.ElementName);
        Assert.Equal("City", field.Property.Name);
    }

    [Fact]
    public void Ordering_by_annotated_complex_leaf_uses_the_element_name()
    {
        Expression<Func<Customer, string>> keySelector = c => c.Address.City;

        Assert.True(BuildShippingTranslator().TryTranslateField(keySelector.Body, out var field));
        Assert.Equal("shipping.City", field.ElementName);
    }

    [Fact]
    public void Filter_on_whole_complex_equality_throws_naming_the_member()
    {
        // A whole complex value has no scalar representation. Driver LINQ cannot evaluate complex members either,
        // so this must throw here (naming the member) rather than fall back to the driver's bare
        // ExpressionNotSupportedException.
        var other = new Address { City = "Portland" };
        Expression<Func<Customer, bool>> predicate = c => c.Address == other;

        var exception = Assert.Throws<InvalidOperationException>(
            () => BuildShippingTranslator().TryTranslate(predicate.Body, out _));
        Assert.Contains("Address", exception.Message);
        Assert.Contains("complex", exception.Message);
    }

    [Fact]
    public void Filter_on_whole_nested_complex_vs_null_throws_naming_the_member()
    {
        Expression<Func<Customer, bool>> predicate = c => c.Contact.Address == null;

        var exception = Assert.Throws<InvalidOperationException>(
            () => BuildNestedTranslator().TryTranslate(predicate.Body, out _));
        Assert.Contains("Address", exception.Message);
        Assert.Contains("complex", exception.Message);
    }
}
