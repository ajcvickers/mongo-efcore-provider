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
/// DTO, are served in every mode (including a value stored under renamed members); a construction with MORE than one
/// argument that includes a whole complex value is not supported: it declines natively and the fallback fails loudly, except
/// for two complex values of the same CLR type, which is a known wrong read (pinned below).
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

    [Fact]
    public void Two_argument_record_of_a_whole_complex_value_and_a_scalar_is_refused()
        => PerMode(Run(db => db.Clients.OrderBy(c => c.Name).Select(c => new Two(c.Billing, c.Name)).ToList().Select(x => x.N + "|" + Composition.Fmt(x.A))),
            [], NotNative, Throws<InvalidOperationException>("could not be located in the document"),
            Throws<InvalidOperationException>("could not be located in the document"));

    // Two whole complex values of the same CLR type stored DIFFERENTLY: loud, with a misleading message (the second
    // argument is read through the first's element names).
    [Fact]
    public void Two_argument_record_of_two_differently_stored_complex_values_fails_loudly()
        => PerMode(Run(db => db.Clients.OrderBy(c => c.Name).Select(c => new TwoAddr(c.Billing, c.Shipping)).ToList().Select(x => Composition.Fmt(x.A) + "|" + Composition.Fmt(x.B))),
            [], NotNative, Throws<InvalidOperationException>("Document element is missing for required non-nullable property 'City'"),
            Throws<InvalidOperationException>("Document element is missing for required non-nullable property 'City'"));

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

    // KNOWN WRONG READ (unreleased, complex-only; listed in the design spec's known limitations): two whole complex values
    // of the same CLR type, stored alike, as constructor arguments read EVERY argument from the LAST one. Correct: H|W.
    // Pinned to the measured rows so a fix flips it; the owned control below reads correctly.
    [Fact]
    public void Two_argument_record_of_two_alike_stored_complex_values_is_a_known_wrong_read()
        => PerMode(Pair(owned: false), ["W|W"], NotNative, Serves, Serves);

    [Fact]
    public void Owned_control_two_argument_record_of_two_owned_values_reads_each_argument()
        => PerMode(Pair(owned: true), ["H|W"], NotNative, Serves, Serves);
}
