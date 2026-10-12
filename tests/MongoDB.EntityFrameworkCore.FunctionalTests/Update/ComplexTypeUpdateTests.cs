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

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Update;

/// <summary>
/// Complex-property update-pipeline behavior: a modified complex property is written as a whole
/// sub-document $set (it is a single CLR value on the entry), matching how owned navigations are rewritten.
/// </summary>
[XUnitCollection("UpdateTests")]
public class ComplexTypeUpdateTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    public class Address
    {
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
    }

    public class Customer
    {
        public int Id { get; set; }
        public Address Address { get; set; } = null!;
    }

#if EF10
    public class NullableCustomer
    {
        public int Id { get; set; }
        public Address? Address { get; set; }
    }
#endif

    [Fact]
    public void Update_modifies_complex_property_in_place()
    {
        var collection = database.CreateCollection<Customer>();

        // Same-context: the load phase (fresh-context materialization of complex properties) is Task 4's
        // acceptance test; until it lands the update pipeline is exercised on the tracked instance.
        using (var db = SingleEntityDbContext.Create(
                   collection,
                   modelBuilder => modelBuilder.Entity<Customer>().ComplexProperty(c => c.Address)))
        {
            db.Entities.Add(new Customer { Id = 1, Address = new Address { City = "Seattle", Street = "1 Main" } });
            db.SaveChanges();

            // Find resolves the tracked instance without a database query (no materialization involved).
            db.Entities.Find(1).Address = new Address { City = "Portland", Street = "9 SE" };
            db.SaveChanges();
        }

        var actual = collection
            .Find(Builders<Customer>.Filter.Empty)
            .Project(Builders<Customer>.Projection.As<BsonDocument>())
            .Single();

        Assert.Equal(
            BsonDocument.Parse("{ _id : 1, Address: { City: 'Portland', Street: '9 SE' } }"),
            actual);
    }

#if EF10
    [Fact]
    public void Nullable_complex_set_to_null_writes_null_element()
    {
        var collection = database.CreateCollection<NullableCustomer>();

        using (var db = SingleEntityDbContext.Create(
                   collection,
                   modelBuilder => modelBuilder.Entity<NullableCustomer>().ComplexProperty(c => c.Address)))
        {
            db.Entities.Add(new NullableCustomer { Id = 1, Address = null });
            db.SaveChanges();
        }

        var actual = collection
            .Find(Builders<NullableCustomer>.Filter.Empty)
            .Project(Builders<NullableCustomer>.Projection.As<BsonDocument>())
            .Single();

        Assert.True(actual["Address"].IsBsonNull, $"Expected Address to be null but was {actual["Address"]}");
    }
#endif
}
