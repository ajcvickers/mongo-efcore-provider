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

#if !EF8 && !EF9
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation;

/// <summary>
/// Ruling R20 scanner: which captured chains name a null-guard-requiring shape inside a COMPLEX collection's element scope
/// (refused on the Native-mode fallback), and which don't (owned elements, root predicates, guard-free predicates).
/// </summary>
public class ComplexElementNullGuardRefusalTests
{
    public class Stop
    {
        public string City { get; set; } = null!;
        public string? Note { get; set; }
        public int Floor { get; set; }
        public int? Zip { get; set; }
        public bool Verified { get; set; }
        public DateTime When { get; set; }
    }

    public class Route
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime Departs { get; set; }
        public List<Stop> Stops { get; set; } = [];
        public List<Stop> Owned { get; set; } = [];
    }

    private static readonly IModel Model = BuildModel();

    private static IModel BuildModel()
    {
        using var db = new RouteContext();
        return db.Model;
    }

    private static Expression Query(Expression<Func<IQueryable<Route>, IQueryable<Route>>> query) => query.Body;

    private static Expression Scalar<T>(Expression<Func<IQueryable<Route>, T>> query) => query.Body;

    [Theory]
    [MemberData(nameof(RefusedShapes))]
    public void Null_guard_requiring_shapes_in_a_complex_element_scope_are_found(string label, Expression captured, string shape)
    {
        var found = ComplexElementNullGuardRefusal.Find(captured, Model);
        Assert.True(found is not null, label);
        Assert.Equal(shape, found!.Value.Shape);
        Assert.Equal("Route.Stops", found.Value.Collection);
    }

    public static IEnumerable<object[]> RefusedShapes()
    {
        yield return ["date part over date-add", Query(q => q.Where(r => r.Stops.Any(s => s.When.AddDays(1).Year < 2030))), "a relational comparison"];
        yield return ["relational on a leaf", Query(q => q.Where(r => r.Stops.Any(s => s.Floor < 1))), "a relational comparison"];
        yield return ["relational, element on the right", Query(q => q.Where(r => r.Stops.Any(s => 1 > s.Floor))), "a relational comparison"];
        yield return ["All", Query(q => q.Where(r => r.Stops.All(s => s.When.AddDays(1).Year < 2030))), "a relational comparison"];
        yield return ["Count(pred) projection", Scalar(q => q.Select(r => r.Stops.Count(s => s.Floor < 1))), "a relational comparison"];
        yield return ["Where(pred).Any()", Query(q => q.Where(r => r.Stops.Where(s => s.Floor < 1).Any())), "a relational comparison"];
        yield return ["== null", Query(q => q.Where(r => r.Stops.Any(s => s.Note == null))), "a comparison with null"];
        yield return ["!= null", Query(q => q.Where(r => r.Stops.Any(s => s.Zip != null))), "a comparison with null"];
        yield return ["local list Contains", Query(q => q.Where(r => r.Stops.Any(s => new int?[] { null, 7 }.Contains(s.Zip)))), "a membership test of a member value in a local collection"];
        yield return ["bool == false", Query(q => q.Where(r => r.Stops.Any(s => s.Verified == false))), "an equality on a non-nullable bool member"];
        yield return ["nested under &&", Query(q => q.Where(r => r.Stops.Any(s => s.City == "X" && s.Floor < 1))), "a relational comparison"];
        yield return ["nested under !", Query(q => q.Where(r => r.Stops.Any(s => !(s.Floor > 9)))), "a relational comparison"];
        yield return ["correlated with the root", Query(q => q.Where(r => r.Stops.Any(s => s.When < r.Departs))), "a relational comparison"];
        // The element read is DIRECT on one side of the relational (the nested count is the other side).
        yield return ["nested count compared with an element member", Query(q => q.Where(r => r.Stops.Any(s => r.Stops.Count(s2 => s2.City == s.City) > s.Floor))), "a relational comparison"];
        // The guard-requiring shape sits INSIDE the nested lambda; the outer Finder visits its body.
        yield return ["relational inside a nested element lambda", Query(q => q.Where(r => r.Stops.Any(s => r.Stops.Count(s2 => s2.Floor < s.Floor) >= 1))), "a relational comparison"];
    }

    [Theory]
    [MemberData(nameof(NotRefusedShapes))]
    public void Shapes_outside_the_category_are_not_found(string label, Expression captured)
        => Assert.True(ComplexElementNullGuardRefusal.Find(captured, Model) is null, label);

    public static IEnumerable<object[]> NotRefusedShapes()
    {
        yield return ["equality with a constant", Query(q => q.Where(r => r.Stops.Any(s => s.City == "Oslo")))];
        yield return ["equality on the declined date part", Query(q => q.Where(r => r.Stops.Any(s => s.When.AddDays(1).Year == 2024)))];
        yield return ["bare bool", Query(q => q.Where(r => r.Stops.Any(s => s.Verified)))];
        yield return ["string operator", Query(q => q.Where(r => r.Stops.Any(s => s.City.StartsWith("O"))))];
        yield return ["Length == constant (a null-propagating operator, no guard needed)", Query(q => q.Where(r => r.Stops.Any(s => s.Note!.Length == 2)))];
        yield return ["bare Any", Query(q => q.Where(r => r.Stops.Any()))];
        yield return ["Count comparison at the root", Query(q => q.Where(r => r.Stops.Count > 1))];
        yield return ["indexer, no element lambda", Query(q => q.Where(r => r.Stops[0].City == "Oslo"))];
        yield return ["root relational (no element scope)", Query(q => q.Where(r => r.Departs.AddDays(1).Year < 2030))];
        yield return ["root relational over a complex-collection count", Query(q => q.Where(r => r.Stops.Count < 3))];
        yield return ["OWNED element relational", Query(q => q.Where(r => r.Owned.Any(s => s.When.AddDays(1).Year < 2030)))];
        yield return ["OWNED element == null", Query(q => q.Where(r => r.Owned.Any(s => s.Note == null)))];
        yield return ["relational on the root inside the element lambda only", Query(q => q.Where(r => r.Stops.Any(s => r.Departs < DateTime.UnixEpoch)))];
        yield return ["Contains of the element itself (member-wise equality, not a leaf)", Query(q => q.Where(r => r.Stops.Any(s => new List<Stop>().Contains(s))))];
        // A relational over a COUNT whose nested lambda merely reads the element (`s.City`, an equality): the compared
        // value is the count, never the element's member, so no guard is needed (ParameterReadFinder stops at lambdas).
        yield return ["relational over a nested count that reads the element only inside its lambda", Query(q => q.Where(r => r.Stops.Any(s => r.Stops.Count(s2 => s2.City == s.City) >= 1)))];
        yield return ["relational over a nested Where(...).Count() reading the element inside its lambda", Query(q => q.Where(r => r.Stops.Any(s => r.Stops.Where(s2 => s2.City == s.City).Count() > 1)))];
    }

    [Fact]
    public void Explicit_DriverLinq_is_never_refused()
    {
        var captured = Query(q => q.Where(r => r.Stops.Any(s => s.Floor < 1)));
        ComplexElementNullGuardRefusal.ThrowIfDriverLinqMisreadsNullElements(captured, Model, MongoQueryMode.DriverLinq);
        var native = Assert.Throws<NativeTranslationNotSupportedException>(
            () => ComplexElementNullGuardRefusal.ThrowIfDriverLinqMisreadsNullElements(captured, Model, MongoQueryMode.Native));
        Assert.Contains("'Route.Stops'", native.Message);
        Assert.Contains("a relational comparison", native.Message);
        Assert.Contains("MongoQueryMode.DriverLinq", native.Message);
        Assert.Throws<NativeTranslationNotSupportedException>(
            () => ComplexElementNullGuardRefusal.ThrowIfDriverLinqMisreadsNullElements(captured, Model, MongoQueryMode.NativeOnly));
    }

    // ── Strict (bulk ExecuteUpdate/ExecuteDelete) allow-list ──────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(BulkAllowedShapes))]
    public void Bulk_allow_list_admits_only_shapes_the_driver_answers_like_R17(string label, Expression captured)
        => Assert.True(ComplexElementNullGuardRefusal.FindForBulk([captured], Model) is null, label);

    public static IEnumerable<object[]> BulkAllowedShapes()
    {
        yield return ["== constant", Query(q => q.Where(r => r.Stops.Any(s => s.City == "Oslo")))];
        yield return ["!= constant, constant on the left", Query(q => q.Where(r => r.Stops.Any(s => "Oslo" != s.City)))];
        yield return ["nullable == non-null constant", Query(q => q.Where(r => r.Stops.Any(s => s.Zip == 5)))];
        yield return ["bool == true", Query(q => q.Where(r => r.Stops.Any(s => s.Verified == true)))];
        yield return ["bare bool and its negation under && / ||", Query(q => q.Where(r => r.Stops.Any(s => s.Verified && !(s.Verified || s.City == "X"))))];
        yield return ["non-nullable list Contains", Query(q => q.Where(r => r.Stops.Any(s => new[] { 1, 2 }.Contains(s.Floor))))];
        yield return ["nullable list without null Contains", Query(q => q.Where(r => r.Stops.Any(s => new int?[] { 1, 2 }.Contains(s.Zip))))];
        yield return ["All", Query(q => q.Where(r => r.Stops.All(s => s.City == "Oslo")))];
        yield return ["Count(pred) compared", Query(q => q.Where(r => r.Stops.Count(s => s.City == "Oslo") > 1))];
        yield return ["Where(pred).Skip(1).Any()", Query(q => q.Where(r => r.Stops.Where(s => s.City == "Oslo").Skip(1).Any()))];
        yield return ["bare Any / Count / LongCount", Query(q => q.Where(r => r.Stops.Any() && r.Stops.Count > 0 && r.Stops.LongCount() > 0))];
        yield return ["root predicate only", Query(q => q.Where(r => r.Departs < DateTime.UnixEpoch && r.Name == "x"))];
        yield return ["owned collection relational (owned scopes untouched)", Query(q => q.Where(r => r.Owned.Any(s => s.Floor < 1)))];
    }

    [Theory]
    [MemberData(nameof(BulkRefusedShapes))]
    public void Bulk_allow_list_refuses_everything_else(string label, Expression captured, string shape)
    {
        var found = ComplexElementNullGuardRefusal.FindForBulk([captured], Model);
        Assert.True(found is not null, label);
        Assert.Contains(shape, found!.Value.Shape);
    }

    public static IEnumerable<object[]> BulkRefusedShapes()
    {
        yield return ["relational", Query(q => q.Where(r => r.Stops.Any(s => s.Floor > 1))), "the element predicate"];
        yield return ["== null", Query(q => q.Where(r => r.Stops.Any(s => s.Note == null))), "the element predicate"];
        yield return ["HasValue", Query(q => q.Where(r => r.Stops.Any(s => s.Zip.HasValue))), "the element predicate"];
        yield return ["bool == false", Query(q => q.Where(r => r.Stops.Any(s => s.Verified == false))), "the element predicate"];
        yield return ["bool != false (R19: a null element reads false; the driver's $ne is true)", Query(q => q.Where(r => r.Stops.Any(s => s.Verified != false))), "the element predicate"];
        yield return ["list with null", Query(q => q.Where(r => r.Stops.Any(s => new int?[] { null }.Contains(s.Zip)))), "the element predicate"];
        yield return ["member vs member", Query(q => q.Where(r => r.Stops.Any(s => s.City == s.Note))), "the element predicate"];
        yield return ["member vs root member", Query(q => q.Where(r => r.Stops.Any(s => s.City == r.Name))), "the element predicate"];
        yield return ["string method", Query(q => q.Where(r => r.Stops.Any(s => s.City.StartsWith("O")))), "the element predicate"];
        yield return ["arithmetic in equality", Query(q => q.Where(r => r.Stops.Any(s => s.Floor + 1 == 2))), "the element predicate"];
        yield return ["Select", Query(q => q.Where(r => r.Stops.Select(s => s.Floor).Contains(1))), "the operator 'Select'"];
        yield return ["Max", Query(q => q.Where(r => r.Stops.Max(s => s.Floor) > 1)), "the operator 'Max'"];
        yield return ["OrderBy", Query(q => q.Where(r => r.Stops.OrderBy(s => s.Floor).Any())), "the operator 'OrderBy'"];
        yield return ["collection read outside an operator", Query(q => q.Where(r => r.Stops != null)), "a read of the collection"];
        // The EF.Property spelling of the same read reaches the method-call arm, not the member arm (Task 15: without this
        // row the method-call arm's check survived mutation).
        yield return ["EF.Property collection read outside an operator",
            Query(q => q.Where(r => EF.Property<List<Stop>>(r, nameof(Route.Stops)) != null)), "a read of the collection"];
        // A lambda over complex elements the keying cannot bind (its source unknown, e.g. reached through a navigation) is
        // refused rather than trusted.
        yield return ["unbound element lambda", Scalar(q => (Expression<Func<Stop, bool>>)(s => s.Floor < 1)), "cannot bind to its collection"];
    }

    // ── Fix round 1: collection CLR types (I1) and unknown extension nodes ─────────────────────────────────────────

    public class Tag
    {
        public string Label { get; set; } = null!;
    }

    public class ArrayHolder { public int Id { get; set; } public Tag[] Items { get; set; } = []; }
    public class ListHolder { public int Id { get; set; } public List<Tag> Items { get; set; } = []; }
    public class IListHolder { public int Id { get; set; } public IList<Tag> Items { get; set; } = []; }
    public class ObservableHolder { public int Id { get; set; } public System.Collections.ObjectModel.ObservableCollection<Tag> Items { get; set; } = []; }

    private sealed class HolderContext<T>(Action<ModelBuilder> configure) : DbContext where T : class
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, PerInstanceModelCacheKeyFactory>()
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder) => configure(modelBuilder);
    }

    private sealed class PerInstanceModelCacheKeyFactory : Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime) => (context.GetType(), designTime);
    }

    private static IModel HolderModel<T>(Action<ModelBuilder> configure) where T : class
    {
        using var db = new HolderContext<T>(configure);
        return db.Model;
    }

    [Fact]
    public void Bulk_refuses_operators_over_collection_types_the_bridge_cannot_normalise()
    {
        // Normalised by the bridge (a List<T> is assignable): List<T>, IList<T> (and IEnumerable<T>/ICollection<T>/
        // IReadOnlyList<T>, which EF10 rejects for complex collections at model/serializer level: measured). Not normalised:
        // T[], ObservableCollection<T> (and any other concrete type a List<T> doesn't fit).
        Assert.Null(ComplexElementNullGuardRefusal.FindForBulk(
            [(Expression<Func<IQueryable<ListHolder>, IQueryable<ListHolder>>>)(q => q.Where(h => h.Items.Any(i => i.Label == "l")))],
            HolderModel<ListHolder>(mb => mb.Entity<ListHolder>().ComplexCollection(h => h.Items))));
        Assert.Null(ComplexElementNullGuardRefusal.FindForBulk(
            [(Expression<Func<IQueryable<IListHolder>, IQueryable<IListHolder>>>)(q => q.Where(h => h.Items.Any(i => i.Label == "l")))],
            HolderModel<IListHolder>(mb => mb.Entity<IListHolder>().ComplexCollection(h => h.Items))));

        var array = ComplexElementNullGuardRefusal.FindForBulk(
            [(Expression<Func<IQueryable<ArrayHolder>, IQueryable<ArrayHolder>>>)(q => q.Where(h => h.Items.Any(i => i.Label == "l")))],
            HolderModel<ArrayHolder>(mb => mb.Entity<ArrayHolder>().ComplexCollection(h => h.Items)));
        Assert.Contains("whose CLR type cannot hold a List<T>", array!.Value.Shape);

        var observable = ComplexElementNullGuardRefusal.FindForBulk(
            [(Expression<Func<IQueryable<ObservableHolder>, IQueryable<ObservableHolder>>>)(q => q.Where(h => h.Items.Any(i => i.Label == "l")))],
            HolderModel<ObservableHolder>(mb => mb.Entity<ObservableHolder>().ComplexCollection(h => h.Items)));
        Assert.Contains("whose CLR type cannot hold a List<T>", observable!.Value.Shape);
    }

    [Theory]
    [InlineData(typeof(List<Tag>), true)]
    [InlineData(typeof(IList<Tag>), true)]
    [InlineData(typeof(ICollection<Tag>), true)]
    [InlineData(typeof(IEnumerable<Tag>), true)]
    [InlineData(typeof(IReadOnlyList<Tag>), true)]
    [InlineData(typeof(IReadOnlyCollection<Tag>), true)]
    [InlineData(typeof(Tag[]), false)]
    [InlineData(typeof(HashSet<Tag>), false)]
    [InlineData(typeof(System.Collections.ObjectModel.ObservableCollection<Tag>), false)]
    [InlineData(typeof(System.Collections.ObjectModel.Collection<Tag>), false)]
    public void The_shared_normalisation_predicate_per_collection_type(Type type, bool normalised)
        => Assert.Equal(normalised, type.IsNormalizableToEmptyList(out _));

    private sealed class OpaqueExtension(Expression inner) : Expression
    {
        public Expression Inner { get; } = inner;

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Type Type => Inner.Type;
    }

    [Fact]
    public void Bulk_refuses_an_unknown_extension_node_that_could_hide_an_element_lambda()
    {
        Expression<Func<Route, bool>> elementLambdaHolder = r => r.Stops.Any(s => s.Floor < 1);
        var hidden = Expression.Lambda<Func<Route, bool>>(new OpaqueExtension(elementLambdaHolder.Body), elementLambdaHolder.Parameters);
        var captured = Query(q => q.Where(r => true));
        var wrapped = Expression.Call(((MethodCallExpression)captured).Method, ((MethodCallExpression)captured).Arguments[0], Expression.Quote(hidden));
        var found = ComplexElementNullGuardRefusal.FindForBulk([wrapped], Model);
        Assert.Contains("of type 'OpaqueExtension' the bulk check cannot inspect", found!.Value.Shape);
        // Query mode (non-strict) is unchanged: it skips extension nodes.
        Assert.Null(ComplexElementNullGuardRefusal.Find(wrapped, Model));
    }

    private sealed class RouteContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Route>(b =>
            {
                b.ComplexCollection(r => r.Stops);
                b.OwnsMany(r => r.Owned);
            });
    }
}
#endif
