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

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Optimistic concurrency with complex properties. A concurrency token or row version on a member of a complex type is
/// rejected by the model validator: before that check, EF accepted it on EF8, EF9 and EF10 and the provider ignored it
/// (the update filter holds only the entity's own tokens, and no row version was generated), so a conflicting save
/// silently overwrote the other change. A token on the document root guards the whole document, complex properties
/// included. (Bulk ExecuteUpdate/ExecuteDelete bypass concurrency tokens by design, as for every entity.)
/// </summary>
[XUnitCollection("UpdateTests")]
public class ComplexTypeConcurrencyTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class VAddr
    {
        public string City { get; set; } = null!;
        public long Ver { get; set; }
    }

    public class VCustomer
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public long Version { get; set; }
        public VAddr Home { get; set; } = null!;
    }

    private IMongoCollection<VCustomer> Collection([CallerMemberName] string name = "")
        => database.CreateCollection<VCustomer>(TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8]);

    private static SingleEntityDbContext<VCustomer> Context(IMongoCollection<VCustomer> collection, Action<ModelBuilder> configure)
        => SingleEntityDbContext.Create(collection, configure, null,
            b => b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));

    [Fact]
    public void Concurrency_token_on_a_complex_leaf_is_rejected_by_model_validation()
    {
        using var db = Context(Collection(), mb =>
            mb.Entity<VCustomer>(e =>
            {
                e.Ignore(c => c.Version);
                e.ComplexProperty(c => c.Home).Property(a => a.Ver).IsConcurrencyToken();
            }));

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains("'Ver'", ex.Message);
        Assert.Contains("belongs to a complex type", ex.Message);
    }

    [Fact]
    public void Row_version_on_a_complex_leaf_is_rejected_by_model_validation()
    {
        using var db = Context(Collection(), mb =>
            mb.Entity<VCustomer>(e =>
            {
                e.Ignore(c => c.Version);
                e.ComplexProperty(c => c.Home).Property(a => a.Ver).IsRowVersion();
            }));

        var ex = Assert.Throws<NotSupportedException>(() => db.Model);
        Assert.Contains("'Ver'", ex.Message);
    }

    [Fact]
    public void Root_row_version_guards_a_change_to_a_complex_property()
    {
        var collection = Collection();
        Action<ModelBuilder> configure = mb => mb.Entity<VCustomer>(e =>
        {
            e.Property(c => c.Version).IsRowVersion();
            e.ComplexProperty(c => c.Home);
        });

        using (var db = Context(collection, configure))
        {
            db.Entities.Add(new VCustomer { Name = "a", Home = new VAddr { City = "X", Ver = 1 } });
            db.SaveChanges();
        }

        using var first = Context(collection, configure);
        using var second = Context(collection, configure);
        var one = first.Entities.Single();
        var two = second.Entities.Single();

        one.Home.City = "A";
        first.SaveChanges();

        two.Home.City = "B";
        Assert.Throws<DbUpdateConcurrencyException>(() => second.SaveChanges());

        var stored = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName).Find("{}").Single();
        Assert.Equal("A", stored["Home"]["City"].AsString);
    }
}
