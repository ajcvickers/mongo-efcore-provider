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
using MongoDB.EntityFrameworkCore.Infrastructure;
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.Composition;
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.CompositionAssert;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Complex-property LEAVES composed with set operations, <c>Distinct</c>, entity-level operators, paging, global query
/// filters and <c>Include</c>, over the <see cref="Composition"/> model: two complex properties of one CLR type stored
/// differently (<c>Billing</c> under CLR names; <c>Shipping</c> as <c>ship</c> with City as <c>town</c> and a
/// string-converted Code), so an operand read from the wrong path, or through the wrong serializer, shows.
/// </summary>
/// <remarks>
/// Each shape is pinned per mode against a hand-written answer (<see cref="CompositionAssert"/>). Whole complex values as
/// operands of value-reading operators are refused (ruling R7) and pinned in <see cref="ComplexTypeMaterializationTests"/>;
/// joins are in <see cref="ComplexTypeJoinCompositionTests"/>, hierarchies and owned/shared types in
/// <see cref="ComplexTypeHierarchyCompositionTests"/>.
/// </remarks>
[XUnitCollection("QueryTests")]
public class ComplexTypeCompositionTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    private Func<MongoQueryMode, List<string>> Clients(
        Func<ShopContext, IEnumerable<string>> query, Filter filter = Filter.None,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collections = Seed(database, name + Guid.NewGuid().ToString("N")[..4]);
        return mode =>
        {
            using var db = Create(database, collections, mode, filter);
            return query(db).ToList();
        };
    }

    private static List<string> Sorted<T>(IQueryable<T> query) => [.. query.ToList().Select(x => x?.ToString() ?? "<null>").Order(StringComparer.Ordinal)];

    // ── Set operations over complex leaves ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Concat_of_the_same_complex_leaf()
        => Native(Clients(db => Sorted(db.Clients.Select(c => c.Billing.City).Concat(db.Clients.Select(c => c.Billing.City)))),
            "Hidden", "Hidden", "Oslo", "Oslo", "Paris", "Paris", "Rome", "Rome");

    [Fact]
    public void Union_and_Concat_of_two_complex_paths_of_the_same_clr_type_stored_under_different_element_names()
    {
        // Billing.City is stored as `Billing.City`, Shipping.City as `ship.town`: source2 must be re-aliased, never read as
        // source1's path.
        Native(Clients(db => Sorted(db.Clients.Select(c => c.Billing.City).Union(db.Clients.Select(c => c.Shipping.City)))),
            "Hidden", "Oslo", "Paris", "Rome");
        Native(Clients(db => Sorted(db.Clients.Where(c => c.Rank < 3).Select(c => c.Billing.City)
                .Concat(db.Clients.Where(c => c.Rank > 2).Select(c => c.Shipping.City)))),
            "Paris", "Paris", "Paris", "Rome");
    }

    [Fact]
    public void Intersect_and_Except_of_two_complex_paths()
    {
        // No driver-LINQ oracle for Intersect/Except (Query AGENTS.md): pinned as native, DriverLinq refusing.
        PerMode(Clients(db => Sorted(db.Clients.Select(c => c.Billing.City).Intersect(db.Clients.Select(c => c.Shipping.City)))),
            ["Oslo", "Paris", "Rome"], Serves, Serves, "not supported||Expression not supported");
        PerMode(Clients(db => Sorted(db.Clients.Select(c => c.Billing.City).Except(db.Clients.Select(c => c.Shipping.City)))),
            ["Hidden"], Serves, Serves, "not supported||Expression not supported");
    }

    [Fact]
    public void Union_of_struct_and_nullable_complex_leaves()
    {
        Native(Clients(db => Sorted(db.Clients.Select(c => c.Billing.Geo.Lat).Union(db.Clients.Select(c => c.Shipping.Geo.Lat)))),
            "1", "2", "3", "5");
        // Billing.Zip: 75, BSON null, missing, missing; Shipping.Zip: missing, missing, 9, missing. A BSON null and a MISSING
        // value are two distinct stored rows to the $group dedup, so null appears twice, in every mode (driver-LINQ is the
        // oracle; a root nullable scalar answers the same: pre-existing, and the Distinct missing-marker rule of Query
        // AGENTS.md keeps the two apart on purpose).
        Native(Clients(db => Sorted(db.Clients.Select(c => c.Billing.Zip).Union(db.Clients.Select(c => c.Shipping.Zip)))),
            "75", "9", "<null>", "<null>");
        Native(Clients(db => Sorted(db.Clients.Select(c => c.Billing.Zip).Concat(db.Clients.Select(c => c.Shipping.Zip)))),
            "75", "9", "<null>", "<null>", "<null>", "<null>", "<null>", "<null>");
    }

    [Fact]
    public void Union_of_anonymous_projections_of_complex_leaves_from_two_paths()
        => Native(Clients(db => Sorted(db.Clients.Select(c => new { c.Billing.City, c.Billing.Geo.Lat })
                .Union(db.Clients.Select(c => new { c.Shipping.City, c.Shipping.Geo.Lat })))),
            "{ City = Hidden, Lat = 5 }", "{ City = Oslo, Lat = 1 }", "{ City = Paris, Lat = 1 }", "{ City = Paris, Lat = 3 }",
            "{ City = Paris, Lat = 5 }", "{ City = Rome, Lat = 2 }", "{ City = Rome, Lat = 3 }");

    [Fact]
    public void Union_of_a_complex_leaf_with_a_root_scalar_and_with_another_collections_complex_leaf()
    {
        // Across two collections there is no driver-LINQ oracle (cross-DbSet): native serves, DriverLinq refuses.
        PerMode(Clients(db => Sorted(db.Clients.Select(c => c.Billing.City).Union(db.Orders.Select(o => o.City)))),
            ["Hidden", "Lima", "Oslo", "Paris", "Rome"], Serves, Serves, "cross-DbSet");
        PerMode(Clients(db => Sorted(db.Clients.Where(c => c.Rank < 3).Select(c => c.Shipping.City)
                .Concat(db.Orders.Where(o => o.Rank > 3).Select(o => o.Ship.City)))),
            ["Hidden", "Oslo", "Oslo", "Rome"], Serves, Serves, "cross-DbSet");
        Native(Clients(db => Sorted(db.Clients.Select(c => new { c.Name, c.Billing.City })
                .Union(db.Clients.Where(c => c.Shipping.Geo.Lat > 2).Select(c => new { c.Name, City = c.Shipping.City })))),
            "{ Name = Ann, City = Paris }", "{ Name = Bob, City = Rome }", "{ Name = Cid, City = Oslo }", "{ Name = Cid, City = Paris }",
            "{ Name = Hid, City = Hidden }", "{ Name = Hid, City = Paris }");
    }

    [Fact]
    public void Set_operation_over_complex_leaves_stored_differently_declines_natively()
    {
        // Billing.Code is an int; Shipping.Code is stored as a string (HasConversion<string>): the operands are not
        // stored alike (OperandSerializationsMatch / StoredSerialization.StoredAlike), so the native set op declines. C#
        // answers 1, 2, 3, 4; the driver-LINQ fallback dedups the STORED forms (2 and "2" differ) and answers every value
        // twice. PRE-EXISTING, not complex-specific: an int and a converted root scalar of one entity answer the same way
        // (measured: `[1; 1]`). Jira candidate (not filed); pinned so a fix is noticed.
        PerMode(Clients(db => Sorted(db.Clients.Select(c => c.Billing.Code).Union(db.Clients.Select(c => c.Shipping.Code)))),
            ["1", "1", "2", "2", "3", "3", "4", "4"], NotNative, Serves, Serves);
    }

    // ── Distinct over complex leaves ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Distinct_over_anonymous_and_struct_complex_leaves()
    {
        Native(Clients(db => Sorted(db.Clients.Select(c => new { c.Shipping.City }).Distinct())),
            "{ City = Oslo }", "{ City = Paris }", "{ City = Rome }");
        Native(Clients(db => Sorted(db.Clients.Select(c => new { c.Billing.Geo.Lat, c.Billing.Geo.Lon }).Distinct())),
            "{ Lat = 1, Lon = 10 }", "{ Lat = 3, Lon = 30 }", "{ Lat = 5, Lon = 60 }");
        Native(Clients(db => Sorted(db.Clients.Select(c => new { B = c.Billing.City, S = c.Shipping.City, c.Shipping.Zip }).Distinct())),
            "{ B = Hidden, S = Paris, Zip =  }", "{ B = Oslo, S = Paris, Zip = 9 }", "{ B = Paris, S = Rome, Zip =  }", "{ B = Rome, S = Oslo, Zip =  }");
        Native(Clients(db => [.. db.Clients.Select(c => c.Shipping.Geo.Lat).Distinct().OrderBy(x => x).ToList().Select(x => x.ToString())]),
            "1", "2", "3", "5");
        Native(Clients(db => [db.Clients.Select(c => c.Shipping.City).Distinct().Count().ToString()]), "3");
    }

    [Fact]
    public void Distinct_over_a_converted_complex_leaf()
        => PerMode(Clients(db => Sorted(db.Clients.Select(c => c.Shipping.Code).Distinct())),
            ["1", "2", "3", "4"], NotNative, Serves, Serves);

    // ── Entity-level operators with complex-leaf predicates ─────────────────────────────────────────────────────

    private const string Ann = "Ann|Paris/1/75/1|Rome/2/-/2";
    private const string Bob = "Bob|Rome/3/-/2|Oslo/1/-/1";
    private const string Cid = "Cid|Oslo/1/-/3|Paris/3/9/3";
    private const string Hid = "Hid|Hidden/5/-/4|Paris/5/-/4";

    private static string F(Client? c) => c == null ? "<none>" : Fmt(c);

    [Fact]
    public void FirstOrDefault_Single_SingleOrDefault_and_Last_with_complex_leaf_predicates_materialize_the_entity()
    {
        Native(Clients(db => [F(db.Clients.OrderBy(c => c.Name).FirstOrDefault(c => c.Shipping.City == "Paris"))]), Cid);
        Native(Clients(db => [F(db.Clients.FirstOrDefault(c => c.Shipping.City == "Lima"))]), "<none>");
        Native(Clients(db => [F(db.Clients.Single(c => c.Billing.City == "Rome"))]), Bob);
        Native(Clients(db => [F(db.Clients.SingleOrDefault(c => c.Shipping.Geo.Lon == 60))]), Hid);
        Native(Clients(db => [F(db.Clients.SingleOrDefault(c => c.Billing.Zip == 9))]), "<none>");
        Native(Clients(db => [F(db.Clients.OrderBy(c => c.Billing.Geo.Lat).ThenBy(c => c.Name).Last())]), Hid);
        Native(Clients(db => [F(db.Clients.OrderBy(c => c.Name).LastOrDefault(c => c.Billing.Geo.Lat == 1))]), Cid);
        // Two rows match: Single throws EF's "Sequence contains more than one element" in every mode, never a row.
        PerMode(Clients(db => [F(db.Clients.Single(c => c.Shipping.City == "Paris"))]), [], "more than one element", "more than one element",
            "more than one element");
    }

    [Fact]
    public void Count_LongCount_Any_All_and_Contains_with_complex_leaves()
    {
        Native(Clients(db => [db.Clients.Count(c => c.Shipping.City == "Paris").ToString()]), "2");
        Native(Clients(db => [db.Clients.LongCount(c => c.Billing.Geo.Lat < 2).ToString()]), "2");
        Native(Clients(db => [db.Clients.Any(c => c.Shipping.Zip == 9).ToString()]), "True");
        Native(Clients(db => [db.Clients.Any(c => c.Billing.City == "Lima").ToString()]), "False");
        Native(Clients(db => [db.Clients.All(c => c.Shipping.Geo.Lon >= 20).ToString()]), "True");
        Native(Clients(db => [db.Clients.All(c => c.Billing.Zip != null).ToString()]), "False");
        Native(Clients(db => [db.Clients.Select(c => c.Shipping.City).Contains("Oslo").ToString()]), "True");
        Native(Clients(db => [db.Clients.Select(c => c.Billing.City).Contains("Lima").ToString()]), "False");
        Native(Clients(db => [db.Clients.Select(c => c.Billing.Geo.Lat).Contains(5.0).ToString()]), "True");
    }

    // ── Paging with complex-leaf ordering ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void Skip_and_Take_with_complex_leaf_ordering_over_whole_entities()
    {
        Native(Clients(db => [.. db.Clients.OrderBy(c => c.Shipping.Geo.Lon).Skip(1).Take(2).ToList().Select(Fmt)]), Bob, Cid);
        Native(Clients(db => [.. db.Clients.OrderByDescending(c => c.Billing.City).ThenBy(c => c.Shipping.City).Skip(1).Select(c => c.Name)]),
            "Ann", "Cid", "Hid");
        Native(Clients(db => [.. db.Clients.Where(c => c.Shipping.City == "Paris").OrderByDescending(c => c.Shipping.Geo.Lat).Take(1).ToList().Select(Fmt)]),
            Hid);
        Native(Clients(db => [.. db.Clients.OrderBy(c => c.Billing.Geo.Lat).ThenByDescending(c => c.Name).Skip(1).Take(2)
                .Select(c => new { c.Name, c.Shipping.City }).ToList().Select(x => x.Name + ":" + x.City)]),
            "Ann:Rome", "Bob:Oslo");
    }

    // ── Global query filters over complex leaves ────────────────────────────────────────────────────────────────

    [Fact]
    public void Query_filter_on_a_complex_leaf_with_and_without_IgnoreQueryFilters()
    {
        Native(Clients(db => [.. db.Clients.OrderBy(c => c.Name).ToList().Select(Fmt)], Filter.ClientBillingCity), Ann, Bob, Cid);
        Native(Clients(db => [.. db.Clients.IgnoreQueryFilters().OrderBy(c => c.Name).Select(c => c.Name)], Filter.ClientBillingCity),
            "Ann", "Bob", "Cid", "Hid");
        Native(Clients(db => [.. db.Clients.Where(c => c.Shipping.City == "Paris").Select(c => c.Name)], Filter.ClientBillingCity), "Cid");
        Native(Clients(db => [db.Clients.Count().ToString()], Filter.ClientBillingCity), "3");
        Native(Clients(db => Sorted(db.Clients.Select(c => c.Billing.City).Distinct()), Filter.ClientBillingCity), "Oslo", "Paris", "Rome");
        PerMode(Clients(db => Sorted(db.Clients.Select(c => c.Shipping.City).Union(db.Orders.Select(o => o.Ship.City))), Filter.OrderShipCity),
            ["Oslo", "Paris", "Rome"], Serves, Serves, "cross-DbSet");
    }

    [Fact]
    public void Query_filter_on_a_related_entitys_complex_leaf()
    {
        // o.Client!.Billing.City != "Hidden": o4 has no client (kept: C# null != "Hidden"), o5 belongs to Hid (filtered). A
        // complex leaf of a reference navigation declines natively (as an owned-hop leaf does; pre-existing).
        Declines(Clients(db => [.. db.Orders.OrderBy(o => o.Rank).Select(o => o.Rank.ToString())], Filter.OrderClientBillingCity),
            "1", "2", "3", "5");
        Native(Clients(db => [.. db.Orders.IgnoreQueryFilters().OrderBy(o => o.Rank).Select(o => o.Rank.ToString())], Filter.OrderClientBillingCity),
            "1", "2", "3", "4", "5");
    }

    // ── Include beside complex properties ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void Collection_Include_with_complex_leaf_filter_and_ordering()
    {
        foreach (var tracking in new[] { QueryTrackingBehavior.TrackAll, QueryTrackingBehavior.NoTracking })
        {
            var collections = Seed(database, nameof(Collection_Include_with_complex_leaf_filter_and_ordering) + tracking);
            Native(mode =>
                {
                    using var db = Create(database, collections, mode, tracking: tracking);
                    var rows = db.Clients.Include(c => c.Orders).Where(c => c.Billing.City != "Oslo").OrderBy(c => c.Shipping.Geo.Lat).ToList()
                        .Select(c => Fmt(c) + "|" + string.Join(",", c.Orders.Select(o => o.Rank).Order())).ToList();
                    Assert.Equal(tracking == QueryTrackingBehavior.TrackAll ? 7 : 0, db.ChangeTracker.Entries().Count());
                    return rows;
                },
                Bob + "|5", Ann + "|1,2", Hid + "|4");
        }
    }

    [Fact]
    public void Reference_Include_with_complex_leaf_filter_on_both_entities()
    {
        Native(Clients(db => [.. db.Orders.Include(o => o.Client).Where(o => o.Ship.City == "Paris").OrderBy(o => o.Rank).ToList()
                .Select(o => Fmt(o) + "#" + F(o.Client))]),
            "2|Lima|Paris/2/-/0#" + Ann, "3|Oslo|Paris/4/-/0#<none>");
        // A filter on the INCLUDED entity declines natively (pre-existing: the same for a scalar, `o.Client!.Name == "Ann"`).
        Declines(Clients(db => [.. db.Orders.Include(o => o.Client).Where(o => o.Client!.Shipping.City == "Rome").OrderBy(o => o.Rank).ToList()
                .Select(o => o.Rank + "#" + F(o.Client))]),
            "1#" + Ann, "2#" + Ann);
    }

    [Fact]
    public void Complex_leaves_of_a_reference_navigation_in_projection_predicate_and_ordering()
    {
        // A hop under a reference navigation declines natively (as an owned hop does; pre-existing). Before this task the
        // fallback read `o.Client!.Shipping.City` under its CLR name and answered empty (bridge complex-hop fix).
        Declines(Clients(db => [.. db.Orders.Where(o => o.Client!.Billing.City == "Paris").OrderBy(o => o.Rank)
                .Select(o => new { o.Rank, C = o.Client!.Shipping.City, o.Ship.City }).ToList().Select(x => $"{x.Rank}|{x.C}|{x.City}")]),
            "1|Rome|Rome", "2|Rome|Paris");
        Declines(Clients(db => [.. db.Orders.OrderBy(o => o.Rank).Select(o => new { o.Rank, Z = o.Client!.Billing.Zip }).ToList()
                .Select(x => $"{x.Rank}|{x.Z?.ToString() ?? "-"}")]),
            "1|75", "2|75", "3|-", "4|-", "5|-");
        Declines(Clients(db => [.. db.Orders.Where(o => o.ClientId != null).OrderBy(o => o.Client!.Shipping.Geo.Lon).ThenBy(o => o.Rank).Select(o => o.Rank.ToString())]),
            // o4's client doesn't exist: its Lon is missing and sorts first.
            "3", "1", "2", "5", "4");
    }

    [Fact]
    public void Filtered_Include_ordered_by_a_complex_leaf_sorts_by_the_leaf_not_a_same_named_root_property()
    {
        // Order has a root City AND Ship.City. The filtered-Include $lookup's $sort resolved the key by simple name and
        // sorted by the root `City` (silent wrong order, every mode). Ann's orders, with o2 re-seeded so the two orders
        // disagree: o1 (rank 1) City Paris / Ship Rome, o2 (rank 2) City Lima -> Zurich / Ship Paris. By Ship.City
        // ascending: o2 (Paris), o1 (Rome) = 2,1; by root City: o1 (Paris), o2 (Zurich) = 1,2.
        var collections = Seed(database, nameof(Filtered_Include_ordered_by_a_complex_leaf_sorts_by_the_leaf_not_a_same_named_root_property));
        database.MongoDatabase.GetCollection<MongoDB.Bson.BsonDocument>(collections.Orders).UpdateOne(
            new MongoDB.Bson.BsonDocument("City", "Lima"), new MongoDB.Bson.BsonDocument("$set", new MongoDB.Bson.BsonDocument("City", "Zurich")));
        Native(mode =>
            {
                using var db = Create(database, collections, mode);
                return [.. db.Clients.Where(c => c.Name == "Ann").Include(c => c.Orders.OrderBy(o => o.Ship.City)).ToList()
                    .Select(c => string.Join(",", c.Orders.Select(o => o.Rank)))];
            },
            "2,1");
    }
}
