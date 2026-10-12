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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Serializers;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// Rewrites EF's post-injection entity-materializer block for a streaming-eligible entity so each native-path
/// row is materialized via a single forward <see cref="IBsonReader"/> pass into typed locals, instead of
/// building a <see cref="BsonDocument"/> DOM.
/// </summary>
/// <remarks>
/// Handles flat (scalar / mapped-array) entities, entities with single (reference) owned sub-documents
/// (recursively, since an owned type may itself own further reference sub-documents), and owned collections
/// whose element carries no navigation of its own (via the forward-fill loop, see <see cref="BuildFillLoop"/>).
/// A non-owned collection navigation, and an owned collection whose element itself carries a further
/// navigation, are rejected upstream by <see cref="StreamingEligibility"/> (which gates the streaming path)
/// and surface here as <see cref="NativeTranslationNotSupportedException"/>.
/// <para>
/// EF's construction/tracking blocks are reused verbatim, with their <c>ValueBufferTryReadValue</c> reads
/// redirected to the typed locals and their <see cref="MaterializationContext"/> value-buffer source replaced
/// by <see cref="ValueBuffer.Empty"/>. EF's <see cref="IncludeExpression"/> structure (and navigation fixup)
/// is preserved; only the value source and the owned null-guard inside each block are replaced.
/// </para>
/// </remarks>
internal sealed class MongoStreamingEntityMaterializerRewriter
{
    private readonly IEntityType _rootEntityType;
    private readonly BsonSerializerFactory _bsonSerializerFactory;

    // Scalar property serializers bake in at compile time via the static BsonSerializerFactory
    // .GetPropertySerializationInfo (see BuildTypedRead); complex-collection reads use the instance factory's
    // cached complex serializers (see BuildComplexCollectionRead).
    public MongoStreamingEntityMaterializerRewriter(
        IEntityType rootEntityType, BsonSerializerFactory bsonSerializerFactory)
    {
        _rootEntityType = rootEntityType;
        _bsonSerializerFactory = bsonSerializerFactory;
    }

    private static readonly MethodInfo ReadStartDocumentMethod =
        typeof(IBsonReader).GetMethod(nameof(IBsonReader.ReadStartDocument))!;

    private static readonly MethodInfo ReadEndDocumentMethod =
        typeof(IBsonReader).GetMethod(nameof(IBsonReader.ReadEndDocument))!;

    private static readonly MethodInfo ReadBsonTypeMethod =
        typeof(IBsonReader).GetMethod(nameof(IBsonReader.ReadBsonType))!;

    private static readonly MethodInfo ReadNameMethod =
        typeof(IBsonReaderExtensions).GetMethod(
            nameof(IBsonReaderExtensions.ReadName), [typeof(IBsonReader)])!;

    private static readonly MethodInfo GetCurrentBsonTypeMethod =
        typeof(IBsonReader).GetMethod(nameof(IBsonReader.GetCurrentBsonType))!;

    private static readonly MethodInfo ReadNullMethod =
        typeof(IBsonReader).GetMethod(nameof(IBsonReader.ReadNull))!;

    private static readonly MethodInfo SkipValueMethod =
        typeof(IBsonReader).GetMethod(nameof(IBsonReader.SkipValue))!;

    private static readonly MethodInfo DeserializeMethod =
        typeof(IBsonSerializer).GetMethod(
            nameof(IBsonSerializer.Deserialize),
            [typeof(BsonDeserializationContext), typeof(BsonDeserializationArgs)])!;

    private static readonly MethodInfo StringEqualsMethod =
        typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(string)])!;

    /// <summary>
    /// A per-entity-instance materialization plan: the typed locals for its scalar properties, an optional
    /// "present" flag local (owned sub-documents only), and the plans for its owned reference navigations.
    /// </summary>
    private sealed class EntityPlan
    {
        public required IEntityType EntityType { get; init; }
        public required Dictionary<IProperty, ParameterExpression> Locals { get; init; }

        /// <summary>
        /// Presence flag per required non-nullable scalar, set by the fill loop when the element is encountered
        /// (even as explicit BSON <c>null</c>). Still <c>false</c> after the loop means the element was missing,
        /// and the materializer throws the same <see cref="InvalidOperationException"/> as
        /// <c>BsonBinding.GetPropertyValue</c>.
        /// </summary>
        public required Dictionary<IProperty, ParameterExpression> RequiredPresence { get; init; }

        public ParameterExpression? Present { get; init; }
        public required List<(INavigationBase Navigation, EntityPlan Child)> OwnedNavigations { get; init; }
        public required List<CollectionPlan> OwnedCollections { get; init; }
        public required List<LookupReferencePlan> LookupReferences { get; init; }
        public required List<ComplexPlan> Complexes { get; init; }
        public required List<ComplexCollectionPlan> ComplexCollections { get; init; }

        /// <summary>
        /// The property→local scope this plan's construction block reads from. The root entity and its owned
        /// sub-document subtree share ONE scope (owned-type keys resolve to the principal's local via
        /// <see cref="ConstructionRewriter.ResolveLocal"/>). A lookup-backed non-owned reference target gets its
        /// OWN fresh scope, since it is an independent entity instance — for a self-referential reference (e.g.
        /// <c>Employee.Manager</c>) sharing the root's scope would alias the target's locals onto the root's
        /// identical <see cref="IProperty"/> keys and corrupt both reads.
        /// </summary>
        public required Dictionary<IProperty, ParameterExpression> AllLocals { get; init; }
    }

    /// <summary>
    /// A plan for a non-owned single (reference) navigation materialized from a cross-collection
    /// <c>$lookup</c> + <c>$unwind</c>. Unlike an owned reference (which descends into an embedded element
    /// mid-parse), the joined sub-document arrives as a ROOT-level element named
    /// <see cref="LookupExpression.GetLookupAlias"/> (<c>_lookup_&lt;Nav&gt;</c>) — a sibling of the parent's
    /// own fields after <c>$unwind</c>. The joined entity reads its own primary key as a normal field (no
    /// owner-key resolution) and does its own tracking. The target plan's present flag is false when the lookup
    /// field is BSON Null (no match), yielding a null navigation.
    /// </summary>
    private sealed class LookupReferencePlan
    {
        public required INavigation Navigation { get; init; }
        public required EntityPlan Target { get; init; }
        public required string ElementName { get; init; }
    }

    /// <summary>
    /// A plan for an owned <em>collection</em> navigation: the element materialization plan, a 1-based loop
    /// <see cref="Counter"/> local that supplies the synthesized ordinal key, and a <see cref="List"/>
    /// accumulator local of the element CLR type. Element locals are declared once (in the element plan) and
    /// reassigned on each array iteration.
    /// </summary>
    private sealed class CollectionPlan
    {
        public required INavigation Navigation { get; init; }
        public required EntityPlan Element { get; init; }
        public required ParameterExpression Counter { get; init; }
        public required ParameterExpression List { get; init; }

        // The rewritten per-element construction expression (element locals + counter -> CLR element), set by
        // RewriteMaterializer from the CollectionShaperExpression's inner shaper, consumed by BuildFillLoop to
        // emit `list.Add(<constructed element>)` inside the array loop.
        public Expression? ElementConstructor { get; set; }
    }

    /// <summary>
    /// A plan for one complex property's sub-document: the typed locals for its scalar leaf properties (with
    /// required-presence flags, exactly as an <see cref="EntityPlan"/> tracks them), a presence flag for the
    /// sub-document itself, and the plans for nested complex properties (recursively). Complex properties share the
    /// surrounding entity's locals scope — their leaf reads resolve by the same <see cref="IProperty"/> identity.
    /// A complex <em>collection</em> is not planned here: it is rejected upstream by <see cref="StreamingEligibility"/>
    /// (EF materializes collection members separately from the structural-type block), so the DOM shaper handles it.
    /// </summary>
    private sealed class ComplexPlan
    {
        public required IComplexProperty ComplexProperty { get; init; }
        public required string ElementName { get; init; }
        public required ParameterExpression Present { get; init; }
        public required Dictionary<IProperty, ParameterExpression> Locals { get; init; }
        public required Dictionary<IProperty, ParameterExpression> RequiredPresence { get; init; }
        public required List<ComplexPlan> NestedComplexes { get; init; }

        /// <summary>
        /// Whether any complex property on the path to (and including) this one is nullable. Leaves under a
        /// nullable complex read an explicit BSON <c>null</c> as <c>default</c> — EF threads the complex property's
        /// nullability into its leaves' materialization (<c>StructuralTypeMaterializerSource</c>'s
        /// <c>parameters.IsNullable</c>), rather than storing it on each leaf.
        /// </summary>
        public bool NullableContext { get; init; }

        /// <summary>Required (non-nullable) complex properties throw when the sub-document is missing; nullable ones
        /// leave their leaves at <c>default</c>, which EF's own all-leaves-null condition turns into a default value.</summary>
        public bool IsRequired => !ComplexProperty.IsNullable;
    }

    /// <summary>
    /// A plan for a (root-level) complex <em>collection</em> property: the whole stored array is read in one pass
    /// through the complex property's own collection serializer into a <see cref="List"/> local, rather than
    /// per-element. A <see cref="Present"/> flag distinguishes a MISSING element (assignment skipped, so the
    /// constructed instance's own field initializer survives — driver-LINQ parity) from an explicit BSON null
    /// (assigned as null — also driver-LINQ parity). Nested complex collections are not planned here; they are
    /// declined by <see cref="StreamingEligibility"/>.
    /// </summary>
    private sealed class ComplexCollectionPlan
    {
        public required IComplexProperty ComplexProperty { get; init; }
        public required string ElementName { get; init; }
        public required ParameterExpression List { get; init; }
        public required ParameterExpression Present { get; init; }
    }

    private static readonly MethodInfo ReadStartArrayMethod =
        typeof(IBsonReader).GetMethod(nameof(IBsonReader.ReadStartArray))!;

    private static readonly MethodInfo ReadEndArrayMethod =
        typeof(IBsonReader).GetMethod(nameof(IBsonReader.ReadEndArray))!;

    // Set by Rewrite from its readerParameter argument. The caller owns opening/positioning/disposing the
    // reader; this instance only ever serves a single Rewrite call, so a plain field is sufficient.
    private ParameterExpression _reader = null!;

    // The per-document BsonDeserializationContext threaded in from MongoEntityMaterializerSerializer.Deserialize
    // (its Reader IS _reader). Reused for every per-property typed read of the row — including nested owned
    // sub-documents, which share the same reader — so no context is allocated per property.
    private ParameterExpression _context = null!;
    private readonly ParameterExpression _name = Expression.Variable(typeof(string), "__name");

    /// <summary>
    /// Rewrites the post-injection materializer into a forward-streaming one reading from
    /// <paramref name="readerParameter"/>. The caller opens, positions and disposes the reader.
    /// </summary>
    public BlockExpression Rewrite(
        Expression injectedBody, ParameterExpression readerParameter, ParameterExpression contextParameter)
    {
        _reader = readerParameter;
        _context = contextParameter;
        var resultType = injectedBody.Type;

        var rootPlan = BuildPlan(_rootEntityType, present: null, new Dictionary<IProperty, ParameterExpression>());

        var rewrittenBody = RewriteMaterializer(injectedBody, rootPlan);

        var fillLoop = BuildFillLoop(rootPlan);

        var allLocals = new List<ParameterExpression> { _name };
        var initializers = new List<Expression>();
        CollectLocals(rootPlan, allLocals, initializers);

        var body = new List<Expression> { Expression.Call(_reader, ReadStartDocumentMethod) };
        body.AddRange(initializers);
        body.Add(fillLoop);
        body.Add(Expression.Call(_reader, ReadEndDocumentMethod));
        body.Add(rewrittenBody);

        return Expression.Block(resultType, allLocals, body);
    }

    /// <summary>
    /// Builds a plan for <paramref name="entityType"/> (recursively for owned navigations). Rejects a non-owned
    /// collection navigation with <see cref="NativeTranslationNotSupportedException"/>; an owned collection whose
    /// element carries a further navigation is rejected earlier, by <see cref="StreamingEligibility"/>.
    /// </summary>
    private EntityPlan BuildPlan(
        IEntityType entityType,
        ParameterExpression? present,
        Dictionary<IProperty, ParameterExpression> allLocals,
        bool allowLookupReferences = true)
    {
        var locals = new Dictionary<IProperty, ParameterExpression>();
        var requiredPresence = new Dictionary<IProperty, ParameterExpression>();
        foreach (var property in entityType.GetProperties())
        {
            // Owned-type keys (shadow FKs sharing the principal's primary key) live only on the owner document,
            // not the owned sub-document. They get no local and no fill-loop entry; reads resolve to the
            // principal's local via ConstructionRewriter.ResolveLocal.
            if (property.IsOwnedTypeKey())
            {
                continue;
            }

            var local = Expression.Variable(property.ClrType, "__p_" + entityType.ShortName() + "_" + property.Name);
            locals[property] = local;
            allLocals[property] = local;

            // A required (non-nullable) scalar gets a presence flag so a MISSING element throws, matching the
            // DOM / driver-LINQ binding path, rather than silently materializing default(T).
            if (!property.IsNullable)
            {
                requiredPresence[property] =
                    Expression.Variable(typeof(bool), "__present_p_" + entityType.ShortName() + "_" + property.Name);
            }
        }

        var ownedNavigations = new List<(INavigationBase, EntityPlan)>();
        var ownedCollections = new List<CollectionPlan>();
        var lookupReferences = new List<LookupReferencePlan>();
        var complexes = new List<ComplexPlan>();
        var complexCollections = new List<ComplexCollectionPlan>();
        foreach (var complexProperty in entityType.GetComplexProperties())
        {
            if (complexProperty.IsCollection)
            {
                // Root-level complex collection: the whole array reads in one pass through the property's own
                // collection serializer (see ComplexCollectionPlan). A nested one is declined upstream by
                // StreamingEligibility.
                var list = Expression.Variable(
                    complexProperty.ClrType, "__list_c_" + complexProperty.Name);
                var collectionPresent = Expression.Variable(typeof(bool), "__present_c_" + complexProperty.Name);
                complexCollections.Add(new ComplexCollectionPlan
                {
                    ComplexProperty = complexProperty,
                    ElementName = complexProperty.GetElementName(),
                    List = list,
                    Present = collectionPresent
                });
                continue;
            }

            complexes.Add(BuildComplexPlan(complexProperty, allLocals, inheritedNullable: false));
        }

        foreach (var navigation in entityType.GetNavigations())
        {
            var target = navigation.TargetEntityType;

            if (!target.IsOwned())
            {
                // Non-owned reference navigations are planned only one level deep, off the root entity: a
                // lookup-backed target does not plan its own further non-owned references, since a
                // self-referential relationship (e.g. Staff.Manager) would otherwise recurse forever here. A
                // deeper (ThenInclude) non-owned reference has no LookupReferencePlan, so
                // RewriteLookupReferenceNavigation rejects it and falls back to the DOM path.
                if (!allowLookupReferences)
                {
                    continue;
                }

                // A non-owned navigation is only streamable as a single reference backed by a cross-collection
                // $lookup + $unwind. The joined sub-document arrives as a root-level `_lookup_<Nav>` element (a
                // sibling of this entity's own fields); its primary key is a normal field of the joined
                // document, read without owner-key resolution. A non-owned collection is not streamable.
                if (navigation.IsCollection)
                {
                    throw new NativeTranslationNotSupportedException(
                        $"Streaming materialization of navigation '{entityType.DisplayName()}.{navigation.Name}' is not supported "
                        + "(non-owned collection navigation).");
                }

                // The joined target is an independent entity instance: it gets its own fresh locals scope, not
                // the root's — critical for self-referential references where target and root share IProperty
                // keys. Non-owned reference recursion is disabled (single-level only).
                var lookupPresent = Expression.Variable(typeof(bool), "__present_lookup_" + target.ShortName());
                var lookupTarget = BuildPlan(
                    target, lookupPresent, new Dictionary<IProperty, ParameterExpression>(),
                    allowLookupReferences: false);
                lookupReferences.Add(new LookupReferencePlan
                {
                    Navigation = navigation,
                    Target = lookupTarget,
                    ElementName = LookupExpression.GetLookupAlias(navigation)
                });
                continue;
            }

            if (navigation.IsCollection)
            {
                // Owned collection: element plan locals are reused across iterations (no present flag —
                // presence is per-array-element, governed by the loop). A 1-based counter local supplies the
                // synthesized ordinal key; a List<TElement> accumulator collects the materialized elements.
                var element = BuildPlan(target, present: null, allLocals, allowLookupReferences);
                var counter = Expression.Variable(typeof(int), "__counter_" + target.ShortName());
                var listType = typeof(List<>).MakeGenericType(target.ClrType);
                var list = Expression.Variable(listType, "__list_" + target.ShortName());
                ownedCollections.Add(new CollectionPlan
                {
                    Navigation = navigation,
                    Element = element,
                    Counter = counter,
                    List = list
                });
                continue;
            }

            var childPresent = Expression.Variable(typeof(bool), "__present_" + target.ShortName());
            var child = BuildPlan(target, childPresent, allLocals, allowLookupReferences);
            ownedNavigations.Add((navigation, child));
        }

        return new EntityPlan
        {
            EntityType = entityType,
            Locals = locals,
            RequiredPresence = requiredPresence,
            Present = present,
            OwnedNavigations = ownedNavigations,
            OwnedCollections = ownedCollections,
            LookupReferences = lookupReferences,
            Complexes = complexes,
            ComplexCollections = complexCollections,
            AllLocals = allLocals
        };
    }

    /// <summary>
    /// Builds the plan for one complex property (recursively for nested complex properties). Leaf locals join the
    /// shared <paramref name="allLocals"/> scope so the construction block's <c>ValueBufferTryReadValue</c> reads for
    /// complex leaves resolve exactly like the entity's own — the sub-document descent is what scopes them, since a
    /// leaf's fill only runs while the reader is positioned inside its sub-document.
    /// </summary>
    private static ComplexPlan BuildComplexPlan(
        IComplexProperty complexProperty,
        Dictionary<IProperty, ParameterExpression> allLocals,
        bool inheritedNullable)
    {
        var plan = new ComplexPlan
        {
            ComplexProperty = complexProperty,
            ElementName = complexProperty.GetElementName(),
            Present = Expression.Variable(typeof(bool), "__present_c_" + complexProperty.Name),
            Locals = new Dictionary<IProperty, ParameterExpression>(),
            RequiredPresence = new Dictionary<IProperty, ParameterExpression>(),
            NestedComplexes = new List<ComplexPlan>(),
            NullableContext = inheritedNullable || complexProperty.IsNullable
        };

        FillComplexTypePlan(complexProperty.ComplexType, plan, allLocals);
        return plan;
    }

    private static void FillComplexTypePlan(
        IComplexType complexType,
        ComplexPlan plan,
        Dictionary<IProperty, ParameterExpression> allLocals)
    {
        foreach (var property in complexType.GetProperties())
        {
            // A leaf of a nullable complex property is effectively nullable (its null contributes to EF's
            // all-leaves-null condition), even when its own CLR shape is not: value-type leaves read into
            // nullable locals so the condition sees a true null (not a boxed default) for them too.
            var localType = plan.NullableContext && property.ClrType.IsValueType
                ? property.ClrType.MakeNullable()
                : property.ClrType;
            var local = Expression.Variable(localType, "__p_" + complexType.ShortName() + "_" + property.Name);
            plan.Locals[property] = local;
            allLocals[property] = local;

            if (!property.IsNullable && !plan.NullableContext)
            {
                plan.RequiredPresence[property] =
                    Expression.Variable(typeof(bool), "__present_p_" + complexType.ShortName() + "_" + property.Name);
            }
        }

        foreach (var nested in complexType.GetComplexProperties())
        {
            var nestedPlan = BuildComplexPlan(nested, allLocals, plan.NullableContext);
            plan.NestedComplexes.Add(nestedPlan);
        }
    }

    private void CollectLocals(EntityPlan plan, List<ParameterExpression> locals, List<Expression> initializers)
    {
        if (plan.Present != null)
        {
            locals.Add(plan.Present);
            initializers.Add(Expression.Assign(plan.Present, Expression.Constant(false)));
        }

        foreach (var local in plan.Locals.Values)
        {
            locals.Add(local);
            initializers.Add(Expression.Assign(local, Expression.Default(local.Type)));
        }

        // Required-scalar presence flags: declared + reset to false per row. The same locals are reused across
        // owned-collection iterations, so they're also re-initialized each pass inside the fill loop itself.
        foreach (var present in plan.RequiredPresence.Values)
        {
            locals.Add(present);
            initializers.Add(Expression.Assign(present, Expression.Constant(false)));
        }

        foreach (var (_, child) in plan.OwnedNavigations)
        {
            CollectLocals(child, locals, initializers);
        }

        foreach (var lookup in plan.LookupReferences)
        {
            CollectLocals(lookup.Target, locals, initializers);
        }

        foreach (var collection in plan.OwnedCollections)
        {
            locals.Add(collection.Counter);
            initializers.Add(Expression.Assign(collection.Counter, Expression.Constant(0)));
            locals.Add(collection.List);
            initializers.Add(Expression.Assign(collection.List, Expression.Default(collection.List.Type)));

            // Element locals are shared across iterations; declare + default-init them once here.
            CollectLocals(collection.Element, locals, initializers);
        }

        foreach (var complex in plan.Complexes)
        {
            CollectComplexLocals(complex, locals, initializers);
        }

        foreach (var complexCollection in plan.ComplexCollections)
        {
            locals.Add(complexCollection.Present);
            initializers.Add(Expression.Assign(complexCollection.Present, Expression.Constant(false)));
            locals.Add(complexCollection.List);
            initializers.Add(Expression.Assign(complexCollection.List, Expression.Default(complexCollection.List.Type)));
        }
    }

    private static void CollectComplexLocals(ComplexPlan plan, List<ParameterExpression> locals, List<Expression> initializers)
    {
        locals.Add(plan.Present);
        initializers.Add(Expression.Assign(plan.Present, Expression.Constant(false)));

        foreach (var local in plan.Locals.Values)
        {
            locals.Add(local);
            initializers.Add(Expression.Assign(local, Expression.Default(local.Type)));
        }

        foreach (var present in plan.RequiredPresence.Values)
        {
            locals.Add(present);
            initializers.Add(Expression.Assign(present, Expression.Constant(false)));
        }

        foreach (var nested in plan.NestedComplexes)
        {
            CollectComplexLocals(nested, locals, initializers);
        }
    }

    /// <summary>
    /// Builds the forward name-dispatch fill loop for one document level (reader already after
    /// <c>ReadStartDocument</c>).
    /// </summary>
    private Expression BuildFillLoop(EntityPlan plan)
    {
        var ifChain = (Expression)Expression.Call(_reader, SkipValueMethod);

        foreach (var property in plan.EntityType.GetProperties())
        {
            // Owned-type keys have no local and are not stored in the sub-document; skip them in the loop.
            if (!plan.Locals.TryGetValue(property, out var local))
            {
                continue;
            }

            // Mark present before reading (not inside BuildTypedRead): a present-but-null required scalar must
            // take BuildTypedRead's null handling, not the post-loop missing-required throw.
            Expression read = BuildTypedRead(property, local);
            if (plan.RequiredPresence.TryGetValue(property, out var presenceFlag))
            {
                read = Expression.Block(
                    Expression.Assign(presenceFlag, Expression.Constant(true)),
                    read);
            }

            ifChain = Dispatch(property.GetElementName(), read, ifChain);
        }

        foreach (var (navigation, child) in plan.OwnedNavigations)
        {
            var elementName = navigation.TargetEntityType.GetContainingElementName()
                              ?? throw new NativeTranslationNotSupportedException(
                                  $"Owned navigation '{navigation.DeclaringEntityType.DisplayName()}.{navigation.Name}' has no element name.");

            ifChain = Dispatch(elementName, BuildNullGuardedDescent(child), ifChain);
        }

        foreach (var lookup in plan.LookupReferences)
        {
            // The joined sub-document is a root-level `_lookup_<Nav>` element (post-$unwind sibling of this
            // entity's own fields). Same null-guarded descent as an owned reference, but keyed by the lookup
            // alias rather than an embedded containing-element name. BSON Null (no $lookup match) -> present
            // = false -> null navigation.
            ifChain = Dispatch(lookup.ElementName, BuildNullGuardedDescent(lookup.Target), ifChain);
        }

        foreach (var collection in plan.OwnedCollections)
        {
            var elementName = collection.Navigation.TargetEntityType.GetContainingElementName()
                              ?? throw new NativeTranslationNotSupportedException(
                                  $"Owned collection '{collection.Navigation.DeclaringEntityType.DisplayName()}.{collection.Navigation.Name}' has no element name.");

            ifChain = Dispatch(elementName, BuildCollectionLoop(collection), ifChain);
        }

        foreach (var complex in plan.Complexes)
        {
            ifChain = Dispatch(complex.ElementName, BuildComplexDescent(complex), ifChain);
        }

        foreach (var complexCollection in plan.ComplexCollections)
        {
            ifChain = Dispatch(complexCollection.ElementName, BuildComplexCollectionRead(complexCollection), ifChain);
        }

        var breakTarget = Expression.Label("__fillDone_" + plan.EntityType.ShortName());
        var loop = Expression.Loop(
            Expression.IfThenElse(
                Expression.NotEqual(
                    Expression.Call(_reader, ReadBsonTypeMethod),
                    Expression.Constant(BsonType.EndOfDocument, typeof(BsonType))),
                Expression.Block(
                    Expression.Assign(_name, Expression.Call(ReadNameMethod, _reader)),
                    ifChain),
                Expression.Break(breakTarget)),
            breakTarget);

        // Nothing to enforce or normalize after the loop: the loop is the whole fill.
        if (plan.RequiredPresence.Count == 0
            && plan.OwnedCollections.Count == 0
            && plan.Complexes.All(c => !c.IsRequired))
        {
            return loop;
        }

        // Reset presence flags and collection accumulators before each pass, and enforce/normalize after.
        // Owned-collection element plans reuse their locals across iterations, so element N's state must not leak
        // into element N+1 (for once-only plans the reset is redundant but harmless). A flag still false after the
        // loop throws like BsonBinding.GetPropertyValue.
        var body = new List<Expression>();
        foreach (var (_, presenceFlag) in plan.RequiredPresence)
        {
            body.Add(Expression.Assign(presenceFlag, Expression.Constant(false)));
        }

        foreach (var collection in plan.OwnedCollections)
        {
            body.Add(Expression.Assign(collection.List, Expression.Default(collection.List.Type)));
        }

        body.Add(loop);

        // Normalize an absent owned collection (missing element, or explicit BSON Null consumed by
        // BuildCollectionLoop) to an empty accumulator. EF Core's contract is an empty collection either way,
        // matching the DOM shaper; a null accumulator makes IncludeCollection skip GetOrCreate, so the result would
        // depend on the POCO's field initializer. The navigation's own IClrCollectionAccessor creates the actual
        // collection, so non-List types such as HashSet<T> still work.
        foreach (var collection in plan.OwnedCollections)
        {
            body.Add(
                Expression.IfThen(
                    Expression.Equal(collection.List, Expression.Constant(null, collection.List.Type)),
                    Expression.Assign(collection.List, Expression.New(collection.List.Type))));
        }

        foreach (var (property, presenceFlag) in plan.RequiredPresence)
        {
            body.Add(
                Expression.IfThen(
                    Expression.Not(presenceFlag),
                    Expression.Throw(
                        Expression.New(
                            InvalidOperationExceptionCtor,
                            Expression.Constant(Storage.BsonBinding.RequiredPropertyMissingMessage(property))))));
        }

        // A required complex property whose sub-document is missing (never dispatched) or BSON Null throws, like
        // the DOM path's "required but not present" for owned references. A nullable complex leaves its leaves at
        // default; EF's own all-leaves-null condition then materializes default(T) — structural-null semantics.
        foreach (var complex in plan.Complexes)
        {
            if (!complex.IsRequired)
            {
                continue;
            }

            body.Add(
                Expression.IfThen(
                    Expression.Not(complex.Present),
                    Expression.Throw(
                        Expression.New(
                            InvalidOperationExceptionCtor,
                            Expression.Constant(
                                $"Field '{complex.ElementName}' required but not present in BsonDocument for a '{plan.EntityType.DisplayName()}'.")))));
        }

        return Expression.Block(body);
    }

    // One link of the fill loop's element-name dispatch: run `body` when the current element is `elementName`,
    // else fall through to `chain`.
    private Expression Dispatch(string elementName, Expression body, Expression chain)
        => Expression.IfThenElse(
            Expression.Call(StringEqualsMethod, _name, Expression.Constant(elementName, typeof(string))),
            body,
            chain);

    // Whether the reader's current value is BSON Null.
    private Expression IsCurrentNull()
        => Expression.Equal(
            Expression.Call(_reader, GetCurrentBsonTypeMethod),
            Expression.Constant(BsonType.Null, typeof(BsonType)));

    // Descends into an embedded sub-document read into `plan` (an owned reference or a joined `_lookup_<Nav>`). If
    // the element is BSON Null the sub-document is absent: ReadNull + present=false. Otherwise: present=true, descend
    // (ReadStartDocument / sub-fill-loop / ReadEndDocument).
    private Expression BuildNullGuardedDescent(EntityPlan plan)
        => Expression.IfThenElse(
            IsCurrentNull(),
            Expression.Block(
                Expression.Call(_reader, ReadNullMethod),
                Expression.Assign(plan.Present!, Expression.Constant(false))),
            Expression.Block(
                Expression.Assign(plan.Present!, Expression.Constant(true)),
                Expression.Call(_reader, ReadStartDocumentMethod),
                BuildFillLoop(plan),
                Expression.Call(_reader, ReadEndDocumentMethod)));

    /// <summary>
    /// Build the array loop for an owned collection. The reader is positioned at the array value (after the
    /// element name). If the value is BSON Null the null is consumed and the accumulator is left NULL — the
    /// post-loop normalization in <see cref="BuildFillLoop"/> turns that into an empty accumulator, which also
    /// covers a MISSING array element (this method never runs for one). Otherwise each array element is read
    /// into the element plan's locals (reassigned per iteration), the 1-based <c>counter</c> supplies the
    /// synthesized ordinal key, and the constructed element is appended.
    /// </summary>
    private Expression BuildCollectionLoop(CollectionPlan collection)
    {
        var listType = collection.List.Type;
        var addMethod = listType.GetMethod(nameof(List<object>.Add))!;
        var elementConstructor = collection.ElementConstructor
                                 ?? throw new NativeTranslationNotSupportedException(
                                     $"Owned collection '{collection.Navigation.Name}' element construction was not prepared.");

        var elementBreak = Expression.Label("__elemDone_" + collection.Element.EntityType.ShortName());

        var arrayBody = Expression.Block(
            Expression.Assign(collection.Counter, Expression.Constant(0)),
            Expression.Assign(collection.List, Expression.New(listType)),
            Expression.Call(_reader, ReadStartArrayMethod),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.NotEqual(
                        Expression.Call(_reader, ReadBsonTypeMethod),
                        Expression.Constant(BsonType.EndOfDocument, typeof(BsonType))),
                    Expression.Block(
                        Expression.Call(_reader, ReadStartDocumentMethod),
                        BuildFillLoop(collection.Element),
                        Expression.Call(_reader, ReadEndDocumentMethod),
                        // The synthesized ordinal key resolves to `counter + 1` (1-based, matching the DOM
                        // path's `ordinal + 1`); construct, append, then advance the 0-based counter.
                        Expression.Call(collection.List, addMethod, elementConstructor),
                        Expression.AddAssign(collection.Counter, Expression.Constant(1))),
                    Expression.Break(elementBreak)),
                elementBreak),
            Expression.Call(_reader, ReadEndArrayMethod));

        // BSON Null: consume it and leave the accumulator null; BuildFillLoop normalizes it the same way as a
        // missing array, keeping the two absent states in one place.
        return Expression.IfThenElse(
            IsCurrentNull(),
            Expression.Call(_reader, ReadNullMethod),
            arrayBody);
    }

    // Descends into a complex property's sub-document. If the element is BSON Null the sub-document is absent:
    // ReadNull + present=false. Otherwise: present=true, descend (ReadStartDocument / complex fill loop /
    // ReadEndDocument). A MISSING element never dispatches — present stays false, which the caller's post-loop
    // enforcement turns into a throw for a required complex and into default leaves for a nullable one.
    private Expression BuildComplexDescent(ComplexPlan plan)
        => Expression.IfThenElse(
            IsCurrentNull(),
            Expression.Block(
                Expression.Call(_reader, ReadNullMethod),
                Expression.Assign(plan.Present, Expression.Constant(false))),
            Expression.Block(
                Expression.Assign(plan.Present, Expression.Constant(true)),
                Expression.Call(_reader, ReadStartDocumentMethod),
                BuildComplexFillLoop(plan),
                Expression.Call(_reader, ReadEndDocumentMethod)));

    /// <summary>
    /// Reads a complex collection's whole stored array in one pass through the complex property's own collection
    /// serializer (the reader is positioned at the array value, after the element name). An explicit BSON null is
    /// consumed and leaves the <see cref="ComplexCollectionPlan.List"/> local <c>null</c>; a MISSING element never
    /// dispatches. Either way <see cref="ComplexCollectionPlan.Present"/> records that the element was seen, so the
    /// construction block's member assignment (skipped when absent) can preserve the instance's own field
    /// initializer — driver-LINQ parity for the missing/null states.
    /// </summary>
    private Expression BuildComplexCollectionRead(ComplexCollectionPlan plan)
    {
        var serializer = _bsonSerializerFactory.GetComplexPropertySerializationInfo(plan.ComplexProperty).Serializer;

        // The same reuse-the-per-row-context deserialize BuildTypedRead emits, into the List local.
        var valueType = serializer.ValueType;
        var genericSerializerType = typeof(IBsonSerializer<>).MakeGenericType(valueType);

        Expression deserialize;
        if (genericSerializerType.IsInstanceOfType(serializer))
        {
            var typedDeserialize = genericSerializerType.GetMethod(
                nameof(IBsonSerializer.Deserialize),
                [typeof(BsonDeserializationContext), typeof(BsonDeserializationArgs)])!;

            deserialize = Expression.Call(
                Expression.Constant(serializer, genericSerializerType),
                typedDeserialize,
                _context,
                Expression.Default(typeof(BsonDeserializationArgs)));
        }
        else
        {
            Expression boxedCall = Expression.Call(
                Expression.Constant(serializer, typeof(IBsonSerializer)),
                DeserializeMethod,
                _context,
                Expression.Default(typeof(BsonDeserializationArgs)));

            deserialize = Expression.Convert(boxedCall, plan.List.Type);
        }

        return Expression.IfThenElse(
            IsCurrentNull(),
            Expression.Block(
                Expression.Call(_reader, ReadNullMethod),
                Expression.Assign(plan.Present, Expression.Constant(true))),
            Expression.Block(
                Expression.Assign(plan.Present, Expression.Constant(true)),
                Expression.Assign(plan.List, deserialize.ConvertIfRequired(plan.List.Type))));
    }

    /// <summary>
    /// The forward name-dispatch fill loop for one complex sub-document level (reader already after
    /// <c>ReadStartDocument</c>): scalar leaves read into typed locals with required-presence enforcement,
    /// nested complex properties descend recursively. Mirrors <see cref="BuildFillLoop"/>, minus keys (complex
    /// types have none) and collections — a complex collection NESTED inside a complex property is declined by
    /// <see cref="StreamingEligibility"/> (a ROOT-level one never appears here: the entity-level fill loop
    /// dispatches it to <see cref="BuildComplexCollectionRead"/>).
    /// </summary>
    private Expression BuildComplexFillLoop(ComplexPlan plan)
    {
        var ifChain = (Expression)Expression.Call(_reader, SkipValueMethod);

        foreach (var property in plan.ComplexProperty.ComplexType.GetProperties())
        {
            var local = plan.Locals[property];

            // Mark present before reading (not inside BuildTypedRead): a present-but-null required scalar must
            // take BuildTypedRead's null handling, not the post-loop missing-required throw.
            Expression read = BuildTypedRead(property, local, plan.NullableContext);
            if (plan.RequiredPresence.TryGetValue(property, out var presenceFlag))
            {
                read = Expression.Block(
                    Expression.Assign(presenceFlag, Expression.Constant(true)),
                    read);
            }

            ifChain = Dispatch(property.GetElementName(), read, ifChain);
        }

        foreach (var nested in plan.NestedComplexes)
        {
            ifChain = Dispatch(nested.ElementName, BuildComplexDescent(nested), ifChain);
        }

        var breakTarget = Expression.Label("__complexFillDone_" + plan.ComplexProperty.ComplexType.ShortName());
        var loop = Expression.Loop(
            Expression.IfThenElse(
                Expression.NotEqual(
                    Expression.Call(_reader, ReadBsonTypeMethod),
                    Expression.Constant(BsonType.EndOfDocument, typeof(BsonType))),
                Expression.Block(
                    Expression.Assign(_name, Expression.Call(ReadNameMethod, _reader)),
                    ifChain),
                Expression.Break(breakTarget)),
            breakTarget);

        if (plan.RequiredPresence.Count == 0)
        {
            return loop;
        }

        // Reset presence flags before each pass (nested complexes inside a complex COLLECTION element would reuse
        // the locals across iterations) and enforce after, exactly as the entity-level loop does.
        var body = new List<Expression>();
        foreach (var (_, presenceFlag) in plan.RequiredPresence)
        {
            body.Add(Expression.Assign(presenceFlag, Expression.Constant(false)));
        }

        body.Add(loop);

        foreach (var (property, presenceFlag) in plan.RequiredPresence)
        {
            body.Add(
                Expression.IfThen(
                    Expression.Not(presenceFlag),
                    Expression.Throw(
                        Expression.New(
                            InvalidOperationExceptionCtor,
                            Expression.Constant(Storage.BsonBinding.RequiredPropertyMissingMessage(property))))));
        }

        // A required NESTED complex property whose sub-document is missing or BSON Null throws, like a required
        // complex at the entity level; a nullable one leaves its leaves at default for EF's condition.
        foreach (var nested in plan.NestedComplexes)
        {
            if (!nested.IsRequired)
            {
                continue;
            }

            body.Add(
                Expression.IfThen(
                    Expression.Not(nested.Present),
                    Expression.Throw(
                        Expression.New(
                            InvalidOperationExceptionCtor,
                            Expression.Constant(
                                $"Field '{nested.ElementName}' required but not present in BsonDocument for a '{plan.ComplexProperty.ComplexType.DisplayName()}'.")))));
        }

        return Expression.Block(body);
    }

    /// <summary>
    /// Rewrite the materializer expression, preserving any <see cref="IncludeExpression"/> structure.
    /// For a plain entity block the value source is redirected to <paramref name="plan"/>'s locals; for an
    /// <see cref="IncludeExpression"/> the entity block is rewritten with the parent plan and each owned
    /// navigation block is rewritten with the matching child plan, its <c>bsonDocN == null</c> guard
    /// replaced by <c>!present</c>.
    /// </summary>
    private Expression RewriteMaterializer(Expression body, EntityPlan plan, CollectionPlan? collection = null)
    {
        if (body is IncludeExpression include)
        {
            // Reduce the IncludeExpression ourselves (it is not a reducible node). EF's binding remover
            // would normally turn it into a fixup call woven through a BsonDocument; on the streaming path
            // there is no BsonDocument, so we replicate the reference-include fixup directly, splicing it
            // into the (recursively rewritten) entity materializer block before its trailing instance.
            if (include.Navigation is not INavigation navigation)
            {
                throw new NativeTranslationNotSupportedException(
                    $"Streaming materialization of navigation '{include.Navigation.Name}' is not supported.");
            }

            if (navigation.IsCollection)
            {
                var entityBlockForCollection = (BlockExpression)RewriteMaterializer(include.EntityExpression, plan, collection);
                var collectionPlan = FindCollectionPlan(plan, navigation);

                // Locate the CollectionShaperExpression and build the per-element construction (stored on the
                // plan for the array loop to emit); splice the fixup into the parent block, fed List<TElement>.
                BuildCollectionElementConstructor(include.NavigationExpression, collectionPlan);

                return SpliceInclude(entityBlockForCollection, navigation, collectionPlan.List, include.SetLoaded);
            }

            var entityBlock = (BlockExpression)RewriteMaterializer(include.EntityExpression, plan, collection);

            // A non-owned single reference is materialized from the cross-collection $lookup result field
            // (`_lookup_<Nav>`). It uses the same IncludeExpression / reference-fixup shape as an owned
            // reference, so the fixup is spliced in via SpliceInclude the same way; the difference is
            // purely how the joined entity is materialized — from a root-level lookup field, reading its own
            // PK as a normal field.
            if (!navigation.TargetEntityType.IsOwned())
            {
                var lookupPlan = FindLookupReferencePlan(plan, navigation);
                var lookupNavExpression =
                    RewriteLookupReferenceNavigation(include.NavigationExpression, navigation, lookupPlan);

                return SpliceInclude(entityBlock, navigation, lookupNavExpression, include.SetLoaded);
            }

            var child = FindChildPlan(plan, navigation);
            var navExpression = RewriteOwnedNavigation(include.NavigationExpression, navigation, child);

            return SpliceInclude(entityBlock, navigation, navExpression, include.SetLoaded);
        }

        // Plain entity block: { bsonDocN; bsonDocN = projection as BsonDocument; bsonDocN == null ? null : <block> }.
        // The root row is always present, so drop the bsonDocN local + null guard and use the materializer
        // block directly, redirecting its value source to this plan's locals. When building a collection
        // element, `collection` carries the loop counter so the synthesized ordinal key resolves to counter+1.
        var materializerBlock = ExtractMaterializerBlock(body, plan.EntityType);
        return new ConstructionRewriter(plan.AllLocals, collection, plan.ComplexCollections).Visit(materializerBlock);
    }

    /// <summary>
    /// Splice a navigation fixup into a rewritten entity materializer block, mirroring EF's
    /// <c>IncludeReference</c>/<c>IncludeCollection</c> paths. The block's trailing instance expression is preserved; the
    /// fixup call is inserted just before it, using the block's own <c>entry</c> / <c>entityType</c> / <c>instance</c>
    /// locals.
    /// </summary>
    /// <param name="entityBlock">The rewritten materializer block of the including entity.</param>
    /// <param name="navigation">The included navigation.</param>
    /// <param name="relatedEntityExpression">
    /// For a reference navigation, the related entity's materialization. For a collection navigation, the
    /// <c>List&lt;TElement&gt;</c> local filled by the array loop (<c>IncludeCollection&lt;TIncluding,TIncluded&gt;</c>
    /// expects <c>IEnumerable&lt;TIncluded&gt;</c>; <c>List&lt;TElement&gt;</c> qualifies), each element of which
    /// <see cref="MongoIncludeFixups"/>'s <c>IncludeCollection</c> wires onto the principal collection navigation
    /// (and, when tracking, marks it loaded).
    /// </param>
    /// <param name="setLoaded">The <c>IncludeExpression.SetLoaded</c> flag.</param>
    private BlockExpression SpliceInclude(
        BlockExpression entityBlock,
        INavigation navigation,
        Expression relatedEntityExpression,
        bool setLoaded)
    {
        var instanceVariable = entityBlock.Variables.Single(v => v.Type == navigation.DeclaringEntityType.ClrType);
        var concreteEntityTypeVariable = entityBlock.Variables.Single(v => v.Type == typeof(IEntityType));
#pragma warning disable EF1001 // Internal EF Core API usage.
        var entryVariable = entityBlock.Variables.SingleOrDefault(v => v.Type == typeof(InternalEntityEntry));
        Expression entityEntryExpression =
            entryVariable ?? (Expression)Expression.Constant(null, typeof(InternalEntityEntry));
#pragma warning restore EF1001 // Internal EF Core API usage.

        var includeCall = MongoIncludeFixups.CreateIncludeCall(
            navigation, entityEntryExpression, instanceVariable, concreteEntityTypeVariable, relatedEntityExpression,
            setLoaded);

        var expressions = new List<Expression>(entityBlock.Expressions);
        var trailing = expressions[^1];
        expressions[^1] = includeCall;
        expressions.Add(trailing);

        return entityBlock.Update(entityBlock.Variables, expressions);
    }

    /// <summary>
    /// Locate the <see cref="CollectionShaperExpression"/> inside an owned-collection navigation expression and
    /// build the per-element construction expression (element locals + 1-based ordinal counter -> CLR element),
    /// storing it on <paramref name="collectionPlan"/> for the array loop to emit as <c>list.Add(...)</c>.
    /// </summary>
    private void BuildCollectionElementConstructor(Expression navExpression, CollectionPlan collectionPlan)
    {
        var shaper = FindCollectionShaper(navExpression)
                     ?? throw new NativeTranslationNotSupportedException(
                         $"Unexpected owned-collection materializer shape for '{collectionPlan.Element.EntityType.DisplayName()}'.");

        // The inner shaper is the per-element materializer. It may itself be an IncludeExpression (the element
        // owns further references/collections) — RewriteMaterializer handles that recursively, redirecting
        // reads to the element plan's locals, with the collection context threaded through so the synthesized
        // ordinal key resolves to `counter + 1`.
        collectionPlan.ElementConstructor =
            RewriteMaterializer(shaper.InnerShaper, collectionPlan.Element, collectionPlan);
    }

    private static CollectionShaperExpression? FindCollectionShaper(Expression expression)
    {
        switch (expression)
        {
            case CollectionShaperExpression shaper:
                return shaper;
            case ConditionalExpression { IfFalse: { } ifFalse } conditional:
                return FindCollectionShaper(ifFalse) ?? FindCollectionShaper(conditional.IfTrue);
            case BlockExpression block:
                for (var i = block.Expressions.Count - 1; i >= 0; i--)
                {
                    if (FindCollectionShaper(block.Expressions[i]) is { } found)
                    {
                        return found;
                    }
                }

                return null;
            case UnaryExpression unary:
                return FindCollectionShaper(unary.Operand);
            default:
                return null;
        }
    }

    private static CollectionPlan FindCollectionPlan(EntityPlan parent, INavigationBase navigation)
    {
        foreach (var collection in parent.OwnedCollections)
        {
            if (collection.Navigation == navigation || collection.Navigation.Name == navigation.Name)
            {
                return collection;
            }
        }

        throw new NativeTranslationNotSupportedException(
            $"No streaming plan for owned collection '{navigation.DeclaringEntityType.DisplayName()}.{navigation.Name}'.");
    }

    /// <summary>
    /// Rewrite an owned reference navigation's expression. The owned-entity block has the shape
    /// <c>{ bsonDocN; bsonDocN = projection as BsonDocument; return bsonDocN == null ? null : &lt;block&gt;; }</c>;
    /// when the owned type itself owns further references the expression is an <see cref="IncludeExpression"/>
    /// wrapping that block. The materializer block's value source is redirected to the child plan's locals,
    /// any nested owned-reference fixup is spliced in, and the <c>bsonDocN == null</c> presence test is
    /// replaced with <c>!present</c> so an absent owned sub-document yields the null navigation.
    /// </summary>
    private Expression RewriteOwnedNavigation(Expression navExpression, INavigation navigation, EntityPlan child)
    {
        // Locate the owned-entity block carrying the `bsonDocN == null ? null : <block>` guard. When the owned
        // type has its own owned references the navigation is an IncludeExpression whose EntityExpression is
        // that block; otherwise the navigation expression is the block directly.
        var entityExpression = navExpression is IncludeExpression nestedInclude
            ? nestedInclude.EntityExpression
            : navExpression;

        if (entityExpression is not BlockExpression block
            || block.Expressions[^1] is not ConditionalExpression { IfFalse: BlockExpression materializerBlock } conditional)
        {
            throw new NativeTranslationNotSupportedException(
                $"Unexpected owned-navigation materializer shape for '{child.EntityType.DisplayName()}'.");
        }

        // Rewrite the owned materializer block's value source to the child's locals (the owned subtree
        // shares the root's scope, so owned-type keys still resolve to the principal's local).
        var rewrittenBlock = (BlockExpression)new ConstructionRewriter(child.AllLocals).Visit(materializerBlock);

        // Splice in any nested owned-reference fixup (recursively rewriting the nested navigation).
        if (navExpression is IncludeExpression include)
        {
            if (include.Navigation is not INavigation nestedNavigation || nestedNavigation.IsCollection)
            {
                throw new NativeTranslationNotSupportedException(
                    $"Streaming materialization of navigation '{include.Navigation.Name}' is not supported "
                    + "(only single owned reference sub-documents are supported).");
            }

            var nestedChild = FindChildPlan(child, nestedNavigation);
            var nestedNavExpression = RewriteOwnedNavigation(include.NavigationExpression, nestedNavigation, nestedChild);
            rewrittenBlock = SpliceInclude(rewrittenBlock, nestedNavigation, nestedNavExpression, include.SetLoaded);
        }

        // Replace the whole `{ bsonDocN; bsonDocN = ... as BsonDocument; bsonDocN == null ? null : <block> }`
        // with `!present ? <absent> : <rewrittenBlock>`. The bsonDocN local is dropped: its RHS is an unreduced
        // EntityProjectionExpression with no streaming equivalent. For a required owned reference, <absent>
        // throws like the DOM path (BsonBinding.GetBsonDocument) rather than yielding null.
        var absent = navigation.ForeignKey.IsRequiredDependent
            ? (Expression)Expression.Block(
                conditional.Type,
                Expression.Throw(
                    Expression.New(
                        InvalidOperationExceptionCtor,
                        Expression.Constant(
                            $"Field '{navigation.TargetEntityType.GetContainingElementName()}' required but not present "
                            + $"in BsonDocument for a '{navigation.DeclaringEntityType.DisplayName()}'."))),
                Expression.Default(conditional.Type))
            : Expression.Constant(null, conditional.Type);

        return Expression.Condition(
            Expression.Not(child.Present!),
            absent,
            Expression.Convert(rewrittenBlock, conditional.Type));
    }

    /// <summary>
    /// Rewrite a non-owned single (reference) navigation's expression. The joined entity arrives from the
    /// root-level <c>_lookup_&lt;Nav&gt;</c> field (a sibling element of the parent, post-<c>$unwind</c>), so it
    /// is materialized from the target plan's own locals — its primary key read normally as a field of the
    /// joined document (no owner-key resolution; the joined entity does its own tracking). The block has the
    /// same EF shape as an owned reference, so the materializer block is extracted the same way; the
    /// <c>bsonDocN == null</c> guard is replaced by <c>!present</c> (false when the lookup field is BSON Null —
    /// no match — yielding a null navigation). Nested includes (ThenInclude) are not yet streamable.
    /// </summary>
    private Expression RewriteLookupReferenceNavigation(
        Expression navExpression,
        INavigation navigation,
        LookupReferencePlan lookup)
    {
        // A nested include (ThenInclude off this reference) wraps the joined block in another
        // IncludeExpression — not yet streamable.
        if (navExpression is IncludeExpression)
        {
            throw new NativeTranslationNotSupportedException(
                $"Streaming materialization of nested include on navigation "
                + $"'{navigation.DeclaringEntityType.DisplayName()}.{navigation.Name}' is not supported.");
        }

        if (navExpression is not BlockExpression block
            || block.Expressions[^1] is not ConditionalExpression { IfFalse: BlockExpression materializerBlock } conditional)
        {
            throw new NativeTranslationNotSupportedException(
                $"Unexpected lookup-reference materializer shape for '{lookup.Target.EntityType.DisplayName()}'.");
        }

        // Redirect the joined block's value source to the target's own isolated locals scope. Its PK is a
        // normal local (not an owned-type key), so ConstructionRewriter.ResolveLocal finds it directly with no
        // owner-key resolution. Using the target's own scope (not the root's) keeps a self-referential
        // reference from aliasing the root's identical-IProperty locals.
        var rewrittenBlock = (BlockExpression)new ConstructionRewriter(lookup.Target.AllLocals).Visit(materializerBlock);

        // `!present ? null : <rewrittenBlock>` — an absent (BSON Null) lookup field yields a null navigation.
        return Expression.Condition(
            Expression.Not(lookup.Target.Present!),
            Expression.Constant(null, conditional.Type),
            Expression.Convert(rewrittenBlock, conditional.Type));
    }

    private static readonly ConstructorInfo InvalidOperationExceptionCtor =
        typeof(InvalidOperationException).GetConstructor([typeof(string)])!;

    private static LookupReferencePlan FindLookupReferencePlan(EntityPlan parent, INavigationBase navigation)
    {
        foreach (var lookup in parent.LookupReferences)
        {
            if (lookup.Navigation == navigation || lookup.Navigation.Name == navigation.Name)
            {
                return lookup;
            }
        }

        throw new NativeTranslationNotSupportedException(
            $"No streaming plan for lookup reference navigation '{navigation.DeclaringEntityType.DisplayName()}.{navigation.Name}'.");
    }

    private static EntityPlan FindChildPlan(EntityPlan parent, INavigationBase navigation)
    {
        foreach (var (nav, child) in parent.OwnedNavigations)
        {
            if (nav == navigation || nav.Name == navigation.Name)
            {
                return child;
            }
        }

        throw new NativeTranslationNotSupportedException(
            $"No streaming plan for owned navigation '{navigation.DeclaringEntityType.DisplayName()}.{navigation.Name}'.");
    }

    /// <summary>
    /// Extract the always-present materializer block from EF's injected
    /// <c>{ bsonDocN; bsonDocN = projection as BsonDocument; bsonDocN == null ? null : &lt;block&gt; }</c>.
    /// </summary>
    private BlockExpression ExtractMaterializerBlock(Expression body, IEntityType entityType)
    {
        if (body is not BlockExpression injectedBlock)
        {
            throw new NativeTranslationNotSupportedException(
                $"Unexpected materializer shape for entity '{entityType.DisplayName()}'.");
        }

        var last = injectedBlock.Expressions[^1];
        if (last is ConditionalExpression { IfFalse: BlockExpression materializerBlock })
        {
            return materializerBlock;
        }

        if (last is BlockExpression directBlock)
        {
            return directBlock;
        }

        // Collection-element materializer block: always present (no `bsonDocN == null ? null` guard), so EF's
        // injected block is itself the materializer block — it declares a MaterializationContext and ends with
        // the instance variable rather than a conditional. Use it directly.
        if (injectedBlock.Variables.Any(v => v.Type == typeof(MaterializationContext)))
        {
            return injectedBlock;
        }

        throw new NativeTranslationNotSupportedException(
            $"Unexpected materializer shape for entity '{entityType.DisplayName()}'.");
    }

    /// <summary>
    /// Reads the value at the reader's current position via the property's serializer into <paramref name="local"/>.
    /// </summary>
    /// <remarks>
    /// An explicit BSON <c>null</c> is consumed. A non-nullable value-typed property is left at
    /// <c>default(T)</c>, matching <see cref="Storage.BsonBinding"/> (whose null check can never fire for an
    /// unconstrained value-typed <c>T</c>). A non-nullable reference-typed property throws the same
    /// <see cref="InvalidOperationException"/> as <see cref="Storage.BsonBinding"/>, since <c>default(T)</c> would be
    /// an invalid null. A missing element is handled separately by the fill loop's presence tracking.
    /// </remarks>
    private Expression BuildTypedRead(IProperty property, ParameterExpression local, bool nullableContext = false)
    {
        var serializer = BsonSerializerFactory.GetPropertySerializationInfo(property).Serializer;

        // Reuse the per-row context rather than allocating one per property. Prefer the generic
        // IBsonSerializer<TValue>.Deserialize (no boxing); otherwise use the non-generic one + Convert.
        var valueType = serializer.ValueType;
        var genericSerializerType = typeof(IBsonSerializer<>).MakeGenericType(valueType);

        Expression deserialize;
        if (genericSerializerType.IsInstanceOfType(serializer))
        {
            var typedDeserialize = genericSerializerType.GetMethod(
                nameof(IBsonSerializer.Deserialize),
                [typeof(BsonDeserializationContext), typeof(BsonDeserializationArgs)])!;

            Expression typedCall = Expression.Call(
                Expression.Constant(serializer, genericSerializerType),
                typedDeserialize,
                _context,
                Expression.Default(typeof(BsonDeserializationArgs)));

            deserialize = typedCall.ConvertIfRequired(local.Type);
        }
        else
        {
            Expression boxedCall = Expression.Call(
                Expression.Constant(serializer, typeof(IBsonSerializer)),
                DeserializeMethod,
                _context,
                Expression.Default(typeof(BsonDeserializationArgs)));

            deserialize = Expression.Convert(boxedCall, local.Type);
        }

        var readAssign = Expression.Assign(local, deserialize);

        // A leaf under a nullable complex property reads an explicit BSON null as default — the same nullability
        // EF threads into leaf materialization (see ComplexPlan.NullableContext).
        Expression onNull = !property.IsNullable && !nullableContext && !local.Type.IsValueType
            ? Expression.Throw(
                Expression.New(
                    InvalidOperationExceptionCtor,
                    Expression.Constant(Storage.BsonBinding.RequiredPropertyNullMessage(property))))
            : Expression.Assign(local, Expression.Default(local.Type));

        return Expression.IfThenElse(
            IsCurrentNull(),
            Expression.Block(
                Expression.Call(_reader, ReadNullMethod),
                onNull),
            readAssign);
    }

    /// <summary>
    /// Rewrites an EF construction block so <c>ValueBufferTryReadValue</c> reads become the property's streaming
    /// local (<c>Convert</c>-wrapped if the type differs) and the <c>MaterializationContext</c> source becomes
    /// <c>ValueBuffer.Empty</c>.
    /// </summary>
    private sealed class ConstructionRewriter : System.Linq.Expressions.ExpressionVisitor
    {
        private readonly Dictionary<IProperty, ParameterExpression> _locals;
        private readonly CollectionPlan? _collection;
        private readonly List<ComplexCollectionPlan>? _complexCollections;

        public ConstructionRewriter(
            Dictionary<IProperty, ParameterExpression> locals,
            CollectionPlan? collection = null,
            List<ComplexCollectionPlan>? complexCollections = null)
        {
            _locals = locals;
            _collection = collection;
            _complexCollections = complexCollections;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var method = node.Method;
            if (method.IsGenericMethod
                && method.GetGenericMethodDefinition() == ExpressionExtensions.ValueBufferTryReadValueMethod)
            {
                var property = node.Arguments[2].GetConstantValue<IProperty>();

                // The synthesized owned-collection ordinal key isn't stored in BSON; it's the 1-based array
                // index supplied by the loop counter (`counter + 1`, matching the DOM path's `ordinal + 1`).
                if (_collection != null
                    && property.DeclaringType == _collection.Element.EntityType
                    && property.IsOwnedTypeOrdinalKey())
                {
                    Expression ordinal = Expression.Add(_collection.Counter, Expression.Constant(1));
                    return ordinal.ConvertIfRequired(node.Type);
                }

                var local = ResolveLocal(property);
                if (local != null)
                {
                    return local.ConvertIfRequired(node.Type);
                }

                throw new NativeTranslationNotSupportedException(
                    $"Streaming materializer found a value read for property '{property.Name}' with no streaming local.");
            }

            return base.VisitMethodCall(node);
        }

        /// <summary>
        /// EF10's materializer block assigns <c>default</c> to a (root-level) complex collection member (v10.0.0
        /// <c>StructuralTypeMaterializerSource.AddInitializeExpression</c>: "Initialize collections to null, they'll
        /// be populated separately") — no value read to redirect. Replace that assignment with the
        /// presence-conditional read from the plan's <see cref="ComplexCollectionPlan.List"/> local: present (an
        /// array or an explicit BSON null) assigns the local — null stays null; missing assigns nothing, so the
        /// instance's own field initializer survives, matching driver-LINQ.
        /// </summary>
        /// <remarks>
        /// This arm is the LIVE member-assignment path for complex collections: the fill loop populates the
        /// plan's <see cref="ComplexCollectionPlan.List"/> local, and this arm is what assigns that local into
        /// the materialized instance in place of EF's <c>default</c> assignment — without it the init assignment
        /// would clobber the filled list after the fill loop. Confirmed by the final whole-branch review (the
        /// whole-entity complex-collection materialization test depends on it); the earlier fix-round probes that
        /// suggested otherwise were stale-binary-unreliable. Matching keys on <see cref="MemberInfo"/> identity,
        /// never a member name.
        /// </remarks>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Assign
                && node.Right is DefaultExpression
                && node.Left is MemberExpression { Expression: not null } memberAccess
                && _complexCollections is { } complexCollections)
            {
                foreach (var complexCollection in complexCollections)
                {
                    if (complexCollection.ComplexProperty.GetMemberInfo(forMaterialization: true, forSet: true)
                        == memberAccess.Member)
                    {
                        return Expression.IfThen(
                            complexCollection.Present,
                            Expression.Assign(
                                memberAccess,
                                complexCollection.List.ConvertIfRequired(memberAccess.Type)));
                    }
                }
            }

            return base.VisitBinary(node);
        }

        /// <summary>
        /// Resolve a read property to its streaming local. An owned-type key (e.g. a shadow FK that shares
        /// the principal's primary key, and so is stored only on the owner document, not in the owned
        /// sub-document) is redirected to its principal property's local — exactly as the DOM binding remover
        /// reads such keys from the owner via <c>FindFirstPrincipal</c>.
        /// </summary>
        private ParameterExpression? ResolveLocal(IProperty property)
        {
            if (_locals.TryGetValue(property, out var local))
            {
                return local;
            }

            var current = property;
            while (current.IsOwnedTypeKey() && current.FindFirstPrincipal() is { } principal && principal != current)
            {
                if (_locals.TryGetValue(principal, out var principalLocal))
                {
                    return principalLocal;
                }

                current = principal;
            }

            return null;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            if (node.Type == typeof(MaterializationContext))
            {
                return Expression.New(
                    node.Constructor!,
                    Expression.Constant(ValueBuffer.Empty),
                    Visit(node.Arguments[1]));
            }

            return base.VisitNew(node);
        }
    }
}
