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
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.Composition;
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.CompositionAssert;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Complex properties on the OUTER and the INNER side of <c>Join</c>, <c>GroupJoin</c>, left joins and <c>SelectMany</c>:
/// complex leaves as join keys (alone and inside anonymous keys), in result selectors, in predicates on either side and
/// after paging, plus whole entities with complex values materialized from either side. Model and seed:
/// <see cref="Composition"/>; every shape is pinned per mode against a hand-written answer.
/// </summary>
/// <remarks>
/// A complex leaf of a JOIN SCOPE (`x.c.Shipping.City`, a hop under `ti.Inner`/`ti.Outer`), of a reference navigation, or
/// as a join key declines natively (NativeOnly refuses; the fallback serves): the native join-scope and key resolvers
/// walk only one hop (`MongoExpressionTranslator.TryBeginOwnedHopWalk` refuses an inner-prefixed scope; join keys are
/// resolved by simple property name). Owned-navigation hops decline identically (pre-existing); native join-scope hops
/// are a follow-up. What these tests guard is that no mode reads the wrong document or the wrong field.
/// </remarks>
[XUnitCollection("QueryTests")]
public class ComplexTypeJoinCompositionTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    private Func<MongoQueryMode, List<string>> Shop(
        Func<ShopContext, IEnumerable<string>> query, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collections = Seed(database, name + Guid.NewGuid().ToString("N")[..4]);
        return mode =>
        {
            using var db = Create(database, collections, mode);
            return query(db).ToList();
        };
    }

    // ── Complex leaves as join keys ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Join_on_a_complex_leaf_of_the_inner_side_against_an_outer_scalar()
        // o.City == c.Billing.City: o1 Paris->Ann, o3 Rome->Bob, o4 Oslo->Cid, o5 Hidden->Hid; o2 Lima has no match.
        => Declines(Shop(db => [.. db.Orders.Join(db.Clients, o => o.City, c => c.Billing.City, (o, c) => new { o.Rank, c.Name })
                .OrderBy(x => x.Rank).ToList().Select(x => $"{x.Rank}|{x.Name}")]),
            "1|Ann", "3|Cid", "4|Hid", "5|Bob");

    [Fact]
    public void Join_on_a_complex_leaf_of_the_outer_side_against_an_inner_complex_leaf_with_a_different_element_name()
        // c.Shipping.City (stored `ship.town`) == o.Ship.City: Ann Rome->o1 (rank 1); Bob Oslo->o3 (5); Cid and Hid Paris->o2 (2), o4 (3).
        => Declines(Shop(db => [.. db.Clients.Join(db.Orders, c => c.Shipping.City, o => o.Ship.City, (c, o) => new { c.Name, o.Rank })
                .OrderBy(x => x.Name).ThenBy(x => x.Rank).ToList().Select(x => $"{x.Name}|{x.Rank}")]),
            "Ann|1", "Bob|5", "Cid|2", "Cid|3", "Hid|2", "Hid|3");

    [Fact]
    public void Join_on_a_struct_complex_leaf()
        // c.Billing.Geo.Lat == o.Ship.Geo.Lat: Ann 1->o1 (rank 1), Cid 1->o1, Bob 3->o3 (5), Hid 5->o5 (4).
        => Declines(Shop(db => [.. db.Clients.Join(db.Orders, c => c.Billing.Geo.Lat, o => o.Ship.Geo.Lat, (c, o) => new { c.Name, o.Rank })
                .OrderBy(x => x.Name).ToList().Select(x => $"{x.Name}|{x.Rank}")]),
            "Ann|1", "Bob|5", "Cid|1", "Hid|4");

    [Fact]
    public void Join_on_an_anonymous_key_holding_complex_leaves_is_not_served()
        // new { City, Rank } on both sides would answer o1 = Ann, o4 = Cid, o5 = Hid. The native anonymous-key join admits
        // only one-hop members (TryResolveAnonymousKeyJoinProperties) and the driver can't translate an anonymous key, so it
        // fails loudly in every mode, exactly like an owned-hop member (pre-existing). Never rows.
        => PerMode(Shop(db => [.. db.Orders.Join(db.Clients, o => new { o.City, o.Rank }, c => new { c.Billing.City, c.Rank },
                    (o, c) => new { o.Rank, c.Name })
                .OrderBy(x => x.Rank).ToList().Select(x => $"{x.Rank}|{x.Name}")]),
            ["1|Ann", "3|Cid", "4|Hid"], NotNative, Throws<MongoDB.Driver.Linq.ExpressionNotSupportedException>("Expression not supported"), Throws<MongoDB.Driver.Linq.ExpressionNotSupportedException>("Expression not supported"));

    [Fact]
    public void Join_key_through_a_complex_hop_never_resolves_to_a_same_named_root_property()
    {
        // Order has a scalar City AND a complex Ship.City. A key `a => a.Ship.City` used to resolve by its simple name to
        // the root's City (silent wrong rows in EVERY mode, NativeOnly included: `1|1; 2|2; ...`). Now only a direct read is
        // resolved by name (IsDirectKeyRead), so the hop key declines natively and the fallback joins on Ship.City.
        // Ranks/City/Ship.City: o1 1/Paris/Rome, o2 2/Lima/Paris, o3 5/Rome/Oslo, o4 3/Oslo/Paris, o5 4/Hidden/Hidden.
        Declines(Shop(db => [.. db.Orders.Join(db.Orders, a => a.Ship.City, b => b.City, (a, b) => a.Rank + "|" + b.Rank).ToList().Order()]),
            "1|5", "2|1", "3|1", "4|4", "5|3");
        Declines(Shop(db => [.. db.Orders.Join(db.Orders, a => a.City, b => b.Ship.City, (a, b) => a.Rank + "|" + b.Rank).ToList().Order()]),
            "1|2", "1|3", "3|5", "4|4", "5|1");
        Declines(Shop(db => [.. (from a in db.Orders
                    join b in db.Orders on a.Ship.City equals b.City into g
                    from b in g.DefaultIfEmpty()
                    select a.Rank + "|" + (b == null ? "-" : b.Rank.ToString())).ToList().Order()]),
            "1|5", "2|1", "3|1", "4|4", "5|3");
    }

    [Fact]
    public void Join_key_over_complex_leaves_stored_differently_is_a_known_pre_existing_wrong_answer()
    {
        // Shipping.Code is stored as a string ("2"), Order.Rank as an int (2): C# would match Bob|1, Ann|2, Cid|3, Hid|4. The
        // native path declines (hop key); the driver's $lookup compares the STORED forms and matches nothing. PRE-EXISTING,
        // not complex-specific: a root scalar key with a converter (`c.Code` HasConversion<string>() vs `o.Rank`) answers []
        // in every mode, NativeOnly included (the single-key join isn't gated on StoredSerialization.StoredAlike, unlike the
        // anonymous key). Jira candidate (not filed). Pinned so a fix is noticed.
        Func<ShopContext, IEnumerable<string>> query = db => [.. db.Clients.Join(db.Orders, c => c.Shipping.Code, o => o.Rank, (c, o) => new { c.Name, o.Rank })
            .OrderBy(x => x.Rank).ToList().Select(x => $"{x.Name}|{x.Rank}")];
        PerMode(Shop(query), [], NotNative, Serves, Serves);
    }

    // ── Complex leaves in result selectors, predicates on either side, paging after the join ────────────────────

    [Fact]
    public void Navigation_join_with_complex_leaves_of_both_sides_in_predicate_and_projection()
    {
        // Before this task the fallback rendered join-scope complex hops under their CLR names (`Inner.Shipping.City`, not
        // `Inner.ship.town`) and answered no rows / empty leaves (MongoEFToLinqTranslatingExpressionVisitor's join member
        // arm now maps complex hops through the complex serializer).
        Declines(Shop(db => [.. db.Orders.Join(db.Clients, o => o.ClientId, c => (ObjectId?)c.Id, (o, c) => new { o, c })
                .Where(x => x.c.Shipping.City == "Rome" && x.o.Ship.Geo.Lat < 2)
                .Select(x => new { x.o.Rank, B = x.c.Billing.City, S = x.o.Ship.City, x.c.Billing.Zip }).ToList()
                .Select(x => $"{x.Rank}|{x.B}|{x.S}|{x.Zip}")]),
            "1|Paris|Rome|75");
        Declines(Shop(db => [.. db.Orders.Join(db.Clients, o => o.ClientId, c => (ObjectId?)c.Id, (o, c) => new { o, c })
                .OrderBy(x => x.c.Shipping.Geo.Lon).ThenBy(x => x.o.Rank)
                .Select(x => x.o.Ship.City + "|" + x.c.Shipping.City)]),
            "Rome|Rome", "Paris|Rome", "Oslo|Oslo", "Hidden|Paris");
        // The EF.Property spelling of the inner hop.
        Declines(Shop(db => [.. db.Orders.Join(db.Clients, o => o.ClientId, c => (ObjectId?)c.Id, (o, c) => new { o, c })
                .Where(x => EF.Property<string>(EF.Property<Addr>(x.c, "Shipping"), "City") == "Oslo")
                .Select(x => x.o.Rank + "|" + EF.Property<Addr>(x.c, "Billing").City)]),
            "5|Rome");
    }

    [Fact]
    public void Paging_after_a_join_ordered_by_complex_leaves()
        => Declines(Shop(db => [.. db.Orders.Join(db.Clients, o => o.ClientId, c => (ObjectId?)c.Id, (o, c) => new { o, c })
                .OrderBy(x => x.c.Billing.Geo.Lat).ThenBy(x => x.o.Ship.Geo.Lat).Skip(1).Take(2)
                .Select(x => new { x.o.Rank, x.c.Name }).ToList().Select(x => $"{x.Rank}|{x.Name}")]),
            "2|Ann", "5|Bob");

    [Fact]
    public void Join_result_selector_with_whole_entities_materializes_complex_values_from_both_sides()
    {
        foreach (var tracking in new[] { false, true })
        {
            Native(Shop(db =>
                {
                    var q = db.Orders.Join(db.Clients, o => o.ClientId, c => (ObjectId?)c.Id, (o, c) => new { o, c }).OrderBy(x => x.o.Rank);
                    return [.. (tracking ? q.AsTracking() : q).ToList().Select(x => Fmt(x.o) + "#" + Fmt(x.c))];
                }, nameof(Join_result_selector_with_whole_entities_materializes_complex_values_from_both_sides) + tracking),
                "1|Paris|Rome/1/-/0#Ann|Paris/1/75/1|Rome/2/-/2",
                "2|Lima|Paris/2/-/0#Ann|Paris/1/75/1|Rome/2/-/2",
                "4|Hidden|Hidden/5/-/0#Hid|Hidden/5/-/4|Paris/5/-/4",
                "5|Rome|Oslo/3/-/0#Bob|Rome/3/-/2|Oslo/1/-/1");
        }
    }

    [Fact]
    public void Inner_entity_with_complex_values_beside_complex_leaves_in_a_join_projection()
        // The mixed reader used to read the root's `x.o.Ship.City` off a root `Ship` the {_outer, _inner} document doesn't
        // have (empty leaves); a hop's source now takes the `_outer` redirect (ResolveWholeDocumentSource).
        => Declines(Shop(db => [.. db.Orders.Join(db.Clients, o => o.ClientId, c => (ObjectId?)c.Id, (o, c) => new { o, c })
                .OrderBy(x => x.o.Rank).Select(x => new { x.c, x.o.Ship.City }).ToList().Select(x => Fmt(x.c) + "#" + x.City)]),
            "Ann|Paris/1/75/1|Rome/2/-/2#Rome", "Ann|Paris/1/75/1|Rome/2/-/2#Paris", "Hid|Hidden/5/-/4|Paris/5/-/4#Hidden",
            "Bob|Rome/3/-/2|Oslo/1/-/1#Oslo");

    // ── Left joins and GroupJoin ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Left_join_with_complex_leaves_of_an_unmatched_inner_side()
        // o4's client does not exist: its inner leaves are null (reference) and the non-nullable Lat reads default.
        => Declines(Shop(db => [.. (from o in db.Orders
                    join c in db.Clients on o.ClientId equals (ObjectId?)c.Id into g
                    from c in g.DefaultIfEmpty()
                    orderby o.Rank
                    select new { o.Rank, City = c == null ? "<none>" : c.Shipping.City, o.Ship.Geo.Lat })
                .ToList().Select(x => $"{x.Rank}|{x.City}|{x.Lat}")]),
            "1|Rome|1", "2|Rome|2", "3|<none>|4", "4|Paris|5", "5|Oslo|3");

    [Fact]
    public void Left_join_keyed_on_a_complex_leaf_with_a_predicate_on_the_inner_complex_leaf()
        // o.City == c.Shipping.City: Paris->Cid,Hid; Lima none; Rome->Ann; Oslo->Bob; Hidden none.
        => Declines(Shop(db => [.. (from o in db.Orders
                    join c in db.Clients on o.City equals c.Shipping.City into g
                    from c in g.DefaultIfEmpty()
                    orderby o.Rank, c!.Name
                    select new { o.Rank, Name = c == null ? "<none>" : c.Name })
                .ToList().Select(x => $"{x.Rank}|{x.Name}")]),
            "1|Cid", "1|Hid", "2|<none>", "3|Bob", "4|<none>", "5|Ann");

#if !EF8 && !EF9
    [Fact]
    public void LeftJoin_operator_on_complex_leaf_keys()
        => Declines(Shop(db => [.. db.Orders.LeftJoin(db.Clients, o => o.Ship.City, c => c.Billing.City, (o, c) => new { o.Rank, N = c == null ? "<none>" : c.Name })
                .OrderBy(x => x.Rank).ThenBy(x => x.N).ToList().Select(x => $"{x.Rank}|{x.N}")]),
            // Ship cities: o1 Rome->Bob, o2 Paris->Ann, o3 Oslo->Cid, o4 Paris->Ann, o5 Hidden->Hid.
            "1|Bob", "2|Ann", "3|Ann", "4|Hid", "5|Cid");
#endif

    [Fact]
    public void GroupJoin_on_complex_leaf_keys_is_the_known_main_bug_M35()
    {
        // The correct answer is Ann|1, Bob|1, Cid|2, Hid|2. Client and Order are also related by a navigation
        // (Client.Orders), and the projected-count binder (MongoProjectionBindingExpressionVisitor.ResolveCollectionNavigation)
        // turns `os.Count()` into that navigation's FK $lookup without checking the predicate (`c.Shipping.City ==
        // o.Ship.City`), so Native (fallback) and DriverLinq answer the per-client ORDER count. This is main bug M35,
        // identical for scalar keys (NativeNonKeyCorrelationTests.Bare_projected_non_key_count_declines_natively_and_falls_back_to_main_behavior
        // pins it as parity with main); out of scope here (owner decision recorded in the native-parity plan). Pinned so a fix
        // is noticed. NativeOnly declines.
        PerMode(Shop(db => [.. db.Clients.GroupJoin(db.Orders, c => c.Shipping.City, o => o.Ship.City, (c, os) => new { c.Name, N = os.Count() })
                .OrderBy(x => x.Name).ToList().Select(x => $"{x.Name}|{x.N}")]),
            ["Ann|2", "Bob|1", "Cid|0", "Hid|1"], NotNative, Serves, Serves);
    }

    // ── SelectMany ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SelectMany_over_a_collection_navigation_with_complex_leaves_of_both_sides()
        // A reference-collection SelectMany with hop leaves is not native and has no driver-LINQ oracle (cross-DbSet): loud
        // in every mode, exactly like an owned-hop leaf (pre-existing).
        => PerMode(Shop(db => [.. db.Clients.SelectMany(c => c.Orders, (c, o) => new { c.Name, B = c.Billing.City, S = o.Ship.City, o.Rank })
                .Where(x => x.S != "Hidden").OrderBy(x => x.Rank).ToList().Select(x => $"{x.Name}|{x.B}|{x.S}|{x.Rank}")]),
            ["Ann|Paris|Rome|1", "Ann|Paris|Paris|2", "Bob|Rome|Oslo|5"], NotNative, Throws<InvalidOperationException>("cross-DbSet"), Throws<InvalidOperationException>("cross-DbSet"));

    [Fact]
    public void SelectMany_correlated_on_a_complex_leaf()
        // A correlated subquery on a non-key member is refused by EF itself in every mode, as for a scalar member.
        => PerMode(Shop(db => [.. db.Clients.SelectMany(c => db.Orders.Where(o => o.Ship.City == c.Shipping.City), (c, o) => new { c.Name, o.Rank })
                .OrderBy(x => x.Name).ThenBy(x => x.Rank).ToList().Select(x => $"{x.Name}|{x.Rank}")]),
            ["Ann|1", "Bob|5", "Cid|2", "Cid|3", "Hid|2", "Hid|3"], Throws<InvalidOperationException>("could not be translated"), Throws<InvalidOperationException>("could not be translated"), Throws<InvalidOperationException>("could not be translated"));

    // ── Key guards: a hop key whose leaf NAME matches a property of the scope (mutation-discriminating rows) ─────

    public class Delivery
    {
        public ObjectId? ClientId { get; set; }
        public string City { get; set; } = null!;
    }

    public class Parcel
    {
        public ObjectId Id { get; set; }
        public ObjectId? ClientId { get; set; }
        public Client? Client { get; set; }
        public string Label { get; set; } = null!;
        public string City { get; set; } = null!;
        public Delivery Delivery { get; set; } = null!;
    }

    public class Home
    {
        public string City { get; set; } = null!;
    }

    public class Courier
    {
        public ObjectId Id { get; set; }
        public ObjectId? ClientId { get; set; }
        public string City { get; set; } = null!;
        public Home Home { get; set; } = null!;
    }

    private sealed class ParcelContext(DbContextOptions options, string clients, string parcels, string couriers) : DbContext(options)
    {
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Parcel> Parcels => Set<Parcel>();
        public DbSet<Courier> Couriers => Set<Courier>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Client>(b =>
            {
                b.ToCollection(clients);
                b.ComplexProperty(c => c.Billing, a => a.ComplexProperty(x => x.Geo));
                b.ComplexProperty(c => c.Shipping, a => a.ComplexProperty(x => x.Geo));
                b.Ignore(c => c.Orders);
            });
            mb.Entity<Parcel>(b =>
            {
                b.ToCollection(parcels);
                b.HasOne(p => p.Client).WithMany().HasForeignKey(p => p.ClientId);
                b.ComplexProperty(p => p.Delivery);
            });
            mb.Entity<Courier>(b =>
            {
                b.ToCollection(couriers);
                b.OwnsOne(c => c.Home);
            });
        }
    }

    private Func<MongoQueryMode, List<string>> Parcels(
        Func<ParcelContext, IEnumerable<string>> query, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var clients = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + suffix + "_c";
        var parcels = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + suffix + "_p";
        var couriers = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + suffix + "_k";
        BsonDocument Addr(string city) => new() { { "Street", "s" }, { "City", city }, { "Geo", new BsonDocument { { "Lat", 1.0 }, { "Lon", 1.0 } } }, { "Code", 1 } };
        database.MongoDatabase.GetCollection<BsonDocument>(clients).InsertMany(
        [
            new BsonDocument { { "_id", AnnId }, { "Name", "Ann" }, { "Rank", 1 }, { "Billing", Addr("Paris") }, { "Shipping", Addr("Rome") } },
            new BsonDocument { { "_id", BobId }, { "Name", "Bob" }, { "Rank", 2 }, { "Billing", Addr("Rome") }, { "Shipping", Addr("Oslo") } }
        ]);
        // p1 is owned by Ann but delivered to Bob; p2 owned by Bob, delivered to Ann. Joining on the root ClientId instead
        // of Delivery.ClientId swaps every row.
        database.MongoDatabase.GetCollection<BsonDocument>(parcels).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "ClientId", AnnId }, { "Label", "p1" }, { "City", "Paris" }, { "Delivery", new BsonDocument { { "ClientId", BobId }, { "City", "Oslo" } } } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "ClientId", BobId }, { "Label", "p2" }, { "City", "Oslo" }, { "Delivery", new BsonDocument { { "ClientId", AnnId }, { "City", "Paris" } } } }
        ]);
        // k1 lives in Rome with Home.City Lima, k2 in Oslo with Home.City Rome (= k1's root City).
        database.MongoDatabase.GetCollection<BsonDocument>(couriers).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "ClientId", AnnId }, { "City", "Rome" }, { "Home", new BsonDocument("City", "Lima") } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "ClientId", BobId }, { "City", "Oslo" }, { "Home", new BsonDocument("City", "Rome") } }
        ]);
        return mode =>
        {
            var builder = new DbContextOptionsBuilder<ParcelContext>()
                .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
                .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
            using var db = new ParcelContext(builder.Options, clients, parcels, couriers);
            return query(db).ToList();
        };
    }

    [Fact]
    public void Join_key_through_a_complex_hop_named_like_a_navigations_foreign_key_does_not_use_the_navigation()
        // `p.Delivery.ClientId` has the simple name of the Parcel.Client navigation's FK. Resolving the navigation by that
        // name would build its $lookup on the ROOT ClientId (p1|Ann, p2|Bob): wrong rows. The hop key declines natively
        // and the fallback joins on Delivery.ClientId.
        => Declines(Parcels(db => [.. db.Parcels.Join(db.Clients, p => p.Delivery.ClientId, c => (ObjectId?)c.Id, (p, c) => p.Label + "|" + c.Name)
                .ToList().Order()]),
            "p1|Bob", "p2|Ann");

    [Fact]
    public void Join_key_through_an_owned_hop_never_resolves_to_a_same_named_root_property()
        // NON-complex analogue (owner-visible correction): Courier has a root City AND an owned Home.City. At 1c616111 the
        // key `k => k.Home.City` resolved by simple name to the root City and NativeOnly served `k1|k1; k2|k2` (wrong rows,
        // every mode: Rome|Rome, Oslo|Oslo). Now it declines natively and the fallback joins on Home.City: only k2's Home.City
        // (Rome) equals a root City (k1's), so the one row is k2|k1.
        => Declines(Parcels(db => [.. db.Couriers.Join(db.Couriers, a => a.Home.City, b => b.City, (a, b) => a.City + "|" + b.City).ToList()]),
            "Oslo|Rome");

    [Fact]
    public void Owned_hop_beside_a_whole_joined_entity_reads_the_outer_document()
        // Owned analogue of the complex-hop `_outer` redirect: `x.k.Home.City` beside the whole joined client used to read a
        // root `Home` the {_outer, _inner} document doesn't have (null; pre-existing for owned).
        => Declines(Parcels(db => [.. db.Couriers.Join(db.Clients, k => k.ClientId, c => (ObjectId?)c.Id, (k, c) => new { k, c })
                .Select(x => new { x.c, x.k.Home.City }).ToList().Select(x => x.c.Name + "#" + x.City).Order()]),
            "Ann#Lima", "Bob#Rome");

#if !EF8 && !EF9
    [Fact]
    public void Whole_entity_LeftJoin_keyed_through_a_complex_hop_does_not_join_on_a_same_named_root_property()
        // Parcel has a root City AND Delivery.City. A whole-entity LeftJoin takes the bridge's own left-join $lookup builder,
        // which resolved keys by simple name: `p.Delivery.City` must not become the root `City`.
        => Declines(Parcels(db => [.. db.Parcels.LeftJoin(db.Couriers, p => p.Delivery.City, k => k.City, (p, k) => new { p, k })
                .ToList().Select(x => x.p.Label + "|" + (x.k == null ? "-" : x.k.City)).Order()]),
            // p1 Delivery.City Oslo -> k2 (Oslo); p2 Delivery.City Paris -> none. Root cities would give p1 Paris -> none, p2 Oslo -> k2.
            "p1|Oslo", "p2|-");
#endif

    // ── Inner-side hop key on a NAVIGATION-backed join (fix round 1, I1) ─────────────────────────────────────────

    public class AccountRef
    {
        public ObjectId Id { get; set; }
        public AccountRefInner Inner { get; set; } = null!;
    }

    public class AccountRefInner
    {
        public ObjectId Id { get; set; }
    }

    public class AccountTag
    {
        public ObjectId Id { get; set; }
    }

    public class Account
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public AccountRef Ref { get; set; } = null!;
        public AccountTag Tag { get; set; } = null!;
    }

    public class Ticket
    {
        public ObjectId Id { get; set; }
        public string Label { get; set; } = null!;
        public ObjectId? AccountId { get; set; }
        public Account? Account { get; set; }
    }

    private sealed class TicketContext(DbContextOptions options, string accounts, string tickets) : DbContext(options)
    {
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Ticket> Tickets => Set<Ticket>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Account>(b =>
            {
                b.ToCollection(accounts);
                b.ComplexProperty(a => a.Ref, r => r.ComplexProperty(x => x.Inner));
                b.OwnsOne(a => a.Tag);
            });
            mb.Entity<Ticket>(b =>
            {
                b.ToCollection(tickets);
                b.HasOne(t => t.Account).WithMany().HasForeignKey(t => t.AccountId);
            });
        }
    }

    private static readonly ObjectId AccountA = ObjectId.GenerateNewId();
    private static readonly ObjectId AccountB = ObjectId.GenerateNewId();

    // Ann (_id A) points at B in every hop (Ref.Id, Ref.Inner.Id, Tag.Id) and Bob (_id B) at A. t1 references A, t2 B,
    // t3 nothing. Joining a hop key on the navigation's `_id` instead swaps the names.
    private Func<MongoQueryMode, List<string>> Tickets(
        Func<TicketContext, IEnumerable<string>> query, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var accounts = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + suffix + "_a";
        var tickets = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + suffix + "_t";
        BsonDocument Points(ObjectId to) => new() { { "Id", to }, { "Inner", new BsonDocument("Id", to) } };
        database.MongoDatabase.GetCollection<BsonDocument>(accounts).InsertMany(
        [
            new BsonDocument { { "_id", AccountA }, { "Name", "Ann" }, { "Ref", Points(AccountB) }, { "Tag", new BsonDocument("Id", AccountB) } },
            new BsonDocument { { "_id", AccountB }, { "Name", "Bob" }, { "Ref", Points(AccountA) }, { "Tag", new BsonDocument("Id", AccountA) } }
        ]);
        database.MongoDatabase.GetCollection<BsonDocument>(tickets).InsertMany(
        [
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Label", "t1" }, { "AccountId", AccountA } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Label", "t2" }, { "AccountId", AccountB } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Label", "t3" }, { "AccountId", BsonNull.Value } }
        ]);
        return mode =>
        {
            var builder = new DbContextOptionsBuilder<TicketContext>()
                .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
                .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
            using var db = new TicketContext(builder.Options, accounts, tickets);
            return query(db).ToList();
        };
    }

    // The outer key `t.AccountId` is the Ticket.Account FK, and the inner hop key's leaf is named `Id` like Account's PK, so a
    // simple-name check accepted the navigation's $lookup (`_id`) and NativeOnly served the swapped rows (t1|Ann, t2|Bob).
    // Correct: t1 (A) -> Bob (whose hops hold A), t2 (B) -> Ann.
    public static TheoryData<string> InnerHopKeyShapes => ["complex", "complex_multi_hop", "owned", "left_join"];

    [Theory]
    [MemberData(nameof(InnerHopKeyShapes))]
    public void Inner_hop_key_named_like_the_navigations_principal_key_does_not_use_the_navigation(string shape)
        => Declines(Tickets(db => shape switch
            {
                "complex" => [.. db.Tickets.Join(db.Accounts, t => t.AccountId, a => (ObjectId?)a.Ref.Id, (t, a) => t.Label + "|" + a.Name).ToList().Order()],
                "complex_multi_hop" => [.. db.Tickets.Join(db.Accounts, t => t.AccountId, a => (ObjectId?)a.Ref.Inner.Id, (t, a) => t.Label + "|" + a.Name).ToList().Order()],
                // An owned type may carry a CLR property named Id (EF maps it as an ordinary property; the owned key is shadow).
                "owned" => [.. db.Tickets.Join(db.Accounts, t => t.AccountId, a => (ObjectId?)a.Tag.Id, (t, a) => t.Label + "|" + a.Name).ToList().Order()],
                _ => [.. (from t in db.Tickets
                        join a in db.Accounts on t.AccountId equals (ObjectId?)a.Ref.Id into g
                        from a in g.DefaultIfEmpty()
                        select t.Label + "|" + (a == null ? "-" : a.Name)).ToList().Order().Where(x => x != "t3|-")]
            }, nameof(Inner_hop_key_named_like_the_navigations_principal_key_does_not_use_the_navigation) + shape),
            "t1|Bob", "t2|Ann");

    [Fact]
    public void Direct_inner_key_on_a_navigation_join_stays_native()
        => Native(Tickets(db => [.. db.Tickets.Join(db.Accounts, t => t.AccountId, a => (ObjectId?)a.Id, (t, a) => t.Label + "|" + a.Name).ToList().Order()]),
            "t1|Ann", "t2|Bob");

    [Fact]
    public void GroupJoin_count_over_an_inner_hop_key()
        // Account has no collection navigation back to Ticket, so the M35 count binder can't bind it; EF refuses the
        // correlated count in every mode. Never rows. Correct answer would be t1 1, t2 1, t3 0.
        => PerMode(Tickets(db => [.. db.Tickets.GroupJoin(db.Accounts, t => t.AccountId, a => (ObjectId?)a.Ref.Id, (t, g) => new { t.Label, N = g.Count() })
                .ToList().Select(x => x.Label + "|" + x.N).Order()]),
            ["t1|1", "t2|1", "t3|0"], Throws<InvalidOperationException>("could not be translated"), Throws<InvalidOperationException>("could not be translated"), Throws<InvalidOperationException>("could not be translated"));

#if !EF8 && !EF9
    [Fact]
    public void Inner_hop_key_LeftJoin_operator_does_not_use_the_navigation()
        => Declines(Tickets(db => [.. db.Tickets.LeftJoin(db.Accounts, t => t.AccountId, a => (ObjectId?)a.Ref.Id, (t, a) => t.Label + "|" + (a == null ? "-" : a.Name))
                .ToList().Order()]),
            "t1|Bob", "t2|Ann", "t3|-");
#endif
}
