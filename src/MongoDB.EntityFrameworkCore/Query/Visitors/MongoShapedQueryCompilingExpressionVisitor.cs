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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;
using MongoDB.EntityFrameworkCore.Query.Visitors.Dependencies;
using MongoDB.EntityFrameworkCore.Serializers;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.Query.Visitors;

/// <inheritdoc/>
internal sealed class MongoShapedQueryCompilingExpressionVisitor : ShapedQueryCompilingExpressionVisitor
{
    private readonly Type _contextType;
    private readonly bool _threadSafetyChecksEnabled;
    private readonly BsonSerializerFactory _bsonSerializerFactory;

    /// <summary>
    /// Create a <see cref="MongoShapedQueryCompilingExpressionVisitor"/> with the required dependencies and compilation context.
    /// </summary>
    /// <param name="dependencies">The <see cref="ShapedQueryCompilingExpressionVisitorDependencies"/> used by this visitor.</param>
    /// <param name="mongoDependencies">MongoDB-specific dependencies used by this visitor.</param>
    /// <param name="queryCompilationContext">The <see cref="QueryCompilationContext"/> for this specific query.</param>
    public MongoShapedQueryCompilingExpressionVisitor(
        ShapedQueryCompilingExpressionVisitorDependencies dependencies,
        MongoShapedQueryCompilingExpressionVisitorDependencies mongoDependencies,
        QueryCompilationContext queryCompilationContext)
        : base(dependencies, queryCompilationContext)
    {
        _contextType = queryCompilationContext.ContextType;
        _threadSafetyChecksEnabled = dependencies.CoreSingletonOptions.AreThreadSafetyChecksEnabled;
        _bsonSerializerFactory = mongoDependencies.BsonSerializerFactory;
    }

    /// <inheritdoc/>
    protected override Expression VisitShapedQuery(ShapedQueryExpression shapedQueryExpression)
    {
        if (shapedQueryExpression.QueryExpression is not MongoQueryExpression mongoQueryExpression)
        {
            throw new NotSupportedException($" Unhandled expression node type '{nameof(shapedQueryExpression.QueryExpression)}'");
        }

        var rootEntityType = mongoQueryExpression.CollectionExpression.EntityType;
        var projectedEntityType = QueryCompilationContext.Model.FindEntityType(
            shapedQueryExpression.ResultCardinality == ResultCardinality.Enumerable
                ? shapedQueryExpression.Type.TryGetItemType()!
                : shapedQueryExpression.Type);

        if (projectedEntityType == null)
        {
            return VisitProjectedQuery(shapedQueryExpression, rootEntityType, mongoQueryExpression);
        }

        // Entity path: full BsonDocuments shaped into tracked/untracked entity instances
        return CompileShapedQuery(shapedQueryExpression, mongoQueryExpression, rootEntityType,
            (bsonDoc, behavior) => new MongoProjectionBindingRemovingExpressionVisitor(
                rootEntityType, mongoQueryExpression, bsonDoc, behavior));
    }

    private MethodCallExpression VisitProjectedQuery(
        ShapedQueryExpression shapedQueryExpression,
        IEntityType rootEntityType,
        MongoQueryExpression mongoQueryExpression)
    {
        VerifyNoClientConstant(shapedQueryExpression.ShaperExpression);

        if (ProjectionAnalyzer.CanPushDown(shapedQueryExpression.ShaperExpression))
        {
            // Push-down path: scalar/anonymous projections handled entirely by LINQ V3
            return Expression.Call(null,
                ExecuteProjectedQueryMethodInfo.MakeGenericMethod(rootEntityType.ClrType,
                    shapedQueryExpression.ShaperExpression.Type),
                QueryCompilationContext.QueryContextParameter,
                Expression.Constant(rootEntityType),
                Expression.Constant(_bsonSerializerFactory),
                Expression.Constant(mongoQueryExpression),
                Expression.Constant(_contextType),
                Expression.Constant(_threadSafetyChecksEnabled),
                Expression.Constant(shapedQueryExpression.ResultCardinality));
        }

        // Mixed path: projection contains entity references that LINQ V3 can't handle.
        // Strip the Select so the driver returns full BsonDocuments keyed by EF-configured
        // element names; the client-side shaper handles the projection. The Select may sit
        // directly on the captured expression, or under a no-arg cardinality terminator
        // (Single/First/etc.) which we also need to rebind to the un-projected source type.
        mongoQueryExpression.CapturedExpression = StripPushedDownSelect(mongoQueryExpression.CapturedExpression);

        return CompileShapedQuery(shapedQueryExpression, mongoQueryExpression, rootEntityType,
            (bsonDoc, behavior) => new MongoMixedProjectionBindingRemovingExpressionVisitor(
                rootEntityType, mongoQueryExpression, bsonDoc, behavior));
    }

    /// <summary>
    /// Remove the projection <c>Select</c> from the captured query chain so the shaper runs client-side
    /// over full <see cref="BsonDocument"/>s. The Select may be the outermost node, or wrapped by a single
    /// no-arg cardinality terminator (e.g. <c>First</c>, <c>Single</c>) emitted by EF Core for cardinality
    /// reducers such as <c>AssertFirst</c>. The terminal operator is preserved with its generic argument
    /// retargeted to the Select's source element type.
    /// </summary>
    private static Expression? StripPushedDownSelect(Expression? captured)
    {
        if (captured is not MethodCallExpression call || call.Method.DeclaringType != typeof(Queryable))
        {
            return captured;
        }

        if (call.Method.Name == nameof(Queryable.Select) && call.Arguments.Count == 2)
        {
            return call.Arguments[0];
        }

        if (call.Method.IsGenericMethod
            && call.Method.GetParameters().Length == 1
            && call.Method.Name is nameof(Queryable.Single) or nameof(Queryable.SingleOrDefault)
                or nameof(Queryable.First) or nameof(Queryable.FirstOrDefault)
                or nameof(Queryable.Last) or nameof(Queryable.LastOrDefault)
            && call.Arguments is [MethodCallExpression { Method: { Name: nameof(Queryable.Select), DeclaringType: var st } } innerSelect]
            && st == typeof(Queryable))
        {
            var newSource = innerSelect.Arguments[0];
            var newSourceType = newSource.Type.GetGenericArguments()[0];
            var rebound = call.Method.GetGenericMethodDefinition().MakeGenericMethod(newSourceType);
            return Expression.Call(rebound, newSource);
        }

        return captured;
    }

    private MethodCallExpression CompileShapedQuery(
        ShapedQueryExpression shapedQueryExpression,
        MongoQueryExpression mongoQueryExpression,
        IEntityType rootEntityType,
        Func<ParameterExpression, QueryTrackingBehavior, System.Linq.Expressions.ExpressionVisitor> createBindingRemover)
    {
        var bsonDocParameter = Expression.Parameter(typeof(BsonDocument), "bsonDoc");
        var trackingBehavior = QueryCompilationContext.QueryTrackingBehavior;
        var nativeMode = NativeQuery.EffectiveMode(((MongoQueryCompilationContext)QueryCompilationContext).UseNativeQuery);

        var shaperBody = shapedQueryExpression.ShaperExpression;
        var bsonInjector = new BsonDocumentInjectingExpressionVisitor();
        shaperBody = bsonInjector.Visit(shaperBody);
#if EF8 || EF9
        var injectedBody = InjectEntityMaterializers(shaperBody);
#else
        var injectedBody = InjectStructuralTypeMaterializers(shaperBody);
#endif

        // Forward-only streaming materialization: for streaming-eligible entities, rewrite the
        // post-injection materializer to read each native-path RawBsonDocument row via a single
        // forward IBsonReader pass into typed locals instead of building a BsonDocument DOM. On any
        // unsupported shape the rewriter throws; in non-Force mode we fall back to the DOM path.
        //
        // Streaming requires the native pipeline to actually be built at run time, and that gate is narrower
        // than entity eligibility: it needs an enumerable result cardinality and no pending lookups. A
        // cardinality reducer (First/Single/...) or a query with pending lookups falls through to the
        // driver-LINQ path, which never produces streaming rows. Those are excluded up front.
        //
        // Even within that gate, native-pipeline translation can still fail at run time for a reason we
        // cannot see here (e.g. a Where predicate the native translator rejects — dictionary key access,
        // list Contains, Mql.IsMissing). In that case TranslateQuery falls back to the driver-LINQ path,
        // which yields BsonDocuments rather than the RawBsonDocuments the streaming shaper expects. To stay
        // correct we compile BOTH shapers when streaming is selected and let ExecuteStreamingShapedQuery
        // dispatch at run time on executableQuery.Streaming: the streaming shaper for the native streaming
        // pipeline, the DOM shaper for the driver-LINQ fallback.
        var streaming = nativeMode != NativeQueryMode.Off
            && shapedQueryExpression.ResultCardinality == ResultCardinality.Enumerable
            && mongoQueryExpression.GetPendingLookups().Count == 0
            && StreamingEligibility.IsEligible(rootEntityType);

        Delegate? compiledStreamingShaper = null;

        if (streaming)
        {
            var rawRowParameter = Expression.Parameter(typeof(RawBsonDocument), "rawRow");
            try
            {
                var streamingBody = new MongoStreamingEntityMaterializerRewriter(
                        rootEntityType, _bsonSerializerFactory, rawRowParameter)
                    .Rewrite(injectedBody);
                compiledStreamingShaper = Expression.Lambda(
                        streamingBody,
                        QueryCompilationContext.QueryContextParameter,
                        rawRowParameter)
                    .Compile();
            }
            catch (NativeTranslationNotSupportedException) when (nativeMode != NativeQueryMode.Force)
            {
                // The entity shape itself isn't streamable; fall back to the DOM path entirely.
                streaming = false;
            }
        }

        // The DOM (BsonDocument) shaper is always compiled: it is the sole shaper for the non-streaming
        // path, and the run-time fallback shaper for the streaming path when native translation can't build
        // a pipeline.
        var domShaperBody = createBindingRemover(bsonDocParameter, trackingBehavior).Visit(injectedBody);

        // Lift all BsonDocument/BsonArray variables to the lambda level so they are accessible across entity
        // boundaries in join projections. (The streaming shaper materializes entirely from typed locals
        // declared by the rewriter and does not use these variables.)
        if (bsonInjector.AllVariables.Count > 0)
        {
            domShaperBody = Expression.Block(
                domShaperBody.Type,
                bsonInjector.AllVariables,
                domShaperBody);
        }

        var domShaperLambda = Expression.Lambda(
            domShaperBody,
            QueryCompilationContext.QueryContextParameter,
            bsonDocParameter);
        var compiledDomShaper = domShaperLambda.Compile();

        var projectedType = domShaperLambda.ReturnType;
        var standAloneStateManager = QueryCompilationContext.QueryTrackingBehavior ==
                                     QueryTrackingBehavior.NoTrackingWithIdentityResolution;

        if (streaming)
        {
            return Expression.Call(null,
                ExecuteStreamingShapedQueryMethodInfo.MakeGenericMethod(rootEntityType.ClrType, projectedType),
                QueryCompilationContext.QueryContextParameter,
                Expression.Constant(rootEntityType),
                Expression.Constant(_bsonSerializerFactory),
                Expression.Constant(mongoQueryExpression),
                Expression.Constant(compiledStreamingShaper!),
                Expression.Constant(compiledDomShaper),
                Expression.Constant(_contextType),
                Expression.Constant(standAloneStateManager),
                Expression.Constant(_threadSafetyChecksEnabled),
                Expression.Constant(shapedQueryExpression.ResultCardinality),
                Expression.Constant(nativeMode));
        }

        var compiledShaper = compiledDomShaper;

        return Expression.Call(null,
            ExecuteShapedQueryMethodInfo.MakeGenericMethod(rootEntityType.ClrType, projectedType),
            QueryCompilationContext.QueryContextParameter,
            Expression.Constant(rootEntityType),
            Expression.Constant(_bsonSerializerFactory),
            Expression.Constant(mongoQueryExpression),
            Expression.Constant(compiledShaper),
            Expression.Constant(_contextType),
            Expression.Constant(standAloneStateManager),
            Expression.Constant(_threadSafetyChecksEnabled),
            Expression.Constant(shapedQueryExpression.ResultCardinality),
            Expression.Constant(nativeMode));
    }

    private static (MongoQueryContext, MongoExecutableQuery) TranslateQuery<TSource>(
        QueryContext queryContext,
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoQueryExpression queryExpression,
        ResultCardinality resultCardinality,
        bool nativeEligible,
        bool streaming,
        NativeQueryMode nativeMode,
        Func<MongoEFToLinqTranslatingExpressionVisitor, Expression?, Expression> translate)
    {
        var mongoQueryContext = (MongoQueryContext)queryContext;
        var collection = mongoQueryContext.MongoClient.GetCollection<TSource>(queryExpression.CollectionExpression.CollectionName);

        var transaction = mongoQueryContext.Context.Database.CurrentTransaction as MongoTransaction;
        var queryable = transaction == null ? collection.AsQueryable() : collection.AsQueryable(transaction.Session);
        var source = queryable.As((IBsonSerializer<TSource>)bsonSerializerFactory.GetEntitySerializer(entityType));

        var innerSources = new Dictionary<IEntityType, Expression>();
        if (queryExpression.IsJoinQuery)
        {
            foreach (var (innerEntityType, innerCollectionExpression) in queryExpression.InnerCollections)
            {
                innerSources[innerEntityType] = CreateInnerSource(
                    mongoQueryContext, bsonSerializerFactory, innerEntityType, innerCollectionExpression.CollectionName, transaction);
            }
        }

        // The native MQL path returns full BsonDocuments and relies on the client-side shaper used by the
        // entity / mixed-projection path (ExecuteShapedQuery). The push-down path (ExecuteProjectedQuery)
        // uses an identity shaper expecting the driver to have projected scalars/anonymous types server-side;
        // handing it full BsonDocuments would fail to materialize. So native translation is only attempted on
        // the client-side-shaper path. Pushed-down projections always use the driver-LINQ path.
        if (nativeEligible
            && nativeMode != NativeQueryMode.Off
            && resultCardinality == ResultCardinality.Enumerable
            && queryExpression.GetPendingLookups().Count == 0)
        {
            try
            {
                var nativePipeline = new MongoPipelineTranslator((IEntityType)entityType, queryContext)
                    .Translate(queryExpression.CapturedExpression);

                var nativeExecutable = new MongoExecutableQuery(
                    Expression.Empty(),
                    resultCardinality,
                    (IMongoQueryProvider)source.Provider,
                    collection.CollectionNamespace,
                    new(new Dictionary<string, object>()))
                {
                    NativePipeline = nativePipeline,
                    Session = transaction?.Session,
                    Streaming = streaming
                };
                return (mongoQueryContext, nativeExecutable);
            }
            catch (NativeTranslationNotSupportedException) when (nativeMode != NativeQueryMode.Force)
            {
                // fall through to the driver-LINQ path
            }
        }

        var queryTranslator = new MongoEFToLinqTranslatingExpressionVisitor(
            queryContext, source.Expression, bsonSerializerFactory, queryExpression.GetPendingLookups(), innerSources);
        var translatedQuery = translate(queryTranslator, queryExpression.CapturedExpression);

        var executableQuery = new MongoExecutableQuery(
            translatedQuery,
            resultCardinality,
            (IMongoQueryProvider)source.Provider,
            collection.CollectionNamespace,
            new(queryTranslator.AdditionalState));

        return (mongoQueryContext, executableQuery);
    }

    private static Action<MongoQueryContext, MongoExecutableQuery>? GetOnZeroResultsAction(MongoQueryExpression queryExpression)
    {
        if (queryExpression.CapturedExpression is MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.Name == "Select" && methodCallExpression.Arguments is [MethodCallExpression mce, _])
            {
                methodCallExpression = mce;
            }

            if (methodCallExpression.IsVectorSearch())
            {
                return (qc, eq) => qc.QueryLogger.VectorSearchReturnedZeroResults(
                    (IProperty)eq.AdditionalState[MongoExecutableQuery.VectorQueryProperty],
                    (string)eq.AdditionalState[MongoExecutableQuery.VectorQueryIndexName]);
            }
        }

        return null;
    }

    private static QueryingEnumerable<TResult, TResult> ExecuteProjectedQuery<TSource, TResult>(
        QueryContext queryContext,
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoQueryExpression queryExpression,
        Type contextType,
        bool threadSafetyChecksEnabled,
        ResultCardinality resultCardinality)
    {
        var (mongoQueryContext, executableQuery) = TranslateQuery<TSource>(
            queryContext, entityType, bsonSerializerFactory, queryExpression, resultCardinality,
            nativeEligible: false,
            streaming: false,
            nativeMode: NativeQueryMode.Off,
            (translator, expression) => translator.TranslateProjected(expression));

        return new QueryingEnumerable<TResult, TResult>(
            mongoQueryContext,
            executableQuery,
            (_, e) => e,
            contextType,
            standAloneStateManager: false,
            threadSafetyChecksEnabled,
            GetOnZeroResultsAction(queryExpression));
    }

    private static QueryingEnumerable<BsonDocument, TResult> ExecuteShapedQuery<TSource, TResult>(
        QueryContext queryContext,
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoQueryExpression queryExpression,
        Func<QueryContext, BsonDocument, TResult> shaper,
        Type contextType,
        bool standAloneStateManager,
        bool threadSafetyChecksEnabled,
        ResultCardinality resultCardinality,
        NativeQueryMode nativeMode)
    {
        var (mongoQueryContext, executableQuery) = TranslateQuery<TSource>(
            queryContext, entityType, bsonSerializerFactory, queryExpression, resultCardinality,
            nativeEligible: true,
            streaming: false,
            nativeMode,
            (translator, expression) => translator.Translate(expression, resultCardinality));

        return new QueryingEnumerable<BsonDocument, TResult>(
            mongoQueryContext,
            executableQuery,
            shaper,
            contextType,
            standAloneStateManager,
            threadSafetyChecksEnabled,
            GetOnZeroResultsAction(queryExpression));
    }

    // Returns a type implementing both IEnumerable<TResult> and IAsyncEnumerable<TResult>: EF's query
    // executor builds a sync lambda returning IEnumerable<TResult> or an async lambda returning
    // IAsyncEnumerable<TResult> from the same compiled method, so its static return type must satisfy both.
    private static DispatchingQueryingEnumerable<TResult> ExecuteStreamingShapedQuery<TSource, TResult>(
        QueryContext queryContext,
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoQueryExpression queryExpression,
        Func<QueryContext, RawBsonDocument, TResult> streamingShaper,
        Func<QueryContext, BsonDocument, TResult> domShaper,
        Type contextType,
        bool standAloneStateManager,
        bool threadSafetyChecksEnabled,
        ResultCardinality resultCardinality,
        NativeQueryMode nativeMode)
    {
        var (mongoQueryContext, executableQuery) = TranslateQuery<TSource>(
            queryContext, entityType, bsonSerializerFactory, queryExpression, resultCardinality,
            nativeEligible: true,
            streaming: true,
            nativeMode,
            (translator, expression) => translator.Translate(expression, resultCardinality));

        // If native translation was actually used the rows are RawBsonDocuments materialized by the
        // streaming shaper. Otherwise (e.g. a Where predicate the native pipeline can't translate) the
        // executable query fell back to the driver-LINQ path, which yields plain BsonDocuments — so use the
        // DOM shaper. Both shapers were compiled at query-build time precisely so this run-time fallback is
        // type-correct rather than throwing.
        if (executableQuery.Streaming)
        {
            return new DispatchingQueryingEnumerable<TResult>(
                new QueryingEnumerable<RawBsonDocument, TResult>(
                    mongoQueryContext,
                    executableQuery,
                    streamingShaper,
                    contextType,
                    standAloneStateManager,
                    threadSafetyChecksEnabled,
                    GetOnZeroResultsAction(queryExpression)));
        }

        return new DispatchingQueryingEnumerable<TResult>(
            new QueryingEnumerable<BsonDocument, TResult>(
                mongoQueryContext,
                executableQuery,
                domShaper,
                contextType,
                standAloneStateManager,
                threadSafetyChecksEnabled,
                GetOnZeroResultsAction(queryExpression)));
    }

    /// <summary>
    /// Wraps the chosen <see cref="QueryingEnumerable{TSource,TTarget}"/> (RawBsonDocument streaming or
    /// BsonDocument DOM fallback) behind a single type implementing both <see cref="IEnumerable{T}"/> and
    /// <see cref="IAsyncEnumerable{T}"/>, so the streaming-path compiled query has a stable static return
    /// type usable by both EF's sync and async query executors.
    /// </summary>
    private sealed class DispatchingQueryingEnumerable<TResult>(
        object inner) : IEnumerable<TResult>, IAsyncEnumerable<TResult>
    {
        public IEnumerator<TResult> GetEnumerator()
            => ((IEnumerable<TResult>)inner).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public IAsyncEnumerator<TResult> GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken = default)
            => ((IAsyncEnumerable<TResult>)inner).GetAsyncEnumerator(cancellationToken);
    }

    private static readonly MethodInfo ExecuteShapedQueryMethodInfo =
        typeof(MongoShapedQueryCompilingExpressionVisitor)
            .GetTypeInfo()
            .DeclaredMethods
            .Single(m => m.Name == nameof(ExecuteShapedQuery));

    private static readonly MethodInfo ExecuteProjectedQueryMethodInfo =
        typeof(MongoShapedQueryCompilingExpressionVisitor)
            .GetTypeInfo()
            .DeclaredMethods
            .Single(m => m.Name == nameof(ExecuteProjectedQuery));

    private static readonly MethodInfo ExecuteStreamingShapedQueryMethodInfo =
        typeof(MongoShapedQueryCompilingExpressionVisitor)
            .GetTypeInfo()
            .DeclaredMethods
            .Single(m => m.Name == nameof(ExecuteStreamingShapedQuery));

    private static readonly MethodInfo CreateInnerSourceMethodInfo =
        typeof(MongoShapedQueryCompilingExpressionVisitor)
            .GetTypeInfo()
            .GetDeclaredMethod(nameof(CreateInnerSourceTyped))!;

    private static Expression CreateInnerSource(
        MongoQueryContext mongoQueryContext,
        BsonSerializerFactory bsonSerializerFactory,
        IReadOnlyEntityType innerEntityType,
        string collectionName,
        MongoTransaction? transaction)
    {
        return (Expression)CreateInnerSourceMethodInfo
            .MakeGenericMethod(innerEntityType.ClrType)
            .Invoke(null, [mongoQueryContext, bsonSerializerFactory, innerEntityType, collectionName, transaction])!;
    }

    private static Expression CreateInnerSourceTyped<TInner>(
        MongoQueryContext mongoQueryContext,
        BsonSerializerFactory bsonSerializerFactory,
        IReadOnlyEntityType innerEntityType,
        string collectionName,
        MongoTransaction? transaction)
    {
        // The driver's Join/GroupJoin pipeline translator requires the inner operand to be a bare
        // IMongoQueryable backed by a collection (a ConstantExpression). It rejects an operand wrapped
        // in .As(serializer) (a MethodCallExpression), so we cannot use .As(...) here as we do for the
        // outer source. Instead we wrap the collection so its DocumentSerializer returns EF's entity
        // serializer; the driver derives the inner pipeline-input serializer from collection.DocumentSerializer,
        // which keeps EF's element-name / discriminator / BsonRepresentation mappings on the inner side.
        var innerCollection = new SerializerOverrideCollection<TInner>(
            mongoQueryContext.MongoClient.GetCollection<TInner>(collectionName),
            (IBsonSerializer<TInner>)bsonSerializerFactory.GetEntitySerializer(innerEntityType));
        var innerQueryable = transaction == null
            ? innerCollection.AsQueryable()
            : innerCollection.AsQueryable(transaction.Session);
        return innerQueryable.Expression;
    }
}
