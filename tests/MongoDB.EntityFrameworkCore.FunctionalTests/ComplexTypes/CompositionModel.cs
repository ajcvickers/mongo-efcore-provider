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
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// The model, seed and per-mode assertion shared by the composition tests (<see cref="ComplexTypeCompositionTests"/>,
/// <see cref="ComplexTypeJoinCompositionTests"/>): clients with two complex properties of the SAME CLR type stored
/// differently (<c>Billing</c> under its CLR name with an int <c>Code</c>; <c>Shipping</c> under <c>ship</c>, its City under
/// <c>town</c>, its Code stored as a string), and orders with their own complex <c>Ship</c>, a scalar <c>City</c> and an
/// optional reference to the client.
/// </summary>
public static class Composition
{
    public class Addr
    {
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
        public GeoPoint Geo { get; set; }
        public int? Zip { get; set; }
        public int Code { get; set; }
    }

    public class Client
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rank { get; set; }
        public Addr Billing { get; set; } = null!;
        public Addr Shipping { get; set; } = null!;
        public List<Order> Orders { get; set; } = [];
    }

    public class Order
    {
        public ObjectId Id { get; set; }
        public ObjectId? ClientId { get; set; }
        public Client? Client { get; set; }
        public string City { get; set; } = null!;
        public int Rank { get; set; }
        public Addr Ship { get; set; } = null!;
    }

    public enum Filter
    {
        None,
        ClientBillingCity,
        OrderClientBillingCity,
        OrderShipCity
    }

    public sealed class ShopContext(DbContextOptions options, string clients, string orders, Filter filter) : DbContext(options)
    {
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Client>(b =>
            {
                b.ToCollection(clients);
                b.ComplexProperty(c => c.Billing, a => a.ComplexProperty(x => x.Geo));
                b.ComplexProperty(c => c.Shipping, a =>
                {
                    a.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "ship");
                    a.Property(x => x.City).Metadata.SetElementName("town");
                    a.Property(x => x.Code).HasConversion<string>();
                    a.ComplexProperty(x => x.Geo);
                });
                b.HasMany(c => c.Orders).WithOne(o => o.Client).HasForeignKey(o => o.ClientId);
                if (filter == Filter.ClientBillingCity)
                {
                    b.HasQueryFilter(c => c.Billing.City != "Hidden");
                }
            });
            mb.Entity<Order>(b =>
            {
                b.ToCollection(orders);
                b.ComplexProperty(o => o.Ship, a => a.ComplexProperty(x => x.Geo));
                if (filter == Filter.OrderClientBillingCity)
                {
                    b.HasQueryFilter(o => o.Client!.Billing.City != "Hidden");
                }
                else if (filter == Filter.OrderShipCity)
                {
                    b.HasQueryFilter(o => o.Ship.City != "Hidden");
                }
            });
        }
    }

    public static readonly ObjectId AnnId = ObjectId.GenerateNewId();
    public static readonly ObjectId BobId = ObjectId.GenerateNewId();
    public static readonly ObjectId CidId = ObjectId.GenerateNewId();
    public static readonly ObjectId HidId = ObjectId.GenerateNewId();

    private static BsonDocument Geo(double lat, double lon) => new() { { "Lat", lat }, { "Lon", lon } };

    private static BsonDocument Bill(string city, double lat, double lon, BsonValue? zip, int code)
    {
        var doc = new BsonDocument { { "Street", "b-" + city }, { "City", city }, { "Geo", Geo(lat, lon) }, { "Code", code } };
        if (zip != null)
        {
            doc["Zip"] = zip;
        }

        return doc;
    }

    // Stored under the Shipping property's own element names: `town` for City, Code as a string.
    private static BsonDocument ShipTo(string city, double lat, double lon, BsonValue? zip, string code)
    {
        var doc = new BsonDocument { { "Street", "s-" + city }, { "town", city }, { "Geo", Geo(lat, lon) }, { "Code", code } };
        if (zip != null)
        {
            doc["Zip"] = zip;
        }

        return doc;
    }

    private static BsonDocument OrderShip(string city, double lat)
        => new() { { "Street", "o-" + city }, { "City", city }, { "Geo", Geo(lat, lat) }, { "Code", 0 } };

    /// <summary>
    /// Clients (Billing city/lat/lon/zip/code | Shipping city/lat/lon/zip/code):
    /// Ann r1 (Paris 1 10 75 1 | Rome 2 20 - 2), Bob r2 (Rome 3 30 null 2 | Oslo 1 40 - 1),
    /// Cid r3 (Oslo 1 10 - 3 | Paris 3 50 9 3), Hid r4 (Hidden 5 60 - 4 | Paris 5 60 - 4); "-" = element missing.
    /// Orders (client, City, Rank, Ship city, Ship lat): o1 (Ann, Paris, 1, Rome, 1), o2 (Ann, Lima, 2, Paris, 2),
    /// o3 (Bob, Rome, 5, Oslo, 3), o4 (no client, Oslo, 3, Paris, 4), o5 (Hid, Hidden, 4, Hidden, 5).
    /// </summary>
    public static (string Clients, string Orders) Seed(TemporaryDatabaseFixture database, string name)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var clients = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + suffix + "_c";
        var orders = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + suffix + "_o";
        database.MongoDatabase.GetCollection<BsonDocument>(clients).InsertMany(
        [
            Client(AnnId, "Ann", 1, Bill("Paris", 1, 10, 75, 1), ShipTo("Rome", 2, 20, null, "2")),
            Client(BobId, "Bob", 2, Bill("Rome", 3, 30, BsonNull.Value, 2), ShipTo("Oslo", 1, 40, null, "1")),
            Client(CidId, "Cid", 3, Bill("Oslo", 1, 10, null, 3), ShipTo("Paris", 3, 50, 9, "3")),
            Client(HidId, "Hid", 4, Bill("Hidden", 5, 60, null, 4), ShipTo("Paris", 5, 60, null, "4"))
        ]);
        database.MongoDatabase.GetCollection<BsonDocument>(orders).InsertMany(
        [
            Order(AnnId, "Paris", 1, OrderShip("Rome", 1)),
            Order(AnnId, "Lima", 2, OrderShip("Paris", 2)),
            Order(BobId, "Rome", 5, OrderShip("Oslo", 3)),
            Order(ObjectId.GenerateNewId(), "Oslo", 3, OrderShip("Paris", 4)),
            Order(HidId, "Hidden", 4, OrderShip("Hidden", 5))
        ]);
        return (clients, orders);

        static BsonDocument Client(ObjectId id, string name, int rank, BsonDocument billing, BsonDocument shipping)
            => new() { { "_id", id }, { "Name", name }, { "Rank", rank }, { "Billing", billing }, { "ship", shipping } };

        static BsonDocument Order(ObjectId clientId, string city, int rank, BsonDocument ship)
            => new() { { "_id", ObjectId.GenerateNewId() }, { "ClientId", clientId }, { "City", city }, { "Rank", rank }, { "Ship", ship } };
    }

    public static ShopContext Create(
        TemporaryDatabaseFixture database, (string Clients, string Orders) collections, MongoQueryMode mode,
        Filter filter = Filter.None, QueryTrackingBehavior tracking = QueryTrackingBehavior.NoTracking)
    {
        var builder = new DbContextOptionsBuilder<ShopContext>()
            .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
            .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
            .UseQueryTrackingBehavior(tracking)
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
        return new ShopContext(builder.Options, collections.Clients, collections.Orders, filter);
    }

    public static string Fmt(Addr a) => $"{a.City}/{a.Geo.Lat}/{a.Zip?.ToString() ?? "-"}/{a.Code}";

    public static string Fmt(Client c) => $"{c.Name}|{Fmt(c.Billing)}|{Fmt(c.Shipping)}";

    public static string Fmt(Order o) => $"{o.Rank}|{o.City}|{Fmt(o.Ship)}";
}

/// <summary>
/// Per-mode outcome pins for the composition tests: each mode either serves the hand-written rows (<see cref="Serves"/>),
/// declines natively (<see cref="NotNative"/>: a <see cref="NativeTranslationNotSupportedException"/>), or throws an
/// exception whose message contains a fragment (<c>||</c>-separated alternatives). Never wrong rows: a serving mode is
/// compared with the hand-written answer, and a both-throw pin names the message, so it is not vacuous.
/// </summary>
internal static class CompositionAssert
{
    public const string Serves = "serves";
    public const string NotNative = "<NativeTranslationNotSupportedException>";

    /// <param name="run">Runs the query under a mode.</param>
    /// <param name="expected">The hand-written rows a <see cref="Serves"/> mode must return.</param>
    /// <param name="nativeOnly">The NativeOnly outcome.</param>
    /// <param name="native">The Native outcome.</param>
    /// <param name="driverLinq">The DriverLinq outcome.</param>
    /// <param name="fragmentExceptionTypes">
    /// When given, the exception TYPE of each message fragment is pinned too (a fragment alone could match an unrelated
    /// exception); a matched fragment missing from the map fails the test.
    /// </param>
    public static void PerMode(
        Func<MongoQueryMode, List<string>> run, string[] expected, string nativeOnly, string native, string driverLinq,
        IReadOnlyDictionary<string, Type>? fragmentExceptionTypes = null)
    {
        var failures = new List<string>();
        foreach (var (mode, want) in new[] { (MongoQueryMode.NativeOnly, nativeOnly), (MongoQueryMode.Native, native), (MongoQueryMode.DriverLinq, driverLinq) })
        {
            List<string>? rows = null;
            Exception? error = null;
            try
            {
                rows = run(mode);
            }
            catch (Exception e) when (e is not Xunit.Sdk.XunitException)
            {
                error = e;
            }

            var got = error == null ? $"rows [{string.Join("; ", rows!)}]" : $"{error.GetType().Name}: {error.Message}";
            var ok = want switch
            {
                Serves => error == null && expected.SequenceEqual(rows!),
                NotNative => error is NativeTranslationNotSupportedException,
                _ => error != null && want.Split("||").Any(error.Message.Contains)
                     && (fragmentExceptionTypes is null
                         || (fragmentExceptionTypes.TryGetValue(want.Split("||").First(error.Message.Contains), out var type)
                             && error.GetType() == type))
            };
            if (!ok)
            {
                failures.Add($"{mode}: expected {(want == Serves ? $"rows [{string.Join("; ", expected)}]" : want)}, got {got}");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>Native in every mode (NativeOnly proves it goes native), all three against the hand-written answer.</summary>
    public static void Native(Func<MongoQueryMode, List<string>> run, params string[] expected)
        => PerMode(run, expected, Serves, Serves, Serves);

    /// <summary>A clean decline: NativeOnly refuses, Native falls back and, like DriverLinq, serves the hand-written answer.</summary>
    public static void Declines(Func<MongoQueryMode, List<string>> run, params string[] expected)
        => PerMode(run, expected, NotNative, Serves, Serves);

    /// <summary>
    /// A refusal (ruling R20): NativeOnly declines, Native refuses with a <see cref="NativeTranslationNotSupportedException"/>
    /// whose message contains every one of <paramref name="nativeMessageFragments"/> (instead of falling back to wrong rows),
    /// and explicit DriverLinq runs the driver, pinned to its measured <paramref name="driverRows"/>.
    /// </summary>
    public static void Refused(Func<MongoQueryMode, List<string>> run, string[] driverRows, params string[] nativeMessageFragments)
    {
        Assert.IsType<NativeTranslationNotSupportedException>(Record.Exception(() => run(MongoQueryMode.NativeOnly)));
        var native = Assert.IsType<NativeTranslationNotSupportedException>(Record.Exception(() => run(MongoQueryMode.Native)));
        foreach (var fragment in nativeMessageFragments)
        {
            Assert.Contains(fragment, native.Message);
        }

        var driver = run(MongoQueryMode.DriverLinq);
        Assert.True(driverRows.SequenceEqual(driver), $"DriverLinq: expected [{string.Join("; ", driverRows)}], got [{string.Join("; ", driver)}]");
    }
}
