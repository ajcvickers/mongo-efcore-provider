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
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.EntityFrameworkCore.Serializers;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// Rewrites EF's post-injection entity-materializer <see cref="BlockExpression"/> for a flat
/// (scalar / mapped-array, no owned navigations) streaming-eligible entity so that each native-path
/// row is materialized via a single forward <see cref="IBsonReader"/> pass into typed locals — instead
/// of building a <see cref="BsonDocument"/> DOM. EF's construction / tracking block is reused verbatim,
/// with its <c>ValueBufferTryReadValue</c> reads redirected to the typed locals and its
/// <see cref="MaterializationContext"/> value-buffer source replaced by <see cref="ValueBuffer.Empty"/>.
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

    /// <summary>
    /// Rewrite the post-injection materializer block into a forward-streaming materializer.
    /// </summary>
    public BlockExpression Rewrite(Expression injectedBody)
    {
        // Reject entities with owned/Include navigations up front. For these the injected body is not a
        // flat materializer block at all (it is an IncludeExpression / nested shaper), so this guard also
        // protects the BlockExpression assumption below.
        if (_rootEntityType.GetNavigations().GetEnumerator().MoveNext())
        {
            throw new NativeTranslationNotSupportedException(
                $"Streaming materialization of entity '{_rootEntityType.DisplayName()}' with owned navigations is not yet supported.");
        }

        if (injectedBody is not BlockExpression injectedBlock)
        {
            throw new NativeTranslationNotSupportedException(
                $"Unexpected materializer shape for entity '{_rootEntityType.DisplayName()}'.");
        }

        // injectedBody (flat tracked entity) looks like:
        //   Block([bsonDoc1],
        //     Assign(bsonDoc1, TypeAs(ProjectionBinding, BsonDocument)),
        //     Condition(bsonDoc1 == null, null, <materializerBlock>))
        // For a top-level collection scan the row is always present, so the materializer block is
        // always taken; we drop the bsonDoc local, its assignment, and the null guard.
        var materializerBlock = ExtractMaterializerBlock(injectedBlock);

        // Build the typed locals (one per scalar IProperty) and the forward-fill loop.
        var reader = Expression.Variable(typeof(IBsonReader), "__reader");
        var name = Expression.Variable(typeof(string), "__name");

        var propertyLocals = new Dictionary<IProperty, ParameterExpression>();
        var ifChain = (Expression)Expression.Call(reader, SkipValueMethod);

        foreach (var property in _rootEntityType.GetProperties())
        {
            var local = Expression.Variable(property.ClrType, "__p_" + property.Name);
            propertyLocals[property] = local;

            var read = BuildTypedRead(reader, property, local);
            ifChain = Expression.IfThenElse(
                Expression.Call(StringEqualsMethod, name, Expression.Constant(property.GetElementName(), typeof(string))),
                read,
                ifChain);
        }

        var breakTarget = Expression.Label("__fillDone");
        var fillLoop = Expression.Loop(
            Expression.IfThenElse(
                Expression.NotEqual(
                    Expression.Call(reader, ReadBsonTypeMethod),
                    Expression.Constant(BsonType.EndOfDocument, typeof(BsonType))),
                Expression.Block(
                    Expression.Assign(name, Expression.Call(ReadNameMethod, reader)),
                    ifChain),
                Expression.Break(breakTarget)),
            breakTarget);

        // Rewrite EF's construction/tracking block: redirect ValueBufferTryReadValue reads to the
        // typed locals and replace the MaterializationContext value-buffer source with ValueBuffer.Empty.
        var rewrittenConstruction =
            (BlockExpression)new ConstructionRewriter(propertyLocals).Visit(materializerBlock);

        // Assemble: open reader (try) -> declare locals -> fill loop -> construction; dispose in finally.
        var prelude = new List<Expression>
        {
            Expression.Assign(reader, Expression.Call(OpenMethod, _row)),
            Expression.Call(reader, ReadStartDocumentMethod)
        };
        // Initialize each property local to default(TClr) so unmatched elements leave it well-defined.
        var allLocals = new List<ParameterExpression> { name };
        foreach (var local in propertyLocals.Values)
        {
            allLocals.Add(local);
            prelude.Add(Expression.Assign(local, Expression.Default(local.Type)));
        }

        prelude.Add(fillLoop);
        prelude.Add(Expression.Call(reader, ReadEndDocumentMethod));

        // Merge the construction block's own variables into the inner block, returning the instance.
        var innerBody = new List<Expression>(prelude);
        innerBody.AddRange(rewrittenConstruction.Expressions);
        allLocals.AddRange(rewrittenConstruction.Variables);

        var tryBody = Expression.Block(materializerBlock.Type, allLocals, innerBody);

        var withFinally = Expression.TryFinally(
            tryBody,
            Expression.IfThen(
                Expression.NotEqual(reader, Expression.Constant(null, typeof(IBsonReader))),
                Expression.Call(reader, DisposeMethod)));

        return Expression.Block(
            materializerBlock.Type,
            new[] { reader },
            withFinally);
    }

    /// <summary>
    /// Extract the always-present materializer block from EF's injected
    /// <c>{ bsonDocN; bsonDocN = projection as BsonDocument; bsonDocN == null ? null : &lt;block&gt; }</c>.
    /// Throws when the entity has owned navigations (the block then nests Include / collection shapers,
    /// which this scalar-only rewriter does not yet support).
    /// </summary>
    private BlockExpression ExtractMaterializerBlock(BlockExpression injectedBody)
    {
        // The last expression of the injected block is the conditional guard; its IfFalse is the
        // materializer block we want.
        var last = injectedBody.Expressions[^1];
        if (last is ConditionalExpression { IfFalse: BlockExpression materializerBlock })
        {
            ThrowIfNotScalar(materializerBlock);
            return materializerBlock;
        }

        if (last is BlockExpression directBlock)
        {
            ThrowIfNotScalar(directBlock);
            return directBlock;
        }

        throw new NativeTranslationNotSupportedException(
            $"Unexpected materializer shape for entity '{_rootEntityType.DisplayName()}'.");
    }

    private void ThrowIfNotScalar(Expression block)
    {
        if (new IncludeDetector().Found(block))
        {
            throw new NativeTranslationNotSupportedException(
                $"Streaming materialization of entity '{_rootEntityType.DisplayName()}' with Include / owned-navigation shapers is not yet supported.");
        }
    }

    /// <summary>
    /// Build a typed read for <paramref name="property"/>: deserialize the value at the reader's current
    /// position via the property's serializer and assign it to <paramref name="local"/>. Nullable CLR
    /// properties first check for an explicit BSON null.
    /// </summary>
    private Expression BuildTypedRead(ParameterExpression reader, IProperty property, ParameterExpression local)
    {
        var serializer = BsonSerializerFactory.GetPropertySerializationInfo(property).Serializer;

        // (TClr)serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader, null), default)
        var context = Expression.Call(
            CreateRootMethod,
            reader,
            Expression.Constant(null, typeof(Action<BsonDeserializationContext.Builder>)));

        Expression deserialize = Expression.Call(
            Expression.Constant(serializer, typeof(IBsonSerializer)),
            DeserializeMethod,
            context,
            Expression.Default(typeof(BsonDeserializationArgs)));

        var readAssign = Expression.Assign(local, Expression.Convert(deserialize, local.Type));

        if (local.Type.IsNullableType())
        {
            // GetCurrentBsonType() == Null ? { ReadNull(); local = default; } : <read>
            return Expression.IfThenElse(
                Expression.Equal(
                    Expression.Call(reader, GetCurrentBsonTypeMethod),
                    Expression.Constant(BsonType.Null, typeof(BsonType))),
                Expression.Block(
                    Expression.Call(reader, ReadNullMethod),
                    Expression.Assign(local, Expression.Default(local.Type))),
                readAssign);
        }

        return readAssign;
    }

    /// <summary>
    /// Rewrites EF's construction/tracking block to consume the streaming locals instead of a ValueBuffer:
    /// <list type="bullet">
    /// <item><c>ValueBufferTryReadValue&lt;TClr&gt;(mc.ValueBuffer, i, property)</c> → the property's local
    /// (wrapped in <c>Convert(local, typeof(object))</c> at object-typed call sites).</item>
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
                if (_locals.TryGetValue(property, out var local))
                {
                    // node.Type is the requested TClr at this call site (e.g. <object> for TryGetEntry key,
                    // the property CLR type for the field assignment). Convert from the local's CLR type
                    // to the requested type where they differ.
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

    /// <summary>
    /// Detects EF <see cref="IncludeExpression"/> nodes (owned-navigation / Include shapers). Collection
    /// and reference shaper nodes are reached only for entities with navigations, which the caller already
    /// rejects before reaching here; this is the belt-and-braces guard for any residual nested-entity shape.
    /// </summary>
    private sealed class IncludeDetector : System.Linq.Expressions.ExpressionVisitor
    {
        private bool _found;

        public bool Found(Expression expression)
        {
            Visit(expression);
            return _found;
        }

        protected override Expression VisitExtension(Expression node)
        {
            if (node is IncludeExpression)
            {
                _found = true;
                return node;
            }

            return base.VisitExtension(node);
        }
    }
}
