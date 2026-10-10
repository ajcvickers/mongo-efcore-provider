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
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Infrastructure;
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.CompositionAssert;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Constructor, record and member-init projections of a whole complex value, per mode (the source of the constructor
/// sentence in docs/complex-types.md). A ONE-argument constructor or record of a whole complex value, and a member-init
/// DTO, are served in every mode (including a value stored under renamed members). A construction with MORE than one
/// argument whose arguments are all whole complex values declines natively and the fallback serves it (each argument read
/// from its own element; it used to read every argument from the last one, spec known limitation 29); one that mixes a
/// whole complex value with a scalar declines natively and the fallback refuses it loudly. More shapes:
/// <see cref="ComplexValueSameTypeArgumentTests"/>.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexValueConstructorProjectionTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public record One(Composition.Addr A);
    public record Two(Composition.Addr A, string N);
    public record TwoAddr(Composition.Addr A, Composition.Addr B);

    public class OneCtor(Composition.Addr a)
    {
        public Composition.Addr A { get; } = a;
    }

    public class Dto
    {
        public Composition.Addr? A { get; set; }
        public string? N { get; set; }
    }

    private Func<MongoQueryMode, List<string>> Run(Func<Composition.ShopContext, IEnumerable<string>> query,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collections = Composition.Seed(database, name);
        return mode =>
        {
            using var db = Composition.Create(database, collections, mode);
            return query(db).ToList();
        };
    }

    private static readonly string[] BillingRows = ["Paris/1/75/1", "Rome/3/-/2", "Oslo/1/-/3", "Hidden/5/-/4"];
    private static readonly string[] ShippingRows = ["Rome/2/-/2", "Oslo/1/-/1", "Paris/3/9/3", "Paris/5/-/4"];

    [Fact]
    public void One_argument_record_of_a_whole_complex_value_is_served_in_every_mode()
    {
        Native(Run(db => db.Clients.OrderBy(c => c.Name).Select(c => new One(c.Billing)).ToList().Select(x => Composition.Fmt(x.A))), BillingRows);
        // Shipping is stored under renamed members (`ship`, City as `town`, Code as a string).
        Native(Run(db => db.Clients.OrderBy(c => c.Name).Select(c => new One(c.Shipping)).ToList().Select(x => Composition.Fmt(x.A)), "OneShipping"), ShippingRows);
    }

    [Fact]
    public void One_argument_constructor_of_a_whole_complex_value_is_served_in_every_mode()
        => Native(Run(db => db.Clients.OrderBy(c => c.Name).Select(c => new OneCtor(c.Shipping)).ToList().Select(x => Composition.Fmt(x.A))), ShippingRows);

    [Fact]
    public void Member_init_dto_with_a_whole_complex_value_is_served_in_every_mode()
        => Native(Run(db => db.Clients.OrderBy(c => c.Name).Select(c => new Dto { A = c.Shipping, N = c.Name }).ToList().Select(x => x.N + "|" + Composition.Fmt(x.A!))),
            "Ann|Rome/2/-/2", "Bob|Oslo/1/-/1", "Cid|Paris/3/9/3", "Hid|Paris/5/-/4");

    // The scalar argument shares the construction's one projection member with the complex one, so the fallback's
    // member-less construction refusal names the type and the remedy.
    [Fact]
    public void Two_argument_record_of_a_whole_complex_value_and_a_scalar_is_refused()
        => PerMode(Run(db => db.Clients.OrderBy(c => c.Name).Select(c => new Two(c.Billing, c.Name)).ToList().Select(x => x.N + "|" + Composition.Fmt(x.A))),
            [], NotNative, Throws<InvalidOperationException>("The projection constructs 'Two' from arguments that can't each be read"),
            Throws<InvalidOperationException>("The projection constructs 'Two' from arguments that can't each be read"));

    // Two whole complex values of the same CLR type stored DIFFERENTLY (Shipping under renamed members): each argument is
    // read through its own element names. Before the limitation-29 fix this threw "Document element is missing ..."
    // (the first argument was read through the second's registration).
    [Fact]
    public void Two_argument_record_of_two_differently_stored_complex_values_reads_each_argument()
        => PerMode(Run(db => db.Clients.OrderBy(c => c.Name).Select(c => new TwoAddr(c.Billing, c.Shipping)).ToList().Select(x => Composition.Fmt(x.A) + "|" + Composition.Fmt(x.B))),
            ["Paris/1/75/1|Rome/2/-/2", "Rome/3/-/2|Oslo/1/-/1", "Oslo/1/-/3|Paris/3/9/3", "Hidden/5/-/4|Paris/5/-/4"], NotNative, Serves, Serves);

    public class PA
    {
        public string City { get; set; } = "";
    }

    public class PP
    {
        public ObjectId Id { get; set; }
        public PA Home { get; set; } = null!;
        public PA Work { get; set; } = null!;
    }

    public record PTwo(PA A, PA B);

    private Func<MongoQueryMode, List<string>> Pair(bool owned, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = database.CreateCollection<PP>(TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8]);
        collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName).InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Home", new BsonDocument("City", "H") }, { "Work", new BsonDocument("City", "W") }
        });
        return mode =>
        {
            using var db = SingleEntityDbContext.Create(collection, mb =>
                {
                    if (owned)
                    {
                        mb.Entity<PP>().OwnsOne(x => x.Home);
                        mb.Entity<PP>().OwnsOne(x => x.Work);
                    }
                    else
                    {
                        mb.Entity<PP>().ComplexProperty(x => x.Home);
                        mb.Entity<PP>().ComplexProperty(x => x.Work);
                    }
                }, null,
                b =>
                {
                    b.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
                    new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
                });
            return db.Entities.AsNoTracking().Select(x => new PTwo(x.Home, x.Work)).ToList().Select(t => t.A.City + "|" + t.B.City).ToList();
        };
    }

    // Was a silent wrong read (spec known limitation 29, resolved): two whole complex values of the same CLR type, stored
    // alike, as constructor arguments read EVERY argument from the LAST one (W|W). Each is now read from its own element,
    // like the owned control below.
    [Fact]
    public void Two_argument_record_of_two_alike_stored_complex_values_reads_each_argument()
        => PerMode(Pair(owned: false), ["H|W"], NotNative, Serves, Serves);

    [Fact]
    public void Owned_control_two_argument_record_of_two_owned_values_reads_each_argument()
        => PerMode(Pair(owned: true), ["H|W"], NotNative, Serves, Serves);
}
