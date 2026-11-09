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
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Update;

/// <summary>
/// EF-X703: when the discriminator is part of the composite primary key it is stored inside <c>_id</c>
/// (<c>{ _id: { Id1, Id2, _t } }</c>), so the discriminator filter of a derived-type query must target <c>_id._t</c>.
/// </summary>
public class CompositeKeyDiscriminatorTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    [Fact]
    public void Derived_type_with_discriminator_in_composite_key_can_be_queried_back()
    {
        var collection = database.CreateCollection<Flyer>();
        var ticks = DateTime.UtcNow.Ticks;

        {
            using var db = CreateContext(collection);
            db.Entities.Add(new Pegasus { Id1 = ticks, Id2 = ticks + 1, Name = "Rainbow Dash" });
            db.SaveChanges();
        }

        {
            using var db = CreateContext(collection);
            var pegasus = db.Entities.OfType<Pegasus>().Single(e => e.Id1 == ticks && e.Id2 == ticks + 1);
            Assert.Equal("Rainbow Dash", pegasus.Name);
            Assert.Equal(1, db.Entities.OfType<Pegasus>().Count());
        }
    }

    private static SingleEntityDbContext<Flyer> CreateContext(IMongoCollection<Flyer> collection)
        => SingleEntityDbContext.Create<Flyer, Flyer>(collection, b =>
        {
            b.Entity<Flyer>().HasKey(e => new { e.Id1, e.Id2, e.Discriminator });
            b.Entity<Pegasus>();
        });

    private abstract class Flyer
    {
        public string Discriminator { get; set; } = null!;
        public long Id1 { get; set; }
        public long Id2 { get; set; }
    }

    private class Pegasus : Flyer
    {
        public string? Name { get; set; }
    }
}
