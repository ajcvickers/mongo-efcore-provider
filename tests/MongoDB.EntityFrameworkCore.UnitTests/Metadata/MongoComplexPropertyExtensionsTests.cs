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

namespace MongoDB.EntityFrameworkCore.UnitTests.Metadata;

public static class MongoComplexPropertyExtensionsTests
{
    [Fact]
    public static void Defaults_to_CLR_property_name()
    {
        using var db = new DefaultContext();

        var entityType = db.Model.FindEntityType(typeof(Order));
        Assert.NotNull(entityType);

        var complexProperty = entityType.FindComplexProperty(nameof(Order.Address));
        Assert.NotNull(complexProperty);

        Assert.Equal(nameof(Order.Address), complexProperty.GetElementName());
    }

    [Fact]
    public static void Honors_Mongo_ElementName_annotation()
    {
        using var db = new RenamedContext();

        var entityType = db.Model.FindEntityType(typeof(Order));
        Assert.NotNull(entityType);

        var complexProperty = entityType.FindComplexProperty(nameof(Order.Address));
        Assert.NotNull(complexProperty);

        Assert.Equal("shipping", complexProperty.GetElementName());
    }

    private class Order
    {
        public int Id { get; set; }
        public Address Address { get; set; } = null!;
    }

    private class Address
    {
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
    }

    private class DefaultContext : DbContext
    {
        public DbSet<Order> Orders { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseMongoDB("mongodb://localhost:12345", "unitTests");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Order>().ComplexProperty(o => o.Address);
    }

    private class RenamedContext : DbContext
    {
        public DbSet<Order> Orders { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseMongoDB("mongodb://localhost:12345", "unitTests");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>().ComplexProperty(o => o.Address);

            // ComplexPropertyBuilder exposes no HasAnnotation in any EF version we target, so set the
            // annotation on the mutable complex property directly (it survives FinalizeModel).
            var complexProperty = modelBuilder.Model
                .FindEntityType(typeof(Order))!
                .FindComplexProperty(nameof(Order.Address))!;
            complexProperty.SetAnnotation(MongoAnnotationNames.ElementName, "shipping");
        }
    }
}
