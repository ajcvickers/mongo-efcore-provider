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

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation;

#nullable enable

/// <summary>
/// The native rendering of whole complex-value equality (<c>MongoExpressionTranslator.ComplexEquality.cs</c>): null checks
/// and constant member-wise equality stay in the index-usable query dialect; stored-to-stored equality is <c>$expr</c> over
/// <c>$ifNull</c>-normalized leaves; every <c>!=</c>/<c>!(...)</c> is the exact complement; unsupported members decline.
/// </summary>
public static class ComplexEqualityTranslationTests
{
    public struct Pt
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public class Addr
    {
        public string City { get; set; } = null!;
        public int? Zip { get; set; }
        public Pt Geo { get; set; }
    }

    public class LabelBox
    {
        public List<string> Labels { get; set; } = [];
    }

    public class Holder
    {
        public int Id { get; set; }
        public Addr Home { get; set; } = null!;
        public Addr Work { get; set; } = null!;
        public Addr Converted { get; set; } = null!;
        public LabelBox Box { get; set; } = null!;
    }

    private static IEntityType Model()
    {
        using var db = new TestContext(mb =>
        {
            mb.Entity<Holder>().ComplexProperty(h => h.Home, a => a.ComplexProperty(x => x.Geo));
            mb.Entity<Holder>().ComplexProperty(h => h.Work, a =>
            {
                a.HasPropertyAnnotation(MongoDB.EntityFrameworkCore.Metadata.MongoAnnotationNames.ElementName, "work");
                a.Property(x => x.City).Metadata.SetElementName("town");
                a.ComplexProperty(x => x.Geo);
            });
            mb.Entity<Holder>().ComplexProperty(h => h.Converted, a =>
            {
                a.Property(x => x.Zip).HasConversion<string>();
                a.ComplexProperty(x => x.Geo);
            });
            mb.Entity<Holder>().ComplexProperty(h => h.Box);
        });
        return db.Model.FindEntityType(typeof(Holder))!;
    }

    private static string Render(Expression<Func<Holder, bool>> predicate)
    {
        var translator = new MongoExpressionTranslator(Model(), predicate.Parameters[0]);
        Assert.True(translator.TryTranslate(predicate.Body, out var node), "expected a native translation");
        return new MongoQueryLanguageRenderer().Render(node, new PlaceholderTable()).ToJson();
    }

    private static void AssertDeclines(Expression<Func<Holder, bool>> predicate)
    {
        var translator = new MongoExpressionTranslator(Model(), predicate.Parameters[0]);
        Assert.False(translator.TryTranslate(predicate.Body, out var node));
        Assert.Null(node);
    }

    [Fact]
    public static void Null_check_is_the_index_usable_query_dialect()
    {
        Assert.Equal("""{ "Home" : null }""", Render(h => h.Home == null));
        Assert.Equal("""{ "work" : { "$ne" : null } }""", Render(h => h.Work != null));
        Assert.Equal("""{ "work" : { "$ne" : null } }""", Render(h => !(h.Work == null)));
        Assert.Equal("""{ "Home.Geo" : null }""", Render(h => null == (object)h.Home.Geo));
        Assert.Equal("""{ "Home" : null }""", Render(h => !(h.Home != null)));
        Assert.Equal("""{ "Home.Geo.Lat" : 0.0, "Home.Geo.Lon" : 0.0 }""", Render(h => h.Home.Geo.Equals(new Pt())));
    }

    [Fact]
    public static void Constant_instance_is_member_wise_in_the_query_dialect_with_a_null_member_as_null_or_missing()
    {
        var rendered = Render(h => h.Home == new Addr { City = "Paris", Zip = null, Geo = new Pt { Lat = 1, Lon = 2 } });
        Assert.Equal(
            """{ "$and" : [{ "Home.City" : "Paris", "Home.Zip" : null }, { "Home.Geo.Lat" : 1.0, "Home.Geo.Lon" : 2.0 }] }""",
            rendered);
    }

    [Fact]
    public static void Not_equal_is_the_De_Morgan_complement_never_an_aggregation_not()
    {
        var rendered = Render(h => h.Home != new Addr { City = "Paris", Zip = 7, Geo = new Pt { Lat = 1, Lon = 2 } });
        Assert.Equal(
            """{ "$or" : [{ "Home.City" : { "$ne" : "Paris" } }, { "Home.Zip" : { "$ne" : 7 } }, { "Home.Geo.Lat" : { "$ne" : 1.0 } }, { "Home.Geo.Lon" : { "$ne" : 2.0 } }] }""",
            rendered);
        Assert.Equal(rendered,
            Render(h => !(h.Home == new Addr { City = "Paris", Zip = 7, Geo = new Pt { Lat = 1, Lon = 2 } })));
    }

    [Fact]
    public static void Stored_to_stored_reads_each_side_under_its_own_element_names_null_safe()
    {
        var rendered = Render(h => h.Home == h.Work);
        Assert.Contains("""{ "$eq" : [{ "$ifNull" : ["$Home.City", null] }, { "$ifNull" : ["$work.town", null] }] }""", rendered);
        Assert.Contains("""{ "$eq" : [{ "$ifNull" : ["$Home.Zip", null] }, { "$ifNull" : ["$work.Zip", null] }] }""", rendered);
        Assert.Contains("""{ "$eq" : [{ "$ifNull" : ["$Home.Geo.Lat", null] }, { "$ifNull" : ["$work.Geo.Lat", null] }] }""", rendered);
    }

    [Fact]
    public static void Stored_values_not_stored_alike_decline()
        // Converted stores Zip as a string; Home as an int.
        => AssertDeclines(h => h.Home == h.Converted);

    [Fact]
    public static void Primitive_collection_leaf_declines()
        => AssertDeclines(h => h.Box == new LabelBox { Labels = new List<string> { "a" } });

    [Fact]
    public static void Construction_leaving_a_member_unbound_declines()
        // Geo is not bound: its value is whatever the initializer leaves, which the translator can't see.
        => AssertDeclines(h => h.Home == new Addr { City = "Paris", Zip = 7 });

    [Fact]
    public static void Construction_reading_the_row_declines()
        => AssertDeclines(h => h.Home == new Addr { City = h.Work.City, Zip = 7, Geo = new Pt { Lat = 1, Lon = 2 } });

    [Fact]
    public static void Collection_typed_leaf_declines_for_a_constant_and_a_captured_comparand()
    {
        // Reaches the leaf guard (the list initializer form declines earlier, as an unbound/computed member).
        var box = new LabelBox { Labels = ["a"] };
        var parameter = Expression.Parameter(typeof(Holder), "h");
        var constant = Expression.Equal(Expression.Property(parameter, nameof(Holder.Box)), Expression.Constant(box));
        Assert.False(new MongoExpressionTranslator(Model(), parameter).TryTranslate(constant, out _));

        var captured = Expression.Equal(Expression.Property(parameter, nameof(Holder.Box)), QueryParameter("__box_0", typeof(LabelBox)));
        Assert.False(new MongoExpressionTranslator(Model(), parameter).TryTranslate(captured, out _));
    }

    [Fact]
    public static void Captured_class_comparand_has_a_per_execution_null_branch()
    {
        // A captured class instance may be null at run time, so `h.Home == p` is (p is null AND Home is null/missing) OR
        // (p is not null AND the members match): the two is-null tests are runtime-evaluated bool parameters ({ $expr: p }).
        // Required, so no presence guard on the member branch. Correct; less index-friendly than a constant.
        var parameter = Expression.Parameter(typeof(Holder), "h");
        var body = Expression.Equal(Expression.Property(parameter, nameof(Holder.Home)), QueryParameter("__other_0", typeof(Addr)));
        Assert.True(new MongoExpressionTranslator(Model(), parameter).TryTranslate(body, out var node));
        var rendered = new MongoQueryLanguageRenderer().Render(node, new PlaceholderTable()).ToJson();
        Assert.StartsWith("""{ "$or" : [{ "$and" : [{ "$expr" : { "__mongoef_param__" : 0 } }, { "Home" : null }] }, { "$and" : [{ "$expr" : { "__mongoef_param__" : 1 } }, """,
            rendered);
        Assert.Contains("\"Home.City\" : { \"__mongoef_param__\" : 2 }", rendered);

        // A non-nullable struct comparand has no null branch.
        var pin = Expression.Call(
            Expression.Convert(Expression.Property(Expression.Property(parameter, nameof(Holder.Home)), nameof(Addr.Geo)), typeof(object)),
            typeof(object).GetMethod(nameof(object.Equals), [typeof(object)])!,
            Expression.Convert(QueryParameter("__pin_0", typeof(Pt)), typeof(object)));
        Assert.True(new MongoExpressionTranslator(Model(), parameter).TryTranslate(pin, out var pinNode));
        Assert.Equal("""{ "Home.Geo.Lat" : { "__mongoef_param__" : 0 }, "Home.Geo.Lon" : { "__mongoef_param__" : 1 } }""",
            new MongoQueryLanguageRenderer().Render(pinNode, new PlaceholderTable()).ToJson());
    }

    [Fact]
    public static void A_declining_equality_inside_a_disjunction_declines_the_whole_predicate_and_leaves_no_residue()
    {
        // The translator is stateless between nodes: a declined equality (an inline construction reading the row) leaves
        // nothing behind, so the same translator then translates a supported predicate exactly as a fresh one does.
        Expression<Func<Holder, bool>> declining
            = h => h.Home == new Addr { City = "Paris", Zip = 7, Geo = new Pt() } || h.Home == new Addr { City = h.Work.City, Zip = 7, Geo = new Pt() };
        var translator = new MongoExpressionTranslator(Model(), declining.Parameters[0]);
        Assert.False(translator.TryTranslate(declining.Body, out _));

        Expression<Func<Holder, bool>> supported = h => h.Home == new Addr { City = "Paris", Zip = 7, Geo = new Pt() } || h.Work == null;
        translator.SelfParam = supported.Parameters[0];
        Assert.True(translator.TryTranslate(supported.Body, out var reused));
        Assert.Equal(Render(supported), new MongoQueryLanguageRenderer().Render(reused, new PlaceholderTable()).ToJson());
    }

    [Fact]
    public static void Element_scope_leaves_are_null_safe_like_the_null_check()
    {
        // Inside a $filter/$map element scope a bare `$eq` against null is not $ifNull-normalized (owner ruling), so the
        // member-wise leaves carry NullSafe: `{ $eq: [{ $ifNull: ["$$e.Home.Zip", null] }, null] }`, consistent with the
        // null check's `{ $ifNull: ["$$e.Home", null] }` (R16).
        var parameter = Expression.Parameter(typeof(Holder), "h");
        var body = Expression.Equal(
            Expression.Property(parameter, nameof(Holder.Home)),
            Expression.Constant(new Addr { City = "Paris", Zip = null, Geo = new Pt() }));
        Assert.True(new MongoExpressionTranslator(Model(), parameter).TryTranslate(body, out var node));
        var rendered = MongoAggregationExpressionRenderer.Render(node, new PlaceholderTable(), elementVariable: "e").ToJson();
        Assert.Contains("""{ "$eq" : [{ "$ifNull" : ["$$e.Home.Zip", null] }, null] }""", rendered);
    }

    private static Expression QueryParameter(string name, Type type)
#if EF8 || EF9
        => Expression.Parameter(type, Microsoft.EntityFrameworkCore.Query.QueryCompilationContext.QueryParameterPrefix + name);
#else
        => new Microsoft.EntityFrameworkCore.Query.QueryParameterExpression(name, type);
#endif

    public class Lookalike
    {
        public Addr Home { get; set; } = null!;
    }

    [Fact]
    public static void A_parameter_of_another_type_never_resolves_against_the_entity()
    {
        // Scope by identity, never member name: `x.Home` on a projected row/lookalike type is not the entity's Home.
        Expression<Func<Lookalike, bool>> predicate = x => x.Home == null;
        var translator = new MongoExpressionTranslator(Model(), predicate.Parameters[0]);
        Assert.False(translator.TryTranslate(predicate.Body, out _));
    }

#if !EF8 && !EF9
    public class WithLines
    {
        public string City { get; set; } = null!;
        public List<Line> Lines { get; set; } = [];
    }

    public class Line
    {
        public string Sku { get; set; } = null!;
    }

    public class LinesHolder
    {
        public int Id { get; set; }
        public WithLines Order { get; set; } = null!;
    }

    [Fact]
    public static void Nested_complex_collection_declines()
    {
        // Its own context type: EF caches one model per context type, so the shared TestContext would answer Holder's.
        using var db = new LinesContext();
        var entityType = db.Model.FindEntityType(typeof(LinesHolder))!;
        Expression<Func<LinesHolder, bool>> predicate
            = h => h.Order == new WithLines { City = "c", Lines = new List<Line>() };
        var translator = new MongoExpressionTranslator(entityType, predicate.Parameters[0]);
        Assert.False(translator.TryTranslate(predicate.Body, out _));

        // Its null check needs no member, so it stays native.
        Expression<Func<LinesHolder, bool>> nullCheck = h => h.Order != null;
        Assert.True(new MongoExpressionTranslator(entityType, nullCheck.Parameters[0]).TryTranslate(nullCheck.Body, out _));
    }
#endif

    [Fact]
    public static void Null_check_node_negates_exactly_and_renders_null_safe_in_the_aggregation_dialect()
    {
        var node = new MongoElementNullCheckExpression("Home", isNotNull: false);
        Assert.True(MongoExpressionNegator.TryNegate(node, out var negated));
        Assert.True(Assert.IsType<MongoElementNullCheckExpression>(negated).IsNotNull);
        Assert.Equal("""{ "$eq" : [{ "$ifNull" : ["$Home", null] }, null] }""",
            MongoAggregationExpressionRenderer.Render(node, new PlaceholderTable()).ToJson());
        Assert.True(MongoFieldPrefixRewriter.TryRewrite(node, "u", out var prefixed));
        Assert.Equal("u.Home", Assert.IsType<MongoElementNullCheckExpression>(prefixed).Path);
    }

#if !EF8 && !EF9
    private sealed class LinesContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<LinesHolder>().ComplexProperty(h => h.Order, o => o.ComplexCollection(x => x.Lines));
    }
#endif

    // One model for every test using it (EF caches the model per context type): only Model() may use it.
    private sealed class TestContext(Action<ModelBuilder> configure) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => configure(modelBuilder);
    }
}
