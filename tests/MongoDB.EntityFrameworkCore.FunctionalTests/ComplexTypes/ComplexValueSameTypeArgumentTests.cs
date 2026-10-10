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
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.CompositionAssert;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// Several whole complex values of the SAME CLR type, stored alike, in one projection (spec known limitation 29): every
/// construction shape (positional constructor, record, member-init DTO, anonymous type, tuple), over required, optional
/// (EF10) and struct complex values, through a same-type reference navigation and a join, with the owned analogue as the
/// control. Every row differs from the others in every leaf, so a swap or a duplication is visible. Every answer is
/// hand-written.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexValueSameTypeArgumentTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public class Addr
    {
        public string City { get; set; } = null!;
        public string? Street { get; set; }
    }

    public struct Pt
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class Site
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public ObjectId? ReferrerId { get; set; }
        public Site? Referrer { get; set; }
        public Addr Home { get; set; } = null!;
        public Addr Work { get; set; } = null!;
        public Addr Alt { get; set; } = null!;
        public Pt HomePt { get; set; }
        public Pt WorkPt { get; set; }
#if !EF8 && !EF9
        public Addr? Opt { get; set; }
#endif
    }

    public record P(Addr A, Addr B);
    public record P3(Addr A, Addr B, Addr C);
    public record PPt(Pt A, Pt B);
    public record PMixed(Addr A, string N);
    public record PMixedReversed(string N, Addr A);

    public class PCtor(Addr a, Addr b)
    {
        public Addr First { get; } = a;
        public Addr Second { get; } = b;
    }

    public class Dto
    {
        public Addr? A { get; set; }
        public Addr? B { get; set; }
    }

    private sealed class SiteContext(DbContextOptions options, string collection) : DbContext(options)
    {
        public DbSet<Site> Sites => Set<Site>();

        protected override void OnModelCreating(ModelBuilder mb)
            => mb.Entity<Site>(b =>
            {
                b.ToCollection(collection);
                b.HasOne(s => s.Referrer).WithMany().HasForeignKey(s => s.ReferrerId);
                b.ComplexProperty(s => s.Home);
                b.ComplexProperty(s => s.Work);
                b.ComplexProperty(s => s.Alt);
                b.ComplexProperty(s => s.HomePt);
                b.ComplexProperty(s => s.WorkPt);
#if !EF8 && !EF9
                b.ComplexProperty(s => s.Opt);
#endif
            });
    }

    private static readonly ObjectId RootId = ObjectId.GenerateNewId();

    // Root (H1/hs1, W1/ws1, A1/as1, pt 1,2 / 3,4, Opt O1); Leaf (referrer Root; H2/null, W2/ws2, A2/as2, pt 5,6 / 7,8,
    // Opt null).
    private Func<MongoQueryMode, List<string>> Run(Func<SiteContext, IEnumerable<string>> query,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collection = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + Guid.NewGuid().ToString("N")[..8];
        static BsonDocument A(string city, string? street)
            => new() { { "City", city }, { "Street", street == null ? BsonNull.Value : street } };
        static BsonDocument Point(int x, int y) => new() { { "X", x }, { "Y", y } };
        database.MongoDatabase.GetCollection<BsonDocument>(collection).InsertMany(
        [
            new BsonDocument
            {
                { "_id", RootId }, { "Name", "Root" }, { "ReferrerId", BsonNull.Value },
                { "Home", A("H1", "hs1") }, { "Work", A("W1", "ws1") }, { "Alt", A("A1", "as1") },
                { "HomePt", Point(1, 2) }, { "WorkPt", Point(3, 4) }, { "Opt", A("O1", "os1") }
            },
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "Name", "Leaf" }, { "ReferrerId", RootId },
                { "Home", A("H2", null) }, { "Work", A("W2", "ws2") }, { "Alt", A("A2", "as2") },
                { "HomePt", Point(5, 6) }, { "WorkPt", Point(7, 8) }, { "Opt", BsonNull.Value }
            }
        ]);
        return mode =>
        {
            var builder = new DbContextOptionsBuilder<SiteContext>()
                .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
                .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(builder).UseQueryMode(mode);
            using var db = new SiteContext(builder.Options, collection);
            return query(db).ToList();
        };
    }

    private static string F(Addr? a) => a == null ? "null" : $"{a.City}/{a.Street ?? "-"}";
    private static string F(Pt p) => $"{p.X},{p.Y}";

    private static IQueryable<Site> Sites(SiteContext db) => db.Sites.OrderBy(s => s.Name);

    // ── Positional constructions ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Record_of_two_same_type_values()
        => PerMode(Run(db => Sites(db).Select(s => new P(s.Home, s.Work)).ToList().Select(p => F(p.A) + "|" + F(p.B))),
            ["H2/-|W2/ws2", "H1/hs1|W1/ws1"], NotNative, Serves, Serves);

    [Fact]
    public void Record_of_two_same_type_values_in_swapped_order()
        => PerMode(Run(db => Sites(db).Select(s => new P(s.Work, s.Home)).ToList().Select(p => F(p.A) + "|" + F(p.B))),
            ["W2/ws2|H2/-", "W1/ws1|H1/hs1"], NotNative, Serves, Serves);

    [Fact]
    public void Record_of_three_same_type_values()
        => PerMode(Run(db => Sites(db).Select(s => new P3(s.Home, s.Work, s.Alt)).ToList().Select(p => F(p.A) + "|" + F(p.B) + "|" + F(p.C))),
            ["H2/-|W2/ws2|A2/as2", "H1/hs1|W1/ws1|A1/as1"], NotNative, Serves, Serves);

    [Fact]
    public void Constructor_with_renamed_members_of_two_same_type_values()
        => PerMode(Run(db => Sites(db).Select(s => new PCtor(s.Home, s.Work)).ToList().Select(p => F(p.First) + "|" + F(p.Second))),
            ["H2/-|W2/ws2", "H1/hs1|W1/ws1"], NotNative, Serves, Serves);

    [Fact]
    public void Record_of_two_same_type_struct_values()
        => PerMode(Run(db => Sites(db).Select(s => new PPt(s.HomePt, s.WorkPt)).ToList().Select(p => F(p.A) + "|" + F(p.B))),
            ["5,6|7,8", "1,2|3,4"], NotNative, Serves, Serves);

    [Fact]
    public void Tuple_create_of_two_same_type_values()
        => PerMode(Run(db => Sites(db).Select(s => Tuple.Create(s.Home, s.Work)).ToList().Select(p => F(p.Item1) + "|" + F(p.Item2))),
            ["H2/-|W2/ws2", "H1/hs1|W1/ws1"], NotNative, Serves, Serves);

    // Out of scope of limitation 29, and loud: the scalar argument shares the construction's projection member, so the
    // fallback's member-less construction refusal fires (in either argument order).
    [Fact]
    public void Record_of_a_complex_value_and_a_scalar_fails_loudly()
    {
        PerMode(Run(db => Sites(db).Select(s => new PMixed(s.Home, s.Name)).ToList().Select(p => F(p.A) + "|" + p.N)),
            [], NotNative, Throws<InvalidOperationException>("The projection constructs 'PMixed' from arguments that can't each be read"),
            Throws<InvalidOperationException>("The projection constructs 'PMixed' from arguments that can't each be read"));
        PerMode(Run(db => Sites(db).Select(s => new PMixedReversed(s.Name, s.Home)).ToList().Select(p => p.N + "|" + F(p.A)), "Reversed"),
            [], NotNative, Throws<InvalidOperationException>("The projection constructs 'PMixedReversed' from arguments that can't each be read"),
            Throws<InvalidOperationException>("The projection constructs 'PMixedReversed' from arguments that can't each be read"));
    }

    // ── Member-named constructions ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Member_init_dto_of_two_same_type_values()
        => Native(Run(db => Sites(db).Select(s => new Dto { A = s.Home, B = s.Work }).ToList().Select(p => F(p.A) + "|" + F(p.B))),
            "H2/-|W2/ws2", "H1/hs1|W1/ws1");

    [Fact]
    public void Anonymous_type_of_two_same_type_values()
        => Native(Run(db => Sites(db).Select(s => new { s.Home, s.Work }).ToList().Select(p => F(p.Home) + "|" + F(p.Work))),
            "H2/-|W2/ws2", "H1/hs1|W1/ws1");

    [Fact]
    public void Anonymous_type_of_two_same_type_struct_values()
        => Native(Run(db => Sites(db).Select(s => new { s.HomePt, s.WorkPt }).ToList().Select(p => F(p.HomePt) + "|" + F(p.WorkPt))),
            "5,6|7,8", "1,2|3,4");

#if !EF8 && !EF9
    [Fact]
    public void Record_of_an_optional_and_a_required_same_type_value()
        => PerMode(Run(db => Sites(db).Select(s => new P(s.Opt!, s.Work)).ToList().Select(p => F(p.A) + "|" + F(p.B))),
            ["null|W2/ws2", "O1/os1|W1/ws1"], NotNative, Serves, Serves);

    [Fact]
    public void Anonymous_type_of_an_optional_and_a_required_same_type_value()
        => Native(Run(db => Sites(db).Select(s => new { s.Opt, s.Work }).ToList().Select(p => F(p.Opt) + "|" + F(p.Work))),
            "null|W2/ws2", "O1/os1|W1/ws1");
#endif

    // ── Through a same-type reference navigation ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Record_of_own_and_referrer_value_of_the_same_type()
        => PerMode(Run(db => Sites(db).Where(s => s.ReferrerId != null).Select(s => new P(s.Home, s.Referrer!.Home)).ToList()
                .Select(p => F(p.A) + "|" + F(p.B))),
            ["H2/-|H1/hs1"], NotNative, Serves, Serves);

    // ── Through a join ───────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Record_of_outer_and_inner_value_of_the_same_type_through_a_join()
        => PerMode(Run(db => db.Sites.Join(db.Sites, l => l.ReferrerId, r => (ObjectId?)r.Id, (l, r) => new P(l.Work, r.Work)).ToList()
                .Select(p => F(p.A) + "|" + F(p.B))),
            ["W2/ws2|W1/ws1"], NotNative, Serves, Serves);

    [Fact]
    public void Anonymous_type_of_outer_and_inner_value_of_the_same_type_through_a_join()
        => Declines(Run(db => db.Sites.Join(db.Sites, l => l.ReferrerId, r => (ObjectId?)r.Id, (l, r) => new { L = l.Work, R = r.Work }).ToList()
                .Select(p => F(p.L) + "|" + F(p.R))),
            "W2/ws2|W1/ws1");

    [Fact]
    public void Anonymous_type_of_own_and_referrer_value_of_the_same_type()
        => Declines(Run(db => Sites(db).Where(s => s.ReferrerId != null).Select(s => new { s.Home, R = s.Referrer!.Home }).ToList()
                .Select(p => F(p.Home) + "|" + F(p.R))),
            "H2/-|H1/hs1");
}
