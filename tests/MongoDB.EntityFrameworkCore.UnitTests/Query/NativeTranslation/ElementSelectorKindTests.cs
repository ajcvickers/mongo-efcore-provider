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
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;
using MongoDB.EntityFrameworkCore.UnitTests.TestUtilities;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation;

/// <summary>
/// <see cref="NativeReferencedElementMatcher.ClassifySelector"/> decides which element-leaf selectors read the stored row (#32): only those whose
/// every use of the element is the read of a mapped scalar property, which can run over an element built outside the change tracker. Everything else
/// uses the element itself, which on a tracking query is the tracked instance (EF Core's identity resolution), so it is built as usual.
/// </summary>
public class ElementSelectorKindTests
{
    private enum Level
    {
        Low,
        High
    }

    private class Element
    {
        public int Id { get; set; }
        public string Method { get; set; } = "";
        public int Rank { get; set; }
        public int? Count { get; set; }
        public Level Level { get; set; }
        public Level Stored { get; set; }
        public string Renamed { get; set; } = "";
        public List<string> Labels { get; set; } = [];
        public Dictionary<string, string> Extras { get; set; } = [];
        public Parent Parent { get; set; } = null!;
        public Address Address { get; set; } = null!;
        public Place Place { get; set; } = null!;

        [NotMapped]
        public string Display => Method + Rank;

        public string this[string key] => key;

        public string Describe() => Method;
    }

    private class Derived : Element
    {
        public string Extra { get; set; } = "";
    }

    private class Parent
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class Address
    {
        public string City { get; set; } = "";
    }

    private class Place
    {
        public string Name { get; set; } = "";
    }

    private static ElementSelectorKind Classify(LambdaExpression? selector)
    {
        using var db = SingleEntityDbContext.Create<Element>(
            mb =>
            {
                var element = mb.Entity<Element>();
                element.OwnsOne(e => e.Address);
                element.ComplexProperty(e => e.Place);
                element.Property<int>("Shadow");
                element.Property(e => e.Stored).HasConversion<string>();
                element.Property(e => e.Renamed).HasElementName("renamed_in_store");
                element.Ignore(e => e.Display);
                mb.Entity<Derived>();
            });
        return NativeReferencedElementMatcher.ClassifySelector(selector, db.Model.FindEntityType(typeof(Element))!);
    }

    private static void AssertScalarOnly(Expression<Func<Element, object?>> selector)
        => Assert.Equal(ElementSelectorKind.ScalarMembersOnly, Classify(selector));

    private static void AssertOther(Expression<Func<Element, object?>> selector)
        => Assert.Equal(ElementSelectorKind.Other, Classify(selector));

    [Fact]
    public void No_selector_is_None()
        => Assert.Equal(ElementSelectorKind.None, Classify(null));

    [Fact]
    public void A_scalar_read_is_ScalarMembersOnly()
        => AssertScalarOnly(e => e.Method);

    [Fact]
    public void The_key_is_a_scalar_read()
        => AssertScalarOnly(e => e.Id);

    [Fact]
    public void Computing_from_scalar_reads_is_ScalarMembersOnly()
        => AssertScalarOnly(e => e.Method.ToUpper() + (e.Rank + 1));

    [Fact]
    public void Constructing_a_result_from_scalar_reads_is_ScalarMembersOnly()
        => AssertScalarOnly(e => new { e.Method, e.Rank });

    [Fact]
    public void A_nullable_enum_converted_and_renamed_scalar_is_ScalarMembersOnly()
    {
        AssertScalarOnly(e => e.Count);
        AssertScalarOnly(e => e.Level);
        AssertScalarOnly(e => e.Stored);
        AssertScalarOnly(e => e.Renamed);
    }

    [Fact]
    public void A_conditional_over_scalar_reads_is_ScalarMembersOnly()
    {
        AssertScalarOnly(e => e.Rank > 1 ? e.Method : null);
        AssertScalarOnly(e => e.Count != null ? e.Count.Value + 1 : 0);
        AssertScalarOnly(e => e.Method != null ? e.Method.Length : 0);
        AssertScalarOnly(e => e.Count ?? e.Rank);
    }

    [Fact]
    public void A_primitive_collection_property_is_a_scalar_read()
    {
        AssertScalarOnly(e => e.Labels);
        AssertScalarOnly(e => e.Labels.Count);
        AssertScalarOnly(e => e.Extras["x"]);
    }

    [Fact]
    public void A_nested_lambda_that_only_reads_scalars_of_the_element_is_ScalarMembersOnly()
    {
        AssertScalarOnly(e => e.Labels.Any(label => label == e.Method));
        AssertScalarOnly(e => new[] { 1, 2 }.Select(x => x + e.Rank).ToList());
    }

    [Fact]
    public void A_captured_value_is_not_a_use_of_the_element()
    {
        var suffix = "!";
        AssertScalarOnly(e => e.Method + suffix);
    }

    /// <summary>A query parameter as nav-expansion leaves it (EF8/EF9: a prefixed <see cref="ParameterExpression"/>; EF10+: <c>QueryParameterExpression</c>).</summary>
    private static Expression QueryParameter(string name, Type type)
#if EF8 || EF9
        => Expression.Parameter(type, QueryCompilationContext.QueryParameterPrefix + name);
#else
        => new QueryParameterExpression("__" + name, type);
#endif

    private static Expression<Func<Element, string>> MethodPlusQueryParameter()
    {
        var element = Expression.Parameter(typeof(Element), "e");
        var body = Expression.Call(
            typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)])!,
            Expression.Property(element, nameof(Element.Method)),
            QueryParameter("suffix_0", typeof(string)));
        return Expression.Lambda<Func<Element, string>>(body, element);
    }

    [Fact]
    public void A_query_parameter_is_not_a_use_of_the_element()
        => Assert.Equal(ElementSelectorKind.ScalarMembersOnly, Classify(MethodPlusQueryParameter()));

    [Fact]
    public void A_selector_reading_a_query_parameter_is_self_contained()
        => Assert.True(NativeProjectionBinder.IsSelfContainedLambda(MethodPlusQueryParameter()));

    [Fact]
    public void A_selector_reading_another_free_parameter_is_not_self_contained()
    {
        var element = Expression.Parameter(typeof(Element), "e");
        var outer = Expression.Parameter(typeof(string), "outer");
        var lambda = Expression.Lambda<Func<Element, string>>(
            Expression.Call(
                typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)])!,
                Expression.Property(element, nameof(Element.Method)), outer),
            element);

        Assert.False(NativeProjectionBinder.IsSelfContainedLambda(lambda));
    }

    [Fact]
    public void ReplaceQueryParameters_reads_each_query_parameter_from_the_query_context()
    {
        var selector = MethodPlusQueryParameter();
        var body = NativeQueryParameter.ReplaceQueryParameters(selector.Body, QueryCompilationContext.QueryContextParameter);
        var compiled = Expression.Lambda<Func<QueryContext, Element, string>>(
            body, QueryCompilationContext.QueryContextParameter, selector.Parameters[0]).Compile();

        using var db = SingleEntityDbContext.Create<Element>();
        var queryContext = db.GetService<IQueryContextFactory>().Create();
#if EF8 || EF9
        queryContext.AddParameter(QueryCompilationContext.QueryParameterPrefix + "suffix_0", "!");
#else
        queryContext.Parameters["__suffix_0"] = "!";
#endif

        Assert.Equal("Tattoo!", compiled(queryContext, new Element { Method = "Tattoo" }));
        Assert.False(NativeQueryParameter.TryGetQueryParameterName(body, out _));
    }

    [Fact]
    public void The_streaming_cursor_accepts_a_query_parameter_read_from_the_query_context()
    {
        using var db = SingleEntityDbContext.Create<Element>();
        var elementType = db.Model.FindEntityType(typeof(Element))!;
        var method = elementType.FindProperty(nameof(Element.Method))!;
        var local = Expression.Variable(typeof(string), "method");
        var selector = MethodPlusQueryParameter();
        var body = NativeQueryParameter.ReplaceQueryParameters(selector.Body, QueryCompilationContext.QueryContextParameter);

        var rewriter = new MongoElementSelectorRewriter(
            elementType, new Dictionary<Microsoft.EntityFrameworkCore.Metadata.IProperty, ParameterExpression> { [method] = local },
            selector.Parameters[0]);
        var rewritten = rewriter.Visit(body)!;

        Assert.Equal([method], rewriter.Used);
        Assert.False(ExpressionReferences(rewritten, selector.Parameters[0]));

        // Only a read from the shaper's own query-context parameter is accepted, not one from any other expression.
        Assert.Throws<NativeTranslationNotSupportedException>(
            () => new MongoElementSelectorRewriter(
                elementType, new Dictionary<Microsoft.EntityFrameworkCore.Metadata.IProperty, ParameterExpression> { [method] = local },
                selector.Parameters[0]).Visit(NativeQueryParameter.ReplaceQueryParameters(selector.Body, Expression.Parameter(typeof(QueryContext), "other"))));
    }

    private static bool ExpressionReferences(Expression expression, ParameterExpression parameter)
    {
        var found = false;
        new ParameterFinder(p => found |= p == parameter).Visit(expression);
        return found;
    }

    private sealed class ParameterFinder(Action<ParameterExpression> onParameter) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            onParameter(node);
            return node;
        }
    }

    [Fact]
    public void Returning_the_element_is_Other()
        => AssertOther(e => e);

    [Fact]
    public void Keeping_the_element_beside_a_scalar_is_Other()
        => AssertOther(e => new { e, e.Method });

    [Fact]
    public void Calling_a_method_on_the_element_is_Other()
        => AssertOther(e => e.Describe());

    [Fact]
    public void Reading_a_navigation_is_Other()
        => AssertOther(e => e.Parent.Name);

    [Fact]
    public void One_escaping_use_makes_the_whole_selector_Other()
        => AssertOther(e => e.Method + e.Describe());

    [Fact]
    public void Reading_an_owned_member_is_Other()
        => AssertOther(e => e.Address.City);

    [Fact]
    public void Reading_a_complex_member_is_Other()
    {
        AssertOther(e => e.Place.Name);
        AssertOther(e => e.Place);
    }

    [Fact]
    public void Reading_a_shadow_property_is_Other()
        => AssertOther(e => EF.Property<int>(e, "Shadow"));

    [Fact]
    public void Reading_a_property_through_EF_Property_is_Other_even_for_a_mapped_one()
        => AssertOther(e => EF.Property<string>(e, "Method"));

    [Fact]
    public void Reading_an_unmapped_CLR_property_is_Other()
        => AssertOther(e => e.Display);

    [Fact]
    public void Reading_through_the_element_indexer_is_Other()
        => AssertOther(e => e["x"]);

    [Fact]
    public void Converting_the_element_is_Other()
    {
        AssertOther(e => (object)e);
        AssertOther(e => e as Derived);
        AssertOther(e => e is Derived);
        AssertOther(e => ((Derived)e).Extra);
        AssertOther(e => (e as Derived)!.Extra);
    }

    [Fact]
    public void Invoking_a_delegate_with_the_element_is_Other()
    {
        Func<Element, string> describe = x => x.Method;
        AssertOther(e => describe(e));
    }

    [Fact]
    public void A_nested_lambda_that_lets_the_element_escape_is_Other()
    {
        AssertOther(e => new[] { 1 }.Select(x => e).ToList());
        AssertOther(e => new[] { 1 }.Select(x => e.Describe()).ToList());
    }

    [Fact]
    public void Object_methods_on_the_element_are_Other()
    {
        AssertOther(e => e.ToString());
        AssertOther(e => e.GetType());
        AssertOther(e => e.GetHashCode());
    }

    [Fact]
    public void A_block_that_aliases_the_element_is_Other()
    {
        var element = Expression.Parameter(typeof(Element), "e");
        var alias = Expression.Variable(typeof(Element), "alias");
        var body = Expression.Block(
            [alias],
            Expression.Assign(alias, element),
            Expression.Property(alias, nameof(Element.Method)));

        Assert.Equal(ElementSelectorKind.Other, Classify(Expression.Lambda<Func<Element, string>>(body, element)));
    }

    private class Holder
    {
        public int Id { get; set; }
        public List<Kid> Kids { get; set; } = [];
    }

    private class Kid
    {
        public int W { get; set; }
        public Nested Nested { get; set; } = null!;
    }

    private class Nested
    {
        public int X { get; set; }
    }

    private static ElementSelectorKind ClassifyOwned(Expression<Func<Kid, object?>> selector)
    {
        using var db = SingleEntityDbContext.Create<Holder>(
            mb => mb.Entity<Holder>().OwnsMany(h => h.Kids, kid => kid.OwnsOne(k => k.Nested)));
        return NativeReferencedElementMatcher.ClassifySelector(selector, db.Model.FindEntityType(typeof(Kid))!);
    }

    [Fact]
    public void An_owned_element_s_scalar_read_is_ScalarMembersOnly()
        => Assert.Equal(ElementSelectorKind.ScalarMembersOnly, ClassifyOwned(k => k.W));

    [Fact]
    public void An_owned_element_s_nested_owned_member_is_Other()
        => Assert.Equal(ElementSelectorKind.Other, ClassifyOwned(k => k.Nested.X));

    private class KeyedHolder
    {
        public int Id { get; set; }
        public List<KeyedKid> Kids { get; set; } = [];
    }

    private class KeyedKid
    {
        public int Id { get; set; }
        public int KeyedHolderId { get; set; }
        public int W { get; set; }
    }

    private static ElementSelectorKind ClassifyKeyedOwned(Expression<Func<KeyedKid, object?>> selector)
    {
        using var db = SingleEntityDbContext.Create<KeyedHolder>(
            mb => mb.Entity<KeyedHolder>().OwnsMany(h => h.Kids, kid =>
            {
                kid.WithOwner().HasForeignKey(k => k.KeyedHolderId);
                kid.HasKey(k => new { k.KeyedHolderId, k.Id });
            }));
        return NativeReferencedElementMatcher.ClassifySelector(selector, db.Model.FindEntityType(typeof(KeyedKid))!);
    }

    // The owned type's key and ownership foreign key are synthesized by the entity path, not stored, so a stored-value read would miss them.
    [Fact]
    public void An_owned_element_s_key_or_owner_key_read_is_Other()
    {
        Assert.Equal(ElementSelectorKind.Other, ClassifyKeyedOwned(k => k.Id));
        Assert.Equal(ElementSelectorKind.Other, ClassifyKeyedOwned(k => k.KeyedHolderId));
        Assert.Equal(ElementSelectorKind.Other, ClassifyKeyedOwned(k => new { k.W, k.Id }));
        Assert.Equal(ElementSelectorKind.ScalarMembersOnly, ClassifyKeyedOwned(k => k.W));
    }

    [Fact]
    public void A_block_that_only_reads_scalars_is_ScalarMembersOnly()
    {
        var element = Expression.Parameter(typeof(Element), "e");
        var local = Expression.Variable(typeof(string), "local");
        var body = Expression.Block(
            [local],
            Expression.Assign(local, Expression.Property(element, nameof(Element.Method))),
            local);

        Assert.Equal(ElementSelectorKind.ScalarMembersOnly, Classify(Expression.Lambda<Func<Element, string>>(body, element)));
    }
}
