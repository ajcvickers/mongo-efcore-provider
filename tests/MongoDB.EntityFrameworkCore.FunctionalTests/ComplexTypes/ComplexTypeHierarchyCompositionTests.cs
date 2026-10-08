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

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.CompositionAssert;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Complex properties composed with TPH hierarchies (complex property on the base and on a derived type), shared-type
/// entity types, and owned types (owned → complex chains, complex values inside owned collection elements). Every shape is
/// pinned per mode against a hand-written answer (<see cref="CompositionAssert"/>).
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexTypeHierarchyCompositionTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    private DbContextOptions Options<TContext>(MongoQueryMode mode) where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
        return builder.Options;
    }

    private string Collection(string name, params BsonDocument[] documents)
    {
        var collection = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
        database.MongoDatabase.GetCollection<BsonDocument>(collection).InsertMany(documents);
        return collection;
    }

    private static BsonDocument Geo(double lat, double lon) => new() { { "Lat", lat }, { "Lon", lon } };

    // ── TPH: complex property on the base type and on a derived type ───────────────────────────────────────────

    public class Animal
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Composition.Addr Home { get; set; } = null!;
    }

    public class Dog : Animal
    {
        public GeoPoint Kennel { get; set; }
        public int Age { get; set; }
    }

    public class Cat : Animal
    {
        public Composition.Addr Vet { get; set; } = null!;
    }

    private sealed class ZooContext(DbContextOptions options, string collection) : DbContext(options)
    {
        public DbSet<Animal> Animals => Set<Animal>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Animal>(b =>
            {
                b.ToCollection(collection);
                b.HasDiscriminator<string>("_t").HasValue<Animal>("A").HasValue<Dog>("D").HasValue<Cat>("C");
                b.ComplexProperty(a => a.Home, h => h.ComplexProperty(x => x.Geo));
            });
            mb.Entity<Dog>().ComplexProperty(d => d.Kennel);
            // Cat.Vet has the base's complex CLR type under a different element name.
            mb.Entity<Cat>().ComplexProperty(c => c.Vet, v =>
            {
                v.HasPropertyAnnotation(MongoDB.EntityFrameworkCore.Metadata.MongoAnnotationNames.ElementName, "vet");
                v.ComplexProperty(x => x.Geo);
            });
        }
    }

    private static BsonDocument Addr(string city, double lat)
        => new() { { "Street", "s" }, { "City", city }, { "Geo", Geo(lat, lat) }, { "Code", 1 } };

    // Generic (A) Paris; Rex (D, Kennel 2/20, Age 3) Rome; Fido (D, Kennel 5/50, Age 7) Paris; Tom (C, vet Oslo) Rome.
    // Generic's document also holds a decoy `Kennel` (9/9): only Dogs map it.
    private Func<MongoQueryMode, List<string>> Zoo(Func<ZooContext, IEnumerable<string>> query,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = Collection(name,
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "_t", "A" }, { "Name", "Generic" }, { "Home", Addr("Paris", 1) }, { "Kennel", Geo(9, 9) } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "_t", "D" }, { "Name", "Rex" }, { "Home", Addr("Rome", 2) }, { "Kennel", Geo(2, 20) }, { "Age", 3 } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "_t", "D" }, { "Name", "Fido" }, { "Home", Addr("Paris", 3) }, { "Kennel", Geo(5, 50) }, { "Age", 7 } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "_t", "C" }, { "Name", "Tom" }, { "Home", Addr("Rome", 4) }, { "vet", Addr("Oslo", 8) } });
        return mode =>
        {
            using var db = new ZooContext(Options<ZooContext>(mode), collection);
            return query(db).ToList();
        };
    }

    private static string F(Animal a) => a switch
    {
        Dog d => $"D:{d.Name}|{d.Home.City}|{d.Kennel.Lat}|{d.Age}",
        Cat c => $"C:{c.Name}|{c.Home.City}|{c.Vet.City}",
        _ => $"A:{a.Name}|{a.Home.City}"
    };

    [Fact]
    public void Filtering_the_hierarchy_by_a_base_complex_leaf_materializes_every_derived_type()
        => Native(Zoo(db => [.. db.Animals.Where(a => a.Home.City == "Rome").OrderBy(a => a.Name).ToList().Select(F)]),
            "D:Rex|Rome|2|3", "C:Tom|Rome|Oslo");

    [Fact]
    public void OfType_then_a_derived_only_complex_leaf_predicate_and_ordering()
    {
        // Members declared only on the derived type decline natively after OfType (pre-existing, a scalar `d.Age > 1` too:
        // the translator is scoped to the collection's root type); the fallback serves.
        Declines(Zoo(db => [.. db.Animals.OfType<Dog>().Where(d => d.Kennel.Lat > 3).ToList().Select(F)]), "D:Fido|Paris|5|7");
        Declines(Zoo(db => [.. db.Animals.OfType<Dog>().OrderByDescending(d => d.Kennel.Lon).ToList().Select(F)]), "D:Fido|Paris|5|7", "D:Rex|Rome|2|3");
        Declines(Zoo(db => [.. db.Animals.OfType<Cat>().Where(c => c.Vet.City == "Oslo" && c.Home.City == "Rome").ToList().Select(F)]), "C:Tom|Rome|Oslo");
        Declines(Zoo(db => [db.Animals.OfType<Dog>().Count(d => d.Kennel.Lon < 30).ToString()]), "1");
        // A base-declared complex leaf after OfType stays native.
        Native(Zoo(db => [.. db.Animals.OfType<Dog>().Where(d => d.Home.City == "Paris").ToList().Select(F)]), "D:Fido|Paris|5|7");
    }

    [Fact]
    public void Type_test_with_a_derived_only_complex_leaf_over_the_whole_hierarchy()
        // Generic's document carries a decoy Kennel (Lat 9): it must not match, because Generic is not a Dog.
        // Declines natively, like the scalar `((Dog)a).Age` (pre-existing).
        => Declines(Zoo(db => [.. db.Animals.Where(a => a is Dog && ((Dog)a).Kennel.Lat > 1).OrderBy(a => a.Name).ToList().Select(F)]),
            "D:Fido|Paris|5|7", "D:Rex|Rome|2|3");

    [Fact]
    public void Projecting_after_OfType()
    {
        // A base-declared complex leaf after OfType stays native.
        Native(Zoo(db => [.. db.Animals.OfType<Dog>().OrderBy(d => d.Name).Select(d => d.Home.City)]), "Paris", "Rome");
        Native(Zoo(db => [.. db.Animals.OfType<Dog>().OrderBy(d => d.Name).Select(d => new { d.Name, d.Home.Geo.Lat }).ToList().Select(x => x.Name + ":" + x.Lat)]),
            "Fido:3", "Rex:2");
        // Derived-only members projected after OfType decline natively (pre-existing scalar limitation, Task 10): pinned.
        Declines(Zoo(db => [.. db.Animals.OfType<Dog>().OrderBy(d => d.Name).Select(d => d.Kennel.Lat).ToList().Select(x => x.ToString())]), "5", "2");
        Declines(Zoo(db => [.. db.Animals.OfType<Cat>().Select(c => new { c.Name, c.Vet.City }).ToList().Select(x => x.Name + ":" + x.City)]), "Tom:Oslo");
        Declines(Zoo(db => [.. db.Animals.OfType<Dog>().OrderBy(d => d.Name).Select(d => d.Age).ToList().Select(x => x.ToString())]), "7", "3");
    }

    [Fact]
    public void Set_operation_and_Distinct_over_complex_leaves_of_a_hierarchy()
    {
        Native(Zoo(db => [.. db.Animals.Select(a => a.Home.City).Distinct().ToList().Order()]), "Paris", "Rome");
        Native(Zoo(db => [.. db.Animals.OfType<Dog>().Select(d => d.Home.City).Union(db.Animals.OfType<Cat>().Select(c => c.Home.City)).ToList().Order()]),
            "Paris", "Rome");
    }

    // ── Shared-type entity types with complex properties ──────────────────────────────────────────────────────

    public class Bag
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Composition.Addr Where { get; set; } = null!;
    }

    private sealed class SharedContext(DbContextOptions options, string a, string b) : DbContext(options)
    {
        public IQueryable<Bag> A => Set<Bag>("A");
        public IQueryable<Bag> B => Set<Bag>("B");

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.SharedTypeEntity<Bag>("A", e =>
            {
                e.ToCollection(a);
                e.ComplexProperty(x => x.Where, w => w.ComplexProperty(g => g.Geo));
            });
            // B stores the same complex CLR type under different element names.
            mb.SharedTypeEntity<Bag>("B", e =>
            {
                e.ToCollection(b);
                e.ComplexProperty(x => x.Where, w =>
                {
                    w.HasPropertyAnnotation(MongoDB.EntityFrameworkCore.Metadata.MongoAnnotationNames.ElementName, "at");
                    w.Property(x => x.City).Metadata.SetElementName("c");
                    w.ComplexProperty(g => g.Geo);
                });
            });
        }
    }

    [Fact]
    public void Shared_type_entity_types_resolve_their_own_complex_element_names()
    {
        var a = Collection(nameof(Shared_type_entity_types_resolve_their_own_complex_element_names) + "a",
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "a1" }, { "Where", Addr("Paris", 1) } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "a2" }, { "Where", Addr("Rome", 2) } });
        var b = Collection(nameof(Shared_type_entity_types_resolve_their_own_complex_element_names) + "b",
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "b1" },
                { "at", new BsonDocument { { "Street", "s" }, { "c", "Oslo" }, { "Geo", Geo(3, 3) }, { "Code", 1 } } },
                // Decoy under A's element names.
                { "Where", Addr("Paris", 9) }
            });

        Func<MongoQueryMode, List<string>> Run(Func<SharedContext, IEnumerable<string>> query)
            => mode =>
            {
                using var db = new SharedContext(Options<SharedContext>(mode), a, b);
                return query(db).ToList();
            };

        Native(Run(db => [.. db.A.Where(x => x.Where.City == "Paris").Select(x => x.Name)]), "a1");
        // Whole shared-type entities decline natively (pre-existing; a shared-type entity without complex properties too).
        Declines(Run(db => [.. db.B.Where(x => x.Where.City == "Oslo").ToList().Select(x => x.Name + "|" + x.Where.City + "|" + x.Where.Geo.Lat)]), "b1|Oslo|3");
        Native(Run(db => [.. db.B.Where(x => x.Where.City == "Paris").Select(x => x.Name)]));
        PerMode(Run(db => [.. db.A.Select(x => x.Where.City).Union(db.B.Select(x => x.Where.City)).ToList().Order()]),
            ["Oslo", "Paris", "Rome"], Serves, Serves, "cross-DbSet");
        // KNOWN PRE-EXISTING WRONG READ (not complex-specific; Jira candidate, not filed): in a join between two shared-type
        // entity types of ONE CLR type (A joined to B, both Bag), the driver-LINQ path can't attribute a member of the inner
        // side by CLR type and reads it through A's element names: B's `y.Where.City` answers A's decoy `Where.City`
        // ("Paris") instead of `at.c` ("Oslo"), in Native and DriverLinq. A renamed SCALAR of B read on the same path is
        // misread the same way (measured: `a1:Paris` for `y.City` stored as `c`); it only reads right when the join goes
        // native. Not fixed here: the bridge needs per-parameter entity-type tracking. Pinned so a fix is noticed.
        foreach (var mode in new[] { MongoQueryMode.Native, MongoQueryMode.DriverLinq })
        {
            Assert.Equal(["a1:Paris", "a2:Paris"], Run(db => [.. db.A.Join(db.B, x => x.Where.Code, y => y.Where.Code, (x, y) => x.Name + ":" + y.Where.City).ToList().Order()])(mode));
        }

        Assert.Throws<MongoDB.EntityFrameworkCore.Query.NativeTranslation.NativeTranslationNotSupportedException>(
            () => Run(db => [.. db.A.Join(db.B, x => x.Where.Code, y => y.Where.Code, (x, y) => x.Name + ":" + y.Where.City)])(MongoQueryMode.NativeOnly));
    }

    // ── Owned types containing complex properties ─────────────────────────────────────────────────────────────

    [ComplexType]
    public class Plate
    {
        public string Label { get; set; } = null!;
        public int Row { get; set; }
    }

    public class Garage
    {
        public string City { get; set; } = null!;
        public Plate Plate { get; set; } = null!;
    }

    public class Lot
    {
        public string Sku { get; set; } = null!;
        public Plate Plate { get; set; } = null!;
    }

    public class Owner
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Garage Garage { get; set; } = null!;
        public List<Lot> Lots { get; set; } = [];
        public Composition.Addr Address { get; set; } = null!;
    }

    private sealed class OwnerContext(DbContextOptions options, string collection) : DbContext(options)
    {
        public DbSet<Owner> Owners => Set<Owner>();

        protected override void OnModelCreating(ModelBuilder mb)
            => mb.Entity<Owner>(b =>
            {
                b.ToCollection(collection);
                b.OwnsOne(o => o.Garage);
                b.OwnsMany(o => o.Lots);
                b.ComplexProperty(o => o.Address, a => a.ComplexProperty(x => x.Geo));
            });
    }

    private static BsonDocument PlateDoc(string label, int row) => new() { { "Label", label }, { "Row", row } };

    // Ann: Garage Paris plate (P1, 1), lots [x (L1, 3), y (L2, 1)], Address Rome; Bob: Garage Rome plate (P2, 2), lots [z (L3, 2)],
    // Address Paris.
    private Func<MongoQueryMode, List<string>> Owners(Func<OwnerContext, IEnumerable<string>> query,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = Collection(name,
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Ann" },
                { "Garage", new BsonDocument { { "City", "Paris" }, { "Plate", PlateDoc("P1", 1) } } },
                { "Lots", new BsonArray { new BsonDocument { { "Sku", "x" }, { "Plate", PlateDoc("L1", 3) } }, new BsonDocument { { "Sku", "y" }, { "Plate", PlateDoc("L2", 1) } } } },
                { "Address", Addr("Rome", 1) }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Bob" },
                { "Garage", new BsonDocument { { "City", "Rome" }, { "Plate", PlateDoc("P2", 2) } } },
                { "Lots", new BsonArray { new BsonDocument { { "Sku", "z" }, { "Plate", PlateDoc("L3", 2) } } } },
                { "Address", Addr("Paris", 2) }
            });
        return mode =>
        {
            using var db = new OwnerContext(Options<OwnerContext>(mode), collection);
            return query(db).ToList();
        };
    }

    [Fact]
    public void Owned_then_complex_leaves_in_predicates_ordering_and_operators()
    {
        Native(Owners(db => [.. db.Owners.Where(o => o.Garage.Plate.Label == "P2").Select(o => o.Name)]), "Bob");
        Native(Owners(db => [.. db.Owners.OrderByDescending(o => o.Garage.Plate.Row).Select(o => o.Name)]), "Bob", "Ann");
        Native(Owners(db => [.. db.Owners.Where(o => o.Garage.Plate.Row < 2 && o.Address.City == "Rome").ToList()
            .Select(o => $"{o.Name}|{o.Garage.Plate.Label}|{o.Address.City}|{string.Join(",", o.Lots.Select(l => l.Plate.Label))}")]),
            "Ann|P1|Rome|L1,L2");
        Native(Owners(db => [.. db.Owners.Select(o => o.Garage.Plate.Label).Union(db.Owners.Select(o => o.Address.City)).ToList().Order()]),
            "P1", "P2", "Paris", "Rome");
        Native(Owners(db => [.. db.Owners.Select(o => new { o.Garage.City, o.Garage.Plate.Row }).Distinct().ToList().Select(x => x.City + ":" + x.Row).Order()]),
            "Paris:1", "Rome:2");
        Native(Owners(db => [db.Owners.Count(o => o.Garage.Plate.Row > 1).ToString()]), "1");
    }

    [Fact]
    public void Complex_leaves_inside_owned_collection_elements()
    {
        PerMode(Owners(db => [.. db.Owners.Where(o => o.Lots.Any(l => l.Plate.Row > 2)).Select(o => o.Name)]), ["Ann"], Serves, Serves, Serves);
        // A filtered Count over an owned collection in a PROJECTION is refused by EF in every mode, for a scalar element
        // member too (pre-existing). Never rows.
        PerMode(Owners(db => [.. db.Owners.OrderBy(o => o.Name).Select(o => o.Lots.Count(l => l.Plate.Label != "L2").ToString())]), ["1", "1"],
            "could not be translated", "could not be translated", "could not be translated");
        // An owned-collection SelectMany projecting an element's complex leaf declines natively (the SelectMany projection
        // binder binds one-hop members only); the owned unwind fallback serves.
        Declines(Owners(db => [.. db.Owners.SelectMany(o => o.Lots, (o, l) => new { o.Name, l.Sku, l.Plate.Label }).ToList()
                .Select(x => $"{x.Name}|{x.Sku}|{x.Label}").Order()]),
            "Ann|x|L1", "Ann|y|L2", "Bob|z|L3");
    }
}
