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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Metadata.Conventions;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.NativeTranslation;

/// <summary>
/// <see cref="StructuralPath"/>: resolving a root-first member-name chain across owned navigations and complex
/// properties to the stored document path. Each test builds its own model (one holder/context type per test,
/// because EF caches the model per context type).
/// </summary>
public static class StructuralPathTests
{
    // ------------------------------------------------------------------
    // Owned chains: the segments the pre-StructuralPath walk produced
    // ------------------------------------------------------------------

    [Fact]
    public static void Owned_to_owned_leaf_resolves_to_containing_element_names_then_leaf()
    {
        var root = EntityType<OwnedChainHolder>(mb =>
            mb.Entity<OwnedChainHolder>().OwnsOne(e => e.Address, a => a.OwnsOne(x => x.Geo)));

        Assert.True(StructuralPath.TryResolve(root, ["Address", "Geo", "Country"], 2, out var result));

        // "Address.Geo.Country" is what the owned dotted-path walk produced (MongoExpressionTranslatorTests
        // .Nested_owned_single_ref_subproperty_resolves_to_deep_dotted_field pins the translator output).
        Assert.Equal(["Address", "Geo", "Country"], result.Segments);
        var leaf = Assert.IsAssignableFrom<IProperty>(result.Leaf);
        Assert.Equal(nameof(OwnedGeo.Country), leaf.Name);
        Assert.Same(leaf.DeclaringType, result.LeafOwner);
        Assert.Equal(typeof(OwnedGeo), result.LeafOwner.ClrType);
        Assert.False(result.CrossesCollection);
    }

    [Fact]
    public static void Owned_hop_uses_containing_element_name_override()
    {
        var root = EntityType<OwnedRenamedHolder>(mb =>
            mb.Entity<OwnedRenamedHolder>().OwnsOne(e => e.Address, a =>
            {
                a.HasElementName("addr");
                a.OwnsOne(x => x.Geo, g => g.HasElementName("g"));
            }));

        Assert.True(StructuralPath.TryResolve(root, ["Address", "Geo", "Country"], 2, out var result));
        Assert.Equal(["addr", "g", "Country"], result.Segments);
    }

    [Fact]
    public static void Owned_navigation_as_leaf_resolves_to_its_containing_element_name()
    {
        var root = EntityType<OwnedNavLeafHolder>(mb =>
            mb.Entity<OwnedNavLeafHolder>().OwnsOne(e => e.Address, a => a.OwnsOne(x => x.Geo, g => g.HasElementName("g"))));

        Assert.True(StructuralPath.TryResolve(root, ["Address", "Geo"], 1, out var result));

        Assert.Equal(["Address", "g"], result.Segments);
        var navigation = Assert.IsAssignableFrom<INavigation>(result.Leaf);
        Assert.Equal(nameof(OwnedAddress.Geo), navigation.Name);
        Assert.Equal(typeof(OwnedAddress), result.LeafOwner.ClrType);
        Assert.False(result.CrossesCollection);
    }

    [Fact]
    public static void Composite_key_leaf_inside_owned_type_keeps_local_id_prefix()
    {
        var root = EntityType<KeyedOwnedHolder>(mb =>
            mb.Entity<KeyedOwnedHolder>().OwnsOne(e => e.Author, a => a.HasKey(x => new { x.City, x.Country })));

        Assert.True(StructuralPath.TryResolve(root, ["Author", "City"], 1, out var result));

        // The serializer nests an owned type's own composite key under a local "_id": "Author._id.City".
        Assert.Equal(["Author", "_id.City"], result.Segments);
        Assert.Equal("Author._id.City", string.Join(".", result.Segments));
    }

    [Fact]
    public static void Composite_key_leaf_at_root_keeps_id_prefix_with_zero_hops()
    {
        var root = EntityType<CompositeRootHolder>(mb =>
            mb.Entity<CompositeRootHolder>().HasKey(e => new { e.A, e.B }));

        Assert.True(StructuralPath.TryResolve(root, ["A"], 0, out var result));
        Assert.Equal(["_id.A"], result.Segments);
    }

    // ------------------------------------------------------------------
    // Complex chains
    // ------------------------------------------------------------------

    [Fact]
    public static void Complex_hop_then_leaf_resolves_to_element_names()
    {
        var root = EntityType<ComplexLeafHolder>(mb => mb.Entity<ComplexLeafHolder>().ComplexProperty(e => e.Address));

        Assert.True(StructuralPath.TryResolve(root, ["Address", "City"], 1, out var result));

        Assert.Equal(["Address", "City"], result.Segments);
        var leaf = Assert.IsAssignableFrom<IProperty>(result.Leaf);
        Assert.Equal(nameof(Addr.City), leaf.Name);
        var complexType = Assert.IsAssignableFrom<IComplexType>(result.LeafOwner);
        Assert.Same(complexType, leaf.DeclaringType);
        Assert.False(result.CrossesCollection);
    }

    [Fact]
    public static void Owned_then_complex_then_complex_then_leaf_resolves()
    {
        // OwnedNavigationBuilder has no ComplexProperty overload; [ComplexType] puts complex properties on an owned type.
        var root = EntityType<MixedHolder>(mb => mb.Entity<MixedHolder>().OwnsOne(e => e.Owned, o => o.HasElementName("own")));

        Assert.True(StructuralPath.TryResolve(root, ["Owned", "Extra", "Inner", "Value"], 3, out var result));

        Assert.Equal(["own", "Extra", "Inner", "Value"], result.Segments);
        Assert.IsAssignableFrom<IComplexType>(result.LeafOwner);
        Assert.Equal(typeof(MixedInner), result.LeafOwner.ClrType);
        Assert.False(result.CrossesCollection);
    }

    [Fact]
    public static void Struct_complex_hop_resolves()
    {
        var root = EntityType<StructHolder>(mb =>
            mb.Entity<StructHolder>().ComplexProperty(e => e.Address, a => a.ComplexProperty(x => x.Location)));

        Assert.True(StructuralPath.TryResolve(root, ["Address", "Location", "Lat"], 2, out var result));

        Assert.Equal(["Address", "Location", "Lat"], result.Segments);
        Assert.Equal(typeof(Pt), result.LeafOwner.ClrType);
    }

    [Fact]
    public static void Complex_element_name_overrides_apply_to_hop_and_leaf()
    {
        var root = EntityType<ComplexRenamedHolder>(mb =>
            mb.Entity<ComplexRenamedHolder>().ComplexProperty(e => e.Address, a =>
            {
                a.Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "addr");
                a.Property(x => x.City).Metadata.SetElementName("c");
            }));

        Assert.True(StructuralPath.TryResolve(root, ["Address", "City"], 1, out var result));
        Assert.Equal(["addr", "c"], result.Segments);
    }

    [Fact]
    public static void Camel_case_convention_names_complex_hop_and_leaf()
    {
        var root = EntityType<CamelHolder>(mb =>
        {
            mb.Entity<CamelHolder>().Property(e => e.Id).Metadata.SetElementName("_id");
            mb.Entity<CamelHolder>().ComplexProperty(e => e.HomeAddress);
        }, camelCase: true);

        Assert.True(StructuralPath.TryResolve(root, ["HomeAddress", "StreetName"], 1, out var result));
        Assert.Equal(["homeAddress", "streetName"], result.Segments);
    }

    [Fact]
    public static void Complex_leaf_under_composite_key_root_has_no_id_prefix()
    {
        // The root's composite key is (A, B). The complex type also has a member called A, but complex types have no
        // keys, so its leaf stays a plain element name: a name-based "_id." rule would mis-resolve it.
        var root = EntityType<ComplexUnderCompositeHolder>(mb =>
        {
            mb.Entity<ComplexUnderCompositeHolder>().HasKey(e => new { e.A, e.B });
            mb.Entity<ComplexUnderCompositeHolder>().ComplexProperty(e => e.Inner);
        });

        Assert.True(StructuralPath.TryResolve(root, ["Inner", "A"], 1, out var result));
        Assert.Equal(["Inner", "A"], result.Segments);
    }

    [Fact]
    public static void Same_clr_complex_type_at_two_paths_resolves_independently()
    {
        var root = EntityType<TwoAddressHolder>(mb =>
        {
            mb.Entity<TwoAddressHolder>().ComplexProperty(e => e.Home);
            mb.Entity<TwoAddressHolder>().ComplexProperty(e => e.Work, a =>
            {
                a.Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "office");
                a.Property(x => x.City).Metadata.SetElementName("officeCity");
            });
        });

        Assert.True(StructuralPath.TryResolve(root, ["Home", "City"], 1, out var home));
        Assert.True(StructuralPath.TryResolve(root, ["Work", "City"], 1, out var work));

        Assert.Equal(["Home", "City"], home.Segments);
        Assert.Equal(["office", "officeCity"], work.Segments);
        Assert.NotSame(home.LeafOwner, work.LeafOwner);
        Assert.NotSame(home.Leaf, work.Leaf);
    }

    [Fact]
    public static void Complex_property_as_leaf_resolves_to_its_element_name()
    {
        var root = EntityType<ComplexAsLeafHolder>(mb =>
            mb.Entity<ComplexAsLeafHolder>().ComplexProperty(e => e.Address, a => a.ComplexProperty(x => x.Location,
                l => l.Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "loc"))));

        Assert.True(StructuralPath.TryResolve(root, ["Address", "Location"], 1, out var result));

        Assert.Equal(["Address", "loc"], result.Segments);
        var complexProperty = Assert.IsAssignableFrom<IComplexProperty>(result.Leaf);
        Assert.Equal(nameof(NestedAddr.Location), complexProperty.Name);
        Assert.False(result.CrossesCollection);
    }

    // ------------------------------------------------------------------
    // hopCount boundaries
    // ------------------------------------------------------------------

    [Fact]
    public static void Zero_hops_resolves_leaf_on_scope()
    {
        var root = EntityType<ZeroHopHolder>(mb => mb.Entity<ZeroHopHolder>().Property(e => e.Name).HasElementName("n"));

        Assert.True(StructuralPath.TryResolve(root, ["Name"], 0, out var result));

        Assert.Equal(["n"], result.Segments);
        Assert.Same(root, result.LeafOwner);
        Assert.Equal(nameof(ZeroHopHolder.Name), Assert.IsAssignableFrom<IProperty>(result.Leaf).Name);
    }

    [Fact]
    public static void Empty_chain_resolves_to_scope_with_no_segments()
    {
        var root = EntityType<EmptyChainHolder>(_ => { });

        Assert.True(StructuralPath.TryResolve(root, [], 0, out var result));

        Assert.Empty(result.Segments);
        Assert.Null(result.Leaf);
        Assert.Same(root, result.LeafOwner);
    }

    [Fact]
    public static void All_hops_resolves_to_final_scope_with_no_leaf()
    {
        var root = EntityType<AllHopsHolder>(mb =>
            mb.Entity<AllHopsHolder>().OwnsOne(e => e.Owned, o => o.HasElementName("own")));

        Assert.True(StructuralPath.TryResolve(root, ["Owned", "Extra", "Inner"], 3, out var result));

        Assert.Equal(["own", "Extra", "Inner"], result.Segments);
        Assert.Null(result.Leaf);
        Assert.Equal(typeof(MixedInner), result.LeafOwner.ClrType);
    }

    [Fact]
    public static void Hop_count_outside_names_throws()
    {
        var root = EntityType<BadHopCountHolder>(_ => { });

        Assert.Throws<ArgumentOutOfRangeException>(() => StructuralPath.TryResolve(root, ["Name"], 2, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => StructuralPath.TryResolve(root, ["Name"], -1, out _));
    }

    // ------------------------------------------------------------------
    // Declines
    // ------------------------------------------------------------------

    [Fact]
    public static void Unknown_hop_or_leaf_name_declines()
    {
        var root = EntityType<UnknownHolder>(mb =>
        {
            mb.Entity<UnknownHolder>().ComplexProperty(e => e.Address);
            mb.Entity<UnknownHolder>().OwnsOne(e => e.Owned);
        });

        Assert.False(StructuralPath.TryResolve(root, ["Nope", "City"], 1, out var unknownHop));
        Assert.False(unknownHop.CrossesCollection);
        Assert.False(StructuralPath.TryResolve(root, ["Address", "Nope"], 1, out _));
        Assert.False(StructuralPath.TryResolve(root, ["Owned", "Nope"], 1, out _));
        Assert.False(StructuralPath.TryResolve(root, ["Nope"], 0, out _));
    }

    [Fact]
    public static void Scalar_property_as_hop_declines()
    {
        var root = EntityType<ScalarHopHolder>(_ => { });

        // "Name" is a scalar: it can't be walked into, even though a string has members.
        Assert.False(StructuralPath.TryResolve(root, ["Name", "Length"], 1, out var result));
        Assert.False(result.CrossesCollection);
    }

    [Fact]
    public static void Owned_collection_hop_declines_and_reports_crossing()
    {
        var root = EntityType<OwnedCollectionHolder>(mb => mb.Entity<OwnedCollectionHolder>().OwnsMany(e => e.Posts));

        Assert.False(StructuralPath.TryResolve(root, ["Posts", "Title"], 1, out var result));
        Assert.True(result.CrossesCollection);
    }

    [Fact]
    public static void Owned_collection_as_leaf_resolves_without_crossing()
    {
        var root = EntityType<OwnedCollectionLeafHolder>(mb =>
            mb.Entity<OwnedCollectionLeafHolder>().OwnsOne(e => e.Address, a => a.OwnsMany(x => x.Notes)));

        // The array itself has a dotted path; only walking INTO it crosses a collection.
        Assert.True(StructuralPath.TryResolve(root, ["Address", "Notes"], 1, out var result));

        Assert.Equal(["Address", "Notes"], result.Segments);
        Assert.True(Assert.IsAssignableFrom<INavigation>(result.Leaf).IsCollection);
        Assert.False(result.CrossesCollection);
    }

    [Fact]
    public static void Collection_hop_below_owned_reference_declines_and_reports_crossing()
    {
        var root = EntityType<NestedCollectionHopHolder>(mb =>
            mb.Entity<NestedCollectionHopHolder>().OwnsOne(e => e.Address, a => a.OwnsMany(x => x.Notes)));

        Assert.False(StructuralPath.TryResolve(root, ["Address", "Notes", "Text"], 2, out var result));
        Assert.True(result.CrossesCollection);
    }

    [Fact]
    public static void Reference_navigation_to_another_collection_declines_as_hop_and_leaf()
    {
        var root = EntityType<ReferencingHolder>(mb =>
        {
            mb.Entity<ReferencingHolder>();
            mb.Entity<ReferencedEntity>();
        });

        Assert.False(StructuralPath.TryResolve(root, ["Target", "Name"], 1, out var hop));
        Assert.False(hop.CrossesCollection);
        Assert.False(StructuralPath.TryResolve(root, ["Target"], 0, out _));
    }

    [Fact]
    public static void Reference_navigation_to_non_owned_target_with_element_name_annotation_declines_as_hop_and_leaf()
    {
        // The target is a root entity (its own collection) that nevertheless carries a Mongo:ElementName annotation,
        // so GetContainingElementName() is non-empty. Only the IsEmbedded() checks stop the walk from treating the
        // cross-document reference as an embedded sub-document ("tgt.Name").
        var root = EntityType<AnnotatedReferencingHolder>(mb =>
        {
            mb.Entity<AnnotatedReferencingHolder>();
            mb.Entity<AnnotatedReferencedEntity>().Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "tgt");
        });

        var target = root.FindNavigation(nameof(AnnotatedReferencingHolder.Target))!;
        Assert.False(target.IsEmbedded());
        Assert.Equal("tgt", target.TargetEntityType.GetContainingElementName());

        Assert.False(StructuralPath.TryResolve(root, ["Target", "Name"], 1, out var hop));
        Assert.False(hop.CrossesCollection);
        Assert.Empty(hop.Segments);
        Assert.False(StructuralPath.TryResolve(root, ["Target"], 0, out var leaf));
        Assert.Null(leaf.Leaf);
    }

#if !EF8 && !EF9
    [Fact]
    public static void Complex_collection_hop_declines_and_reports_crossing()
    {
        var root = EntityType<ComplexCollectionHolder>(mb => mb.Entity<ComplexCollectionHolder>().ComplexCollection(e => e.Addresses));

        Assert.False(StructuralPath.TryResolve(root, ["Addresses", "City"], 1, out var result));
        Assert.True(result.CrossesCollection);
    }

    [Fact]
    public static void Complex_collection_as_leaf_resolves_without_crossing()
    {
        var root = EntityType<ComplexCollectionLeafHolder>(mb =>
            mb.Entity<ComplexCollectionLeafHolder>().ComplexCollection(e => e.Addresses,
                a => a.Metadata.SetAnnotation(MongoAnnotationNames.ElementName, "addrs")));

        Assert.True(StructuralPath.TryResolve(root, ["Addresses"], 0, out var result));

        Assert.Equal(["addrs"], result.Segments);
        Assert.True(Assert.IsAssignableFrom<IComplexProperty>(result.Leaf).IsCollection);
        Assert.False(result.CrossesCollection);
    }

    [Fact]
    public static void Optional_complex_hop_resolves_like_required()
    {
        var root = EntityType<OptionalHolder>(mb => mb.Entity<OptionalHolder>().ComplexProperty(e => e.Address).IsRequired(false));

        Assert.True(StructuralPath.TryResolve(root, ["Address", "City"], 1, out var result));
        Assert.Equal(["Address", "City"], result.Segments);
    }
#endif

    // ------------------------------------------------------------------
    // Translator call sites now go through StructuralPath
    // ------------------------------------------------------------------

    [Fact]
    public static void Translator_resolves_complex_leaf_to_dotted_field()
    {
        var root = EntityType<TranslatorComplexHolder>(mb =>
            mb.Entity<TranslatorComplexHolder>().ComplexProperty(e => e.Address, a => a.ComplexProperty(x => x.Location)));
        var translator = new MongoExpressionTranslator(root);

        Assert.True(translator.TryTranslateField(Body<TranslatorComplexHolder>(e => e.Address.City), out var city));
        Assert.Equal("Address.City", city.ElementName);
        Assert.Equal(nameof(NestedAddr.City), city.Property.Name);

        Assert.True(translator.TryTranslateField(Body<TranslatorComplexHolder>(e => e.Address.Location.Lat), out var lat));
        Assert.Equal("Address.Location.Lat", lat.ElementName);
    }

    [Fact]
    public static void Translator_resolves_owned_then_complex_leaf_to_dotted_field()
    {
        var root = EntityType<TranslatorMixedHolder>(mb => mb.Entity<TranslatorMixedHolder>().OwnsOne(e => e.Owned));
        var translator = new MongoExpressionTranslator(root);

        Assert.True(translator.TryTranslateField(Body<TranslatorMixedHolder>(e => e.Owned.Extra.Inner.Value), out var field));
        Assert.Equal("Owned.Extra.Inner.Value", field.ElementName);
    }

    [Fact]
    public static void Translator_still_resolves_owned_null_check_and_declines_complex_null_check()
    {
        var root = EntityType<TranslatorNullCheckHolder>(mb =>
        {
            mb.Entity<TranslatorNullCheckHolder>().OwnsOne(e => e.Owned);
            mb.Entity<TranslatorNullCheckHolder>().ComplexProperty(e => e.Address);
        });
        var translator = new MongoExpressionTranslator(root);

        Assert.True(translator.TryTranslate(Predicate<TranslatorNullCheckHolder>(e => e.Owned == null), out var owned));
        var binary = Assert.IsType<MongoBinaryExpression>(owned);
        Assert.Equal("Owned", Assert.IsType<MongoElementRefExpression>(binary.Left).Path);

        // A complex-property null check has null-vs-missing semantics of its own (Task 12); it is not an
        // entity-typed operand, so it declines here rather than reading as an owned navigation.
        Assert.False(translator.TryTranslate(Predicate<TranslatorNullCheckHolder>(e => e.Address == null), out _));
    }

    [Fact]
    public static void Translator_does_not_treat_owned_collection_as_entity_typed_null_operand()
    {
        var root = EntityType<TranslatorCollectionNullHolder>(mb => mb.Entity<TranslatorCollectionNullHolder>().OwnsMany(e => e.Posts));
        var translator = new MongoExpressionTranslator(root);

        // Only a single owned reference is an entity-typed operand; an owned collection compared with null must not
        // become a sub-document null check on the array.
        // Measured: the translator declines outright (the comparison has no native rendering here), so the caller
        // routes the query to the fallback rather than emitting any predicate over the array.
        Assert.False(translator.TryTranslate(Predicate<TranslatorCollectionNullHolder>(e => e.Posts == null), out var result));
        Assert.Null(result);
    }

    [Fact]
    public static void Translator_resolves_owned_collection_array_but_not_owned_reference()
    {
        var root = EntityType<TranslatorReferenceQuantifierHolder>(mb =>
            mb.Entity<TranslatorReferenceQuantifierHolder>().OwnsOne(e => e.Owned, o => o.OwnsMany(x => x.Items)));
        var translator = new MongoExpressionTranslator(root);

        // The array source must be an embedded COLLECTION navigation.
        Assert.True(translator.TryTranslate(Predicate<TranslatorReferenceQuantifierHolder>(e => e.Owned.Items.Any(i => i.Title == "x")), out _));
        Assert.True(translator.TryTranslateOwnedCollectionArray(Body<TranslatorReferenceQuantifierHolder>(e => e.Owned.Items), out var array));
        Assert.Equal("Owned.Items", array.Path);

        // The owned reference itself (same chain, one name shorter) is not an array.
        Assert.False(translator.TryTranslateOwnedCollectionArray(Body<TranslatorReferenceQuantifierHolder>(e => e.Owned), out _));
    }

    // ------------------------------------------------------------------
    // Helpers and model types
    // ------------------------------------------------------------------

    private static Expression Body<T>(Expression<Func<T, object?>> selector)
        => selector.Body is UnaryExpression { NodeType: ExpressionType.Convert } u ? u.Operand : selector.Body;

    private static Expression Predicate<T>(Expression<Func<T, bool>> predicate)
        => predicate.Body;

    private static IEntityType EntityType<TEntity>(Action<ModelBuilder> configure, bool camelCase = false)
        where TEntity : class
    {
        using var db = new TestContext<TEntity>(
            mb =>
            {
                mb.Entity<TEntity>();
                configure(mb);
            },
            camelCase);
        return db.Model.FindEntityType(typeof(TEntity))
               ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not in the model.");
    }

    public class Addr
    {
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
    }

    public struct Pt
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public class NestedAddr
    {
        public string City { get; set; } = null!;
        public Pt Location { get; set; }
    }

    public class CamelAddr
    {
        public string StreetName { get; set; } = null!;
    }

    public class KeyLikeAddr
    {
        public string A { get; set; } = null!;
    }

    public class OwnedGeo
    {
        public string Country { get; set; } = null!;
    }

    public class OwnedAddress
    {
        public string City { get; set; } = null!;
        public OwnedGeo Geo { get; set; } = null!;
    }

    public class OwnedNote
    {
        public string Text { get; set; } = null!;
    }

    public class OwnedNotesAddress
    {
        public string City { get; set; } = null!;
        public List<OwnedNote> Notes { get; set; } = [];
    }

    public class OwnedPost
    {
        public string Title { get; set; } = null!;
    }

    public class KeyedAuthor
    {
        public string City { get; set; } = null!;
        public string Country { get; set; } = null!;
    }

    [ComplexType]
    public class MixedInner
    {
        public string Value { get; set; } = null!;
    }

    [ComplexType]
    public class MixedExtra
    {
        public string Tag { get; set; } = null!;
        public MixedInner Inner { get; set; } = null!;
    }

    public class MixedOwned
    {
        public string Note { get; set; } = null!;
        public MixedExtra Extra { get; set; } = null!;
    }

    public class SimpleOwned
    {
        public string Note { get; set; } = null!;
    }

    class OwnedChainHolder { public int Id { get; set; } public OwnedAddress Address { get; set; } = null!; }
    class OwnedRenamedHolder { public int Id { get; set; } public OwnedAddress Address { get; set; } = null!; }
    class OwnedNavLeafHolder { public int Id { get; set; } public OwnedAddress Address { get; set; } = null!; }
    class KeyedOwnedHolder { public int Id { get; set; } public KeyedAuthor Author { get; set; } = null!; }
    class CompositeRootHolder { public int A { get; set; } public int B { get; set; } }
    class ComplexLeafHolder { public int Id { get; set; } public Addr Address { get; set; } = null!; }
    class MixedHolder { public int Id { get; set; } public MixedOwned Owned { get; set; } = null!; }
    class StructHolder { public int Id { get; set; } public NestedAddr Address { get; set; } = null!; }
    class ComplexRenamedHolder { public int Id { get; set; } public Addr Address { get; set; } = null!; }
    class CamelHolder { public int Id { get; set; } public CamelAddr HomeAddress { get; set; } = null!; }
    class ComplexUnderCompositeHolder { public string A { get; set; } = null!; public int B { get; set; } public KeyLikeAddr Inner { get; set; } = null!; }
    class TwoAddressHolder { public int Id { get; set; } public Addr Home { get; set; } = null!; public Addr Work { get; set; } = null!; }
    class ComplexAsLeafHolder { public int Id { get; set; } public NestedAddr Address { get; set; } = null!; }
    class ZeroHopHolder { public int Id { get; set; } public string Name { get; set; } = null!; }
    class EmptyChainHolder { public int Id { get; set; } }
    class AllHopsHolder { public int Id { get; set; } public MixedOwned Owned { get; set; } = null!; }
    class BadHopCountHolder { public int Id { get; set; } public string Name { get; set; } = null!; }
    class UnknownHolder { public int Id { get; set; } public Addr Address { get; set; } = null!; public SimpleOwned Owned { get; set; } = null!; }
    class ScalarHopHolder { public int Id { get; set; } public string Name { get; set; } = null!; }
    class OwnedCollectionHolder { public int Id { get; set; } public List<OwnedPost> Posts { get; set; } = []; }
    class OwnedCollectionLeafHolder { public int Id { get; set; } public OwnedNotesAddress Address { get; set; } = null!; }
    class NestedCollectionHopHolder { public int Id { get; set; } public OwnedNotesAddress Address { get; set; } = null!; }
    class ReferencingHolder { public int Id { get; set; } public int TargetId { get; set; } public ReferencedEntity Target { get; set; } = null!; }
    class ReferencedEntity { public int Id { get; set; } public string Name { get; set; } = null!; }
    class AnnotatedReferencingHolder { public int Id { get; set; } public int TargetId { get; set; } public AnnotatedReferencedEntity Target { get; set; } = null!; }
    class AnnotatedReferencedEntity { public int Id { get; set; } public string Name { get; set; } = null!; }
    class TranslatorComplexHolder { public int Id { get; set; } public NestedAddr Address { get; set; } = null!; }
    class TranslatorMixedHolder { public int Id { get; set; } public MixedOwned Owned { get; set; } = null!; }
    class TranslatorCollectionNullHolder { public int Id { get; set; } public List<OwnedPost> Posts { get; set; } = []; }
    public class OwnedItems { public string Note { get; set; } = null!; public List<OwnedPost> Items { get; set; } = []; }
    class TranslatorReferenceQuantifierHolder { public int Id { get; set; } public OwnedItems Owned { get; set; } = null!; }
    class TranslatorNullCheckHolder { public int Id { get; set; } public SimpleOwned Owned { get; set; } = null!; public Addr Address { get; set; } = null!; }

#if !EF8 && !EF9
    class ComplexCollectionHolder { public int Id { get; set; } public List<Addr> Addresses { get; set; } = []; }
    class ComplexCollectionLeafHolder { public int Id { get; set; } public List<Addr> Addresses { get; set; } = []; }
    class OptionalHolder { public int Id { get; set; } public Addr? Address { get; set; } }
#endif

    // Generic so each test gets its own context type (EF caches the model per context type).
    class TestContext<TEntity>(Action<ModelBuilder> configure, bool camelCase) : DbContext where TEntity : class
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseMongoDB("mongodb://localhost:27017", "UnitTests")
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            if (camelCase)
            {
                configurationBuilder.Conventions.Add(_ => new CamelCaseElementNameConvention());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => configure(modelBuilder);
    }
}
