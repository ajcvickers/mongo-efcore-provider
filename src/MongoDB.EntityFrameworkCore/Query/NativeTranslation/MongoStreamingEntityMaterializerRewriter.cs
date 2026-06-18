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
using MongoDB.EntityFrameworkCore.Serializers;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// Rewrites EF's post-injection entity-materializer block for a streaming-eligible entity so that each
/// native-path row is materialized via a single forward <see cref="IBsonReader"/> pass into typed locals —
/// instead of building a <see cref="BsonDocument"/> DOM. Handles flat (scalar / mapped-array) entities and
/// entities with single (reference) owned sub-documents, recursively (an owned type may itself own further
/// reference sub-documents). Owned <em>collections</em> and cross-collection / non-owned navigations are
/// rejected with <see cref="NativeTranslationNotSupportedException"/>.
///
/// EF's construction / tracking blocks are reused verbatim, with their <c>ValueBufferTryReadValue</c> reads
/// redirected to the typed locals and their <see cref="MaterializationContext"/> value-buffer source replaced
/// by <see cref="ValueBuffer.Empty"/>. EF's <see cref="IncludeExpression"/> structure (and its navigation
/// fixup) is preserved; only the value source and the owned null-guard inside each block are replaced.
/// </summary>
internal sealed class MongoStreamingEntityMaterializerRewriter
{
    private readonly IEntityType _rootEntityType;
    private readonly BsonSerializerFactory _bsonSerializerFactory;
    private readonly ParameterExpression _row;

    public MongoStreamingEntityMaterializerRewriter(
        IEntityType rootEntityType,
        BsonSerializerFactory bsonSerializerFactory,
        ParameterExpression row)
    {
        _rootEntityType = rootEntityType;
        _bsonSerializerFactory = bsonSerializerFactory;
        _row = row;
    }

    private static readonly MethodInfo OpenMethod =
        typeof(BsonRowReader).GetMethod(nameof(BsonRowReader.Open))!;

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

    private static readonly MethodInfo DisposeMethod =
        typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))!;

    private static readonly MethodInfo CreateRootMethod =
        typeof(BsonDeserializationContext).GetMethod(
            nameof(BsonDeserializationContext.CreateRoot),
            [typeof(IBsonReader), typeof(Action<BsonDeserializationContext.Builder>)])!;

    private static readonly MethodInfo DeserializeMethod =
        typeof(IBsonSerializer).GetMethod(
            nameof(IBsonSerializer.Deserialize),
            [typeof(BsonDeserializationContext), typeof(BsonDeserializationArgs)])!;

    private static readonly MethodInfo StringEqualsMethod =
        typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(string)])!;

    private static readonly MethodInfo IsAssignableFromMethodInfo =
        typeof(IReadOnlyEntityType).GetMethod(
            nameof(IReadOnlyEntityType.IsAssignableFrom), [typeof(IReadOnlyEntityType)])!;

    private static readonly MethodInfo IncludeReferenceMethodInfo =
        typeof(MongoStreamingEntityMaterializerRewriter).GetTypeInfo()
            .GetDeclaredMethod(nameof(IncludeReference))!;

    /// <summary>
    /// A per-entity-instance materialization plan: the typed locals for its scalar properties, an optional
    /// "present" flag local (owned sub-documents only), and the plans for its owned reference navigations.
    /// </summary>
    private sealed class EntityPlan
    {
        public required IEntityType EntityType { get; init; }
        public required Dictionary<IProperty, ParameterExpression> Locals { get; init; }
        public ParameterExpression? Present { get; init; }
        public required List<(INavigationBase Navigation, EntityPlan Child)> OwnedNavigations { get; init; }
    }

    private readonly ParameterExpression _reader = Expression.Variable(typeof(IBsonReader), "__reader");
    private readonly ParameterExpression _name = Expression.Variable(typeof(string), "__name");

    // Global property -> typed-local map across the root and all owned sub-entities. Owned-type keys
    // (shadow FKs that share the principal's PK) are resolved through this map to their principal local.
    private readonly Dictionary<IProperty, ParameterExpression> _allLocals = new();

    /// <summary>
    /// Rewrite the post-injection materializer into a forward-streaming materializer.
    /// </summary>
    public BlockExpression Rewrite(Expression injectedBody)
    {
        var resultType = injectedBody.Type;

        // Build the per-entity plans (typed locals + owned-navigation plans), recursively.
        var rootPlan = BuildPlan(_rootEntityType, present: null);

        // Rewrite the materializer tree: the IncludeExpression structure (and EF's navigation fixup) is
        // preserved; only each block's value source and the owned-block null guard are replaced.
        var rewrittenBody = RewriteMaterializer(injectedBody, rootPlan);

        // Build the forward-fill loop over the root document, descending into owned sub-documents.
        var fillLoop = BuildFillLoop(rootPlan);

        // Collect all locals (reader scratch + every entity's property/present locals).
        var allLocals = new List<ParameterExpression> { _name };
        var initializers = new List<Expression>();
        CollectLocals(rootPlan, allLocals, initializers);

        var prelude = new List<Expression>
        {
            Expression.Assign(_reader, Expression.Call(OpenMethod, _row)),
            Expression.Call(_reader, ReadStartDocumentMethod)
        };
        prelude.AddRange(initializers);
        prelude.Add(fillLoop);
        prelude.Add(Expression.Call(_reader, ReadEndDocumentMethod));
        prelude.Add(rewrittenBody);

        var tryBody = Expression.Block(resultType, allLocals, prelude);

        var withFinally = Expression.TryFinally(
            tryBody,
            Expression.IfThen(
                Expression.NotEqual(_reader, Expression.Constant(null, typeof(IBsonReader))),
                Expression.Call(_reader, DisposeMethod)));

        return Expression.Block(
            resultType,
            new[] { _reader },
            withFinally);
    }

    /// <summary>
    /// Build a plan for <paramref name="entityType"/>: one typed local per scalar property, a "present" flag
    /// (for owned sub-documents), and recursively a plan for each single owned reference navigation. Rejects
    /// owned collections and any non-owned navigation.
    /// </summary>
    private EntityPlan BuildPlan(IEntityType entityType, ParameterExpression? present)
    {
        var locals = new Dictionary<IProperty, ParameterExpression>();
        foreach (var property in entityType.GetProperties())
        {
            // Owned-type keys (shadow FKs that share the principal's primary key) live only on the owner
            // document, not the owned sub-document. They get no local of their own and no fill-loop entry;
            // reads of them are resolved to the principal's local by ConstructionRewriter.ResolveLocal.
            if (property.IsOwnedTypeKey())
            {
                continue;
            }

            var local = Expression.Variable(property.ClrType, "__p_" + entityType.ShortName() + "_" + property.Name);
            locals[property] = local;
            _allLocals[property] = local;
        }

        var ownedNavigations = new List<(INavigationBase, EntityPlan)>();
        foreach (var navigation in entityType.GetNavigations())
        {
            if (navigation.IsCollection
                || !navigation.TargetEntityType.IsOwned())
            {
                throw new NativeTranslationNotSupportedException(
                    $"Streaming materialization of navigation '{entityType.DisplayName()}.{navigation.Name}' is not supported "
                    + "(only single owned reference sub-documents are supported).");
            }

            var target = navigation.TargetEntityType;
            var childPresent = Expression.Variable(typeof(bool), "__present_" + target.ShortName());
            var child = BuildPlan(target, childPresent);
            ownedNavigations.Add((navigation, child));
        }

        return new EntityPlan
        {
            EntityType = entityType,
            Locals = locals,
            Present = present,
            OwnedNavigations = ownedNavigations
        };
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

        foreach (var (_, child) in plan.OwnedNavigations)
        {
            CollectLocals(child, locals, initializers);
        }
    }

    /// <summary>
    /// Build the forward name-dispatch fill loop for a single document level (the reader is already
    /// positioned after <c>ReadStartDocument</c>). Scalar elements fill their typed locals; an owned
    /// navigation element descends into the sub-document (recursively) under a present flag + null guard.
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

            var read = BuildTypedRead(property, local);
            ifChain = Expression.IfThenElse(
                Expression.Call(StringEqualsMethod, _name, Expression.Constant(property.GetElementName(), typeof(string))),
                read,
                ifChain);
        }

        foreach (var (navigation, child) in plan.OwnedNavigations)
        {
            var elementName = navigation.TargetEntityType.GetContainingElementName()
                              ?? throw new NativeTranslationNotSupportedException(
                                  $"Owned navigation '{navigation.DeclaringEntityType.DisplayName()}.{navigation.Name}' has no element name.");

            // If the owned element is BSON Null the sub-document is absent: ReadNull + present=false.
            // Otherwise: present=true, descend (ReadStartDocument / sub-fill-loop / ReadEndDocument).
            var descend = Expression.IfThenElse(
                Expression.Equal(
                    Expression.Call(_reader, GetCurrentBsonTypeMethod),
                    Expression.Constant(BsonType.Null, typeof(BsonType))),
                Expression.Block(
                    Expression.Call(_reader, ReadNullMethod),
                    Expression.Assign(child.Present!, Expression.Constant(false))),
                Expression.Block(
                    Expression.Assign(child.Present!, Expression.Constant(true)),
                    Expression.Call(_reader, ReadStartDocumentMethod),
                    BuildFillLoop(child),
                    Expression.Call(_reader, ReadEndDocumentMethod)));

            ifChain = Expression.IfThenElse(
                Expression.Call(StringEqualsMethod, _name, Expression.Constant(elementName, typeof(string))),
                descend,
                ifChain);
        }

        var breakTarget = Expression.Label("__fillDone_" + plan.EntityType.ShortName());
        return Expression.Loop(
            Expression.IfThenElse(
                Expression.NotEqual(
                    Expression.Call(_reader, ReadBsonTypeMethod),
                    Expression.Constant(BsonType.EndOfDocument, typeof(BsonType))),
                Expression.Block(
                    Expression.Assign(_name, Expression.Call(ReadNameMethod, _reader)),
                    ifChain),
                Expression.Break(breakTarget)),
            breakTarget);
    }

    /// <summary>
    /// Rewrite the materializer expression, preserving any <see cref="IncludeExpression"/> structure.
    /// For a plain entity block the value source is redirected to <paramref name="plan"/>'s locals; for an
    /// <see cref="IncludeExpression"/> the entity block is rewritten with the parent plan and each owned
    /// navigation block is rewritten with the matching child plan, its <c>bsonDocN == null</c> guard
    /// replaced by <c>!present</c>.
    /// </summary>
    private Expression RewriteMaterializer(Expression body, EntityPlan plan)
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
                throw new NativeTranslationNotSupportedException(
                    $"Streaming materialization of collection navigation '{navigation.DeclaringEntityType.DisplayName()}.{navigation.Name}' is not supported.");
            }

            var entityBlock = (BlockExpression)RewriteMaterializer(include.EntityExpression, plan);

            var child = FindChildPlan(plan, navigation);
            var navExpression = RewriteOwnedNavigation(include.NavigationExpression, child);

            return SpliceReferenceInclude(entityBlock, navigation, navExpression, include.SetLoaded);
        }

        // Plain entity block: { bsonDocN; bsonDocN = projection as BsonDocument; bsonDocN == null ? null : <block> }
        // The root row is always present, so drop the bsonDocN local + null guard and use the materializer
        // block directly, redirecting its value source to this plan's locals.
        var materializerBlock = ExtractMaterializerBlock(body, plan.EntityType);
        return new ConstructionRewriter(_allLocals).Visit(materializerBlock);
    }

    /// <summary>
    /// Splice an owned reference-navigation fixup into a rewritten entity materializer block, mirroring EF's
    /// <c>IncludeReference</c> path. The block's trailing instance expression is preserved; the fixup call is
    /// inserted just before it, using the block's own <c>entry</c> / <c>entityType</c> / <c>instance</c> locals.
    /// </summary>
    private BlockExpression SpliceReferenceInclude(
        BlockExpression entityBlock,
        INavigation navigation,
        Expression navigationExpression,
        bool setLoaded)
    {
        var includingClrType = navigation.DeclaringEntityType.ClrType;
        var relatedEntityClrType = navigation.TargetEntityType.ClrType;

        var instanceVariable = entityBlock.Variables.Single(v => v.Type == includingClrType);
        var concreteEntityTypeVariable = entityBlock.Variables.Single(v => v.Type == typeof(IEntityType));
#pragma warning disable EF1001 // Internal EF Core API usage.
        var entryVariable = entityBlock.Variables.SingleOrDefault(v => v.Type == typeof(InternalEntityEntry));
        Expression entityEntryExpression =
            entryVariable ?? (Expression)Expression.Constant(null, typeof(InternalEntityEntry));
#pragma warning restore EF1001 // Internal EF Core API usage.

        var inverseNavigation = navigation.Inverse;
        var fixup = GenerateReferenceFixup(includingClrType, relatedEntityClrType, navigation, inverseNavigation);

        var includeCall = Expression.IfThen(
            Expression.Call(
                Expression.Constant(navigation.DeclaringEntityType, typeof(IReadOnlyEntityType)),
                IsAssignableFromMethodInfo,
                Expression.Convert(concreteEntityTypeVariable, typeof(IReadOnlyEntityType))),
            Expression.Call(
                IncludeReferenceMethodInfo.MakeGenericMethod(includingClrType, relatedEntityClrType),
                entityEntryExpression,
                instanceVariable,
                concreteEntityTypeVariable,
                navigationExpression,
                Expression.Constant(navigation),
                Expression.Constant(inverseNavigation, typeof(INavigation)),
                Expression.Constant(fixup),
                Expression.Constant(setLoaded)));

        // Insert the include call just before the block's trailing instance expression.
        var expressions = new List<Expression>(entityBlock.Expressions);
        var trailing = expressions[^1];
        expressions[^1] = includeCall;
        expressions.Add(trailing);

        return entityBlock.Update(entityBlock.Variables, expressions);
    }

    /// <summary>
    /// Rewrite an owned reference navigation's expression. The owned-entity block has the shape
    /// <c>{ bsonDocN; bsonDocN = projection as BsonDocument; return bsonDocN == null ? null : &lt;block&gt;; }</c>;
    /// when the owned type itself owns further references the expression is an <see cref="IncludeExpression"/>
    /// wrapping that block. The materializer block's value source is redirected to the child plan's locals,
    /// any nested owned-reference fixup is spliced in, and the <c>bsonDocN == null</c> presence test is
    /// replaced with <c>!present</c> so an absent owned sub-document yields the null navigation.
    /// </summary>
    private Expression RewriteOwnedNavigation(Expression navExpression, EntityPlan child)
    {
        // Locate the owned-entity block carrying the `bsonDocN == null ? null : <block>` guard. When the
        // owned type has its own owned references the navigation is an IncludeExpression whose EntityExpression
        // is that block; otherwise the navigation expression is the block directly.
        var entityExpression = navExpression is IncludeExpression nestedInclude
            ? nestedInclude.EntityExpression
            : navExpression;

        if (entityExpression is not BlockExpression block
            || block.Expressions[^1] is not ConditionalExpression { IfFalse: BlockExpression materializerBlock } conditional)
        {
            throw new NativeTranslationNotSupportedException(
                $"Unexpected owned-navigation materializer shape for '{child.EntityType.DisplayName()}'.");
        }

        // Rewrite the owned materializer block's value source to the child's locals.
        var rewrittenBlock = (BlockExpression)new ConstructionRewriter(_allLocals).Visit(materializerBlock);

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
            var nestedNavExpression = RewriteOwnedNavigation(include.NavigationExpression, nestedChild);
            rewrittenBlock = SpliceReferenceInclude(rewrittenBlock, nestedNavigation, nestedNavExpression, include.SetLoaded);
        }

        // Replace the whole `{ bsonDocN; bsonDocN = ... as BsonDocument; bsonDocN == null ? null : <block> }`
        // with `!present ? null : <rewrittenBlock>`. The outer block's bsonDocN local + assignment are
        // dropped entirely: their RHS is an unreduced EntityProjectionExpression (bsonDoc["Address"]) that has
        // no streaming equivalent — presence is tracked by the `present` flag instead.
        return Expression.Condition(
            Expression.Not(child.Present!),
            Expression.Constant(null, conditional.Type),
            Expression.Convert(rewrittenBlock, conditional.Type));
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

        throw new NativeTranslationNotSupportedException(
            $"Unexpected materializer shape for entity '{entityType.DisplayName()}'.");
    }

    /// <summary>
    /// Build a typed read for <paramref name="property"/>: deserialize the value at the reader's current
    /// position via the property's serializer and assign it to <paramref name="local"/>. Nullable CLR
    /// properties first check for an explicit BSON null.
    /// </summary>
    private Expression BuildTypedRead(IProperty property, ParameterExpression local)
    {
        var serializer = BsonSerializerFactory.GetPropertySerializationInfo(property).Serializer;

        var context = Expression.Call(
            CreateRootMethod,
            _reader,
            Expression.Constant(null, typeof(Action<BsonDeserializationContext.Builder>)));

        Expression deserialize = Expression.Call(
            Expression.Constant(serializer, typeof(IBsonSerializer)),
            DeserializeMethod,
            context,
            Expression.Default(typeof(BsonDeserializationArgs)));

        var readAssign = Expression.Assign(local, Expression.Convert(deserialize, local.Type));

        if (local.Type.IsNullableType())
        {
            return Expression.IfThenElse(
                Expression.Equal(
                    Expression.Call(_reader, GetCurrentBsonTypeMethod),
                    Expression.Constant(BsonType.Null, typeof(BsonType))),
                Expression.Block(
                    Expression.Call(_reader, ReadNullMethod),
                    Expression.Assign(local, Expression.Default(local.Type))),
                readAssign);
        }

        return readAssign;
    }

    /// <summary>
    /// Reference-include fixup, mirroring EF's binding remover <c>IncludeReference</c>: wires the materialized
    /// related entity onto the principal via <paramref name="fixup"/> (and sets the navigation loaded flag).
    /// </summary>
    private static void IncludeReference<TIncludingEntity, TIncludedEntity>(
        InternalEntityEntry? entry,
        object? entity,
        IEntityType entityType,
        TIncludedEntity relatedEntity,
        INavigation navigation,
        INavigation? inverseNavigation,
        Action<TIncludingEntity, TIncludedEntity> fixup,
        bool _)
    {
        if (entity == null
            || !navigation.DeclaringEntityType.IsAssignableFrom(entityType))
        {
            return;
        }

        if (entry == null)
        {
            var includingEntity = (TIncludingEntity)entity;
            navigation.SetIsLoadedWhenNoTracking(includingEntity);
            if (relatedEntity != null)
            {
                fixup(includingEntity, relatedEntity);
                if (inverseNavigation != null
                    && !inverseNavigation.IsCollection)
                {
                    inverseNavigation.SetIsLoadedWhenNoTracking(relatedEntity);
                }
            }
        }
        // For non-null relatedEntity the StateManager sets the flag.
        else if (relatedEntity == null)
        {
            entry.SetIsLoaded(navigation);
        }
    }

    private static Delegate GenerateReferenceFixup(
        Type entityType,
        Type relatedEntityType,
        INavigation navigation,
        INavigation? inverseNavigation)
    {
        var entityParameter = Expression.Parameter(entityType);
        var relatedEntityParameter = Expression.Parameter(relatedEntityType);
        var expressions = new List<Expression>
        {
            AssignReferenceNavigation(entityParameter, relatedEntityParameter, navigation)
        };

        if (inverseNavigation != null)
        {
            // A single owned reference can only have a single inverse (back to the principal); collection
            // inverses do not occur for the owned-reference shapes this rewriter accepts.
            expressions.Add(
                AssignReferenceNavigation(relatedEntityParameter, entityParameter, inverseNavigation));
        }

        return Expression.Lambda(
                Expression.Block(typeof(void), expressions), entityParameter, relatedEntityParameter)
            .Compile();
    }

    private static Expression AssignReferenceNavigation(
        ParameterExpression entity,
        ParameterExpression relatedEntity,
        INavigation navigation)
        => entity.MakeMemberAccess(navigation.GetMemberInfo(forMaterialization: true, forSet: true))
            .Assign(relatedEntity);

    /// <summary>
    /// Rewrites an EF construction/tracking block to consume the streaming locals instead of a ValueBuffer:
    /// <list type="bullet">
    /// <item><c>ValueBufferTryReadValue&lt;TClr&gt;(mc.ValueBuffer, i, property)</c> → the property's local
    /// (wrapped in <c>Convert(local, node.Type)</c> where the requested type differs).</item>
    /// <item><c>new MaterializationContext(&lt;source&gt;, ctx)</c> → <c>new MaterializationContext(ValueBuffer.Empty, ctx)</c>.</item>
    /// </list>
    /// </summary>
    private sealed class ConstructionRewriter : System.Linq.Expressions.ExpressionVisitor
    {
        private readonly Dictionary<IProperty, ParameterExpression> _locals;

        public ConstructionRewriter(Dictionary<IProperty, ParameterExpression> locals)
            => _locals = locals;

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var method = node.Method;
            if (method.IsGenericMethod
                && method.GetGenericMethodDefinition() == ExpressionExtensions.ValueBufferTryReadValueMethod)
            {
                var property = node.Arguments[2].GetConstantValue<IProperty>();
                var local = ResolveLocal(property);
                if (local != null)
                {
                    if (node.Type == local.Type)
                    {
                        return local;
                    }

                    return Expression.Convert(local, node.Type);
                }

                throw new NativeTranslationNotSupportedException(
                    $"Streaming materializer found a value read for property '{property.Name}' with no streaming local.");
            }

            return base.VisitMethodCall(node);
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
