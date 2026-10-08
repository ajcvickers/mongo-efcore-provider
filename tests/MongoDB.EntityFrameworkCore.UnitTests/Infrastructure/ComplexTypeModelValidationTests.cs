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

using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.UnitTests.Infrastructure;

/// <summary>
/// The provider's model validator adds no complex-type-specific rules: EF Core's own model validation covers
/// the structural ones (no keys or indexes on complex properties, no empty/duplicate/indexer/shadow complex
/// types, required-only on EF8/9), and unsupported complex QUERY shapes decline in the query pipeline, not at
/// model validation. These tests pin that a valid complex model passes <see cref="MongoModelValidator"/>
/// cleanly — including through nested complexes.
/// </summary>
public static class ComplexTypeModelValidationTests
{
    private class Address
    {
        public string City { get; set; } = "";
        public string Zip { get; set; } = "";
    }

    private class Contact
    {
        public Address Address { get; set; } = null!;
        public string Phone { get; set; } = "";
    }

    private class Customer
    {
        public int Id { get; set; }
        public Address Address { get; set; } = null!;
    }

    private class CustomerWithNested
    {
        public int Id { get; set; }
        public Contact Contact { get; set; } = null!;
    }

    [Fact]
    public static void Validate_succeeds_for_a_supported_complex_model()
    {
        using var db = SingleEntityDbContext.Create<Customer>(
            mb => mb.Entity<Customer>().ComplexProperty(c => c.Address));

        Assert.NotNull(db.Model); // Accessing the model runs MongoModelValidator.Validate.
    }

    [Fact]
    public static void Validate_succeeds_for_a_nested_complex_model()
    {
        using var db = SingleEntityDbContext.Create<CustomerWithNested>(
            mb => mb.Entity<CustomerWithNested>().ComplexProperty(
                c => c.Contact,
                contact => contact.ComplexProperty(x => x.Address)));

        Assert.NotNull(db.Model);
    }

    [Fact]
    public static void Complex_property_element_name_annotation_survives_validation()
    {
        using var db = SingleEntityDbContext.Create<Customer>(mb =>
        {
            mb.Entity<Customer>().ComplexProperty(c => c.Address);

            // ComplexPropertyBuilder exposes no HasAnnotation in any EF version we target, so set the
            // annotation on the mutable complex property directly (it survives FinalizeModel).
            mb.Model.FindEntityType(typeof(Customer))!
                .FindComplexProperty(nameof(Customer.Address))!
                .SetAnnotation(MongoAnnotationNames.ElementName, "shipping");
        });

        Assert.NotNull(db.Model);
        var complexProperty = db.Model.FindEntityType(typeof(Customer))!.FindComplexProperty("Address")!;
        Assert.Equal("shipping", complexProperty.GetElementName());
    }
}
