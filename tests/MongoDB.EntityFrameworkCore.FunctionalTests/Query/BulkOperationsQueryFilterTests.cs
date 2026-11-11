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

// Global query filters are how soft-delete and multi-tenancy are expressed in this provider. A bulk operation that ignored one would
// delete or overwrite documents the application believes are invisible, so this is covered on both execution strategies.

#if !EF8

using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

public class BulkOperationsQueryFilterTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Doc
    {
        public ObjectId _id { get; set; }
        public string Tenant { get; set; } = null!;
        public bool Deleted { get; set; }
        public int Quantity { get; set; }
    }

    private static void SoftDelete(ModelBuilder mb)
        => mb.Entity<Doc>().HasQueryFilter(d => !d.Deleted);

    private static void SingleTenant(ModelBuilder mb)
        => mb.Entity<Doc>().HasQueryFilter(d => d.Tenant == "a");

    // Two tenant-a documents (one soft-deleted) and one tenant-b document.
    private IMongoCollection<Doc> Seed(string name)
    {
        var collection = database.CreateCollection<Doc>(name);
        collection.InsertMany(
        [
            new Doc { _id = ObjectId.GenerateNewId(), Tenant = "a", Deleted = false, Quantity = 1 },
            new Doc { _id = ObjectId.GenerateNewId(), Tenant = "a", Deleted = true, Quantity = 2 },
            new Doc { _id = ObjectId.GenerateNewId(), Tenant = "b", Deleted = false, Quantity = 3 }
        ]);
        return collection;
    }

    [Fact]
    public void Single_command_delete_honours_the_soft_delete_filter()
    {
        var collection = Seed(nameof(Single_command_delete_honours_the_soft_delete_filter));
        using var db = SingleEntityDbContext.Create(collection, SoftDelete);

        var deleted = db.Entities.ExecuteDelete();

        Assert.Equal(2, deleted);
        var remaining = collection.Find(FilterDefinition<Doc>.Empty).Single();
        Assert.True(remaining.Deleted);
    }

    [Fact]
    public void Single_command_delete_honours_the_tenant_filter_combined_with_a_user_predicate()
    {
        var collection = Seed(nameof(Single_command_delete_honours_the_tenant_filter_combined_with_a_user_predicate));
        using var db = SingleEntityDbContext.Create(collection, SingleTenant);

        var deleted = db.Entities.Where(d => d.Quantity >= 2).ExecuteDelete();

        // Quantity >= 2 alone would match the soft-deleted tenant-a document AND the tenant-b one; the filter excludes tenant b.
        Assert.Equal(1, deleted);
        Assert.Equal(2, collection.CountDocuments(FilterDefinition<Doc>.Empty));
        Assert.Equal(1, collection.CountDocuments(Builders<Doc>.Filter.Eq(d => d.Tenant, "b")));
    }

    [Fact]
    public void Two_phase_delete_honours_the_tenant_filter()
    {
        var collection = Seed(nameof(Two_phase_delete_honours_the_tenant_filter));
        using var db = SingleEntityDbContext.Create(collection, SingleTenant);

        var deleted = db.Entities.OrderBy(d => d.Quantity).Take(5).ExecuteDelete();

        Assert.Equal(2, deleted);
        Assert.Equal("b", collection.Find(FilterDefinition<Doc>.Empty).Single().Tenant);
    }

    [Fact]
    public void Single_command_update_honours_the_tenant_filter()
    {
        var collection = Seed(nameof(Single_command_update_honours_the_tenant_filter));
        using var db = SingleEntityDbContext.Create(collection, SingleTenant);

        var updated = db.Entities.ExecuteUpdate(s => s.SetProperty(d => d.Quantity, 99));

        Assert.Equal(2, updated);
        Assert.Equal(3, collection.Find(Builders<Doc>.Filter.Eq(d => d.Tenant, "b")).Single().Quantity);
    }

    [Fact]
    public void Two_phase_update_honours_the_tenant_filter()
    {
        var collection = Seed(nameof(Two_phase_update_honours_the_tenant_filter));
        using var db = SingleEntityDbContext.Create(collection, SingleTenant);

        var updated = db.Entities.OrderBy(d => d.Quantity).Take(5).ExecuteUpdate(s => s.SetProperty(d => d.Quantity, 99));

        Assert.Equal(2, updated);
        Assert.Equal(3, collection.Find(Builders<Doc>.Filter.Eq(d => d.Tenant, "b")).Single().Quantity);
    }

    [Fact]
    public void IgnoreQueryFilters_widens_the_scope_as_EF_intends()
    {
        var collection = Seed(nameof(IgnoreQueryFilters_widens_the_scope_as_EF_intends));
        using var db = SingleEntityDbContext.Create(collection, SingleTenant);

        var deleted = db.Entities.IgnoreQueryFilters().ExecuteDelete();

        // The tenant filter alone would narrow this to 2; a silent no-op of IgnoreQueryFilters would yield 2.
        Assert.Equal(3, deleted);
    }
}

#endif
