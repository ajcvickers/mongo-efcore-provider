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
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.EntityFrameworkCore.Extensions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
#if !EF8
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
#endif
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;
using MongoDB.EntityFrameworkCore.Query.Visitors.Dependencies;
using MongoDB.EntityFrameworkCore.Serializers;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.Query.Visitors;

#if !EF8
/// <summary>Builds the <see cref="MongoBulkPlan"/> behind <c>ExecuteDelete</c>/<c>ExecuteUpdate</c>: the server-side filter, the update document or pipeline, and the two-phase target-id query.</summary>
internal static class MongoBulkPlanBuilder
{
    /// <summary>Builds the plan for a bulk delete or update: filter, update and target-id queries are translated at execution time; only the definitions cross to the executor.</summary>
    internal static MongoBulkPlan Create(
        IReadOnlyEntityType entityType, BsonSerializerFactory bsonSerializerFactory, MongoNonQueryExpression nonQuery,
        MongoQueryMode mode)
        => (MongoBulkPlan)InvokeUnwrapped(
            CreateBulkPlanMethodInfo.MakeGenericMethod(entityType.ClrType), [entityType, bsonSerializerFactory, nonQuery, mode])!;

    // MethodInfo.Invoke wraps whatever the target throws in TargetInvocationException, whose message ("Exception has been thrown by the
    // target of an invocation") hides the real failure (e.g. the driver's "Expression not supported: ..."). Rethrow the original.
    private static object? InvokeUnwrapped(MethodInfo method, object?[] arguments)
    {
        try
        {
            return method.Invoke(null, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    // Generic over TSource so the deferred translation delegates close over the correctly-typed serializer/queryable;
    // invoked once via reflection from VisitNonQuery. Only the resulting definitions cross to the executor.
    private static MongoBulkPlan CreateBulkPlan<TSource>(
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery,
        MongoQueryMode mode)
    {
        var isUpdate = nonQuery.Kind == MongoNonQueryExpression.OperationKind.Update;
        var isTwoPhase = nonQuery.Strategy == MongoNonQueryExpression.BulkStrategy.TwoPhase;

        return new MongoBulkPlan
        {
            Kind = isUpdate ? MongoBulkOperationKind.Update : MongoBulkOperationKind.Delete,
            Strategy = isTwoPhase ? MongoBulkStrategy.TwoPhase : MongoBulkStrategy.SingleCommand,
            CollectionName = nonQuery.SourceQuery.CollectionExpression.CollectionName,
            BuildFilter = isTwoPhase
                ? null
                : qc => TranslateBulkOrThrow(nonQuery, () =>
                {
                    ThrowIfBulkMisreadsNullElements(qc, entityType, nonQuery, mode);
                    return BuildFilter<TSource>(qc, entityType, bsonSerializerFactory, nonQuery);
                }),
            // BuildUpdate needs no check of its own: the executor always calls BuildFilter (single command) or
            // BuildTargetIdQuery (two-phase) first, and that check covers the setter values too.
            BuildUpdate = isUpdate
                ? qc => TranslateBulkOrThrow(nonQuery, () => BuildUpdate<TSource>(qc, entityType, bsonSerializerFactory, nonQuery))
                : null,
            BuildTargetIdQuery = isTwoPhase
                ? qc =>
                {
                    TranslateBulkOrThrow(nonQuery, () =>
                    {
                        ThrowIfBulkMisreadsNullElements(qc, entityType, nonQuery, mode);
                        return true;
                    });
                    return BuildIdDocumentQuery<TSource>(qc, entityType, bsonSerializerFactory, nonQuery);
                }
                : null,
        };
    }

    /// <summary>
    /// A bulk filter and its <c>SetProperty</c> values always run on the driver-LINQ bridge, so a read of a complex
    /// collection's element that the driver evaluates differently from a query is refused before any document is
    /// written (the strict allow-list of <c>ComplexElementNullGuardRefusal</c>). Runs in <c>BuildFilter</c> and
    /// <c>BuildTargetIdQuery</c>, one of which <c>MongoBulkOperationExecutor</c> calls before anything else for every plan
    /// (and before <c>BuildUpdate</c>), with this execution's parameter values. A no-op on EF9 (no complex collections) and
    /// under explicit <see cref="MongoQueryMode.DriverLinq"/>.
    /// </summary>
    private static void ThrowIfBulkMisreadsNullElements(
        QueryContext queryContext, IReadOnlyEntityType entityType, MongoNonQueryExpression nonQuery, MongoQueryMode mode)
    {
#if !EF8 && !EF9
        ComplexElementNullGuardRefusal.ThrowIfBulkMisreadsNullElements(
            // The source chain without the ExecuteUpdate/ExecuteDelete marker (whose property-selector lambdas are targets,
            // checked by BuildSetter, not reads), and each setter value.
            [MongoNonQueryExpression.UnwrapBulkOperator(nonQuery.SourceQuery.CapturedExpression), .. nonQuery.Setters.Select(s => s.ValueExpression)],
            (IModel)entityType.Model, mode, name => queryContext.Parameters[name]);
#endif
    }

    // Bulk filter/update are translated at execution time. A shape that slipped past compile-time validation makes the driver throw a raw
    // ExpressionNotSupportedException/ArgumentException: convert those to EF's non-query translation failure. InvalidOperationException
    // is left as-is (it already carries that message or the cross-DbSet rejection).
    private static T TranslateBulkOrThrow<T>(MongoNonQueryExpression nonQuery, Func<T> translate)
    {
        try
        {
            return translate();
        }
        catch (Exception exception) when (exception is not InvalidOperationException)
        {
            throw NonQueryTranslationFailed(
                ExpressionShapePrinter.PrintQuery(nonQuery.SourceQuery.CapturedExpression), exception.Message, exception);
        }
    }

    // EF11 dropped the "with details" message: the details travel as the inner exception instead.
    internal static InvalidOperationException NonQueryTranslationFailed(
        object? expression, string? details, Exception? innerException = null)
#if EF11
        => new(CoreStrings.NonQueryTranslationFailed(expression), innerException ?? new InvalidOperationException(details));
#else
        => new(CoreStrings.NonQueryTranslationFailedWithDetails(expression, details), innerException);
#endif

    internal static void EnsureKeyOrThrow(IReadOnlyEntityType entityType, MongoNonQueryExpression nonQuery)
    {
        if (entityType.FindPrimaryKey() == null)
        {
            throw NonQueryTranslationFailed(
                ExpressionShapePrinter.PrintQuery(nonQuery.SourceQuery.CapturedExpression),
                "the entity must have a primary key to use ordering, paging, or Distinct in a bulk delete or update.");
        }
    }

    // Builds a driver query yielding the raw stored BsonDocuments for the bulk source, via the read path's TranslateQuery (so the driver-LINQ
    // bridge stays alive even once no read query falls back). Fetches whole documents and keeps only _id; a { _id: 1 } projection could cut transfer.
    private static IQueryable<BsonDocument> BuildIdDocumentQuery<TSource>(
        QueryContext queryContext,
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery)
    {
        var (_, executableQuery) = MongoShapedQueryCompilingExpressionVisitor.TranslateQuery<TSource>(
            queryContext, entityType, bsonSerializerFactory, nonQuery.SourceQuery, ResultCardinality.Enumerable,
            nativeFactory: null, streaming: false,
            (translator, expression) =>
                // guardUnstrippableForceUnwindJoin: false — the guard protects a pre-built read-path shaper; the
                // bulk path builds none, so it would only be a false-positive throw.
                translator.Translate(
                    MongoNonQueryExpression.UnwrapBulkOperator(expression)!, ResultCardinality.Enumerable,
                    guardUnstrippableForceUnwindJoin: false));

        return executableQuery.Provider.CreateQuery<BsonDocument>(executableQuery.Query);
    }

    /// <summary>
    /// Builds the server-side filter scoping a bulk operation from the predicates of every <c>Where</c> in the captured chain, lowered through
    /// the EF-to-driver-LINQ visitor and rendered with the EF entity serializer. An empty chain matches every document.
    /// </summary>
    private static FilterDefinition<BsonDocument> BuildFilter<TSource>(
        QueryContext queryContext,
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery)
    {
        var sharedParameter = Expression.Parameter(typeof(TSource), "e");
        var translator = new MongoEFToLinqTranslatingExpressionVisitor(
            queryContext, Expression.Constant(null, typeof(IQueryable<TSource>)), bsonSerializerFactory);

        Expression? combinedBody = null;
        var expression = MongoNonQueryExpression.UnwrapBulkOperator(nonQuery.SourceQuery.CapturedExpression);
        // ClassifyBulkSource has already rejected any operator other than Queryable.Where on the single-command path.
        while (expression is MethodCallExpression { Method: { DeclaringType: var declaringType, Name: nameof(Queryable.Where) } }
                   whereCall
               && declaringType == typeof(Queryable))
        {
            var predicate = whereCall.Arguments[1].UnwrapLambdaFromQuote();
            var translatedBody = translator.Visit(predicate.Body)!;
            translatedBody = ReplacingExpressionVisitor.Replace(predicate.Parameters[0], sharedParameter, translatedBody);

            combinedBody = combinedBody == null ? translatedBody : Expression.AndAlso(combinedBody, translatedBody);

            expression = whereCall.Arguments[0];
        }

        // Fail loud: a remaining non-Where operator means ClassifyBulkSource admitted something this builder ignores, which would affect the wrong documents.
        if (expression is MethodCallExpression { Method.DeclaringType: var remainingType } remaining
            && remainingType == typeof(Queryable))
        {
            throw new InvalidOperationException(
                $"Bulk filter construction encountered an unsupported '{remaining.Method.Name}' operator in the source "
                + "chain. Only 'Where' can scope a single-command bulk filter; this indicates the bulk-source classifier "
                + "admitted an operator the filter builder does not handle.");
        }

        if (combinedBody == null)
        {
            return FilterDefinition<BsonDocument>.Empty;
        }

        var predicateLambda = Expression.Lambda<Func<TSource, bool>>(combinedBody, sharedParameter);
        var efSerializer = (IBsonSerializer<TSource>)bsonSerializerFactory.GetEntitySerializer(entityType);
        var rendered = new ExpressionFilterDefinition<TSource>(predicateLambda)
            .Render(new RenderArgs<TSource>(efSerializer, BsonSerializer.SerializerRegistry));

        return rendered;
    }

    /// <summary>
    /// Builds the update for a bulk update: a plain <c>$set</c> document of serialized literals, or, when any setter references the updated
    /// entity, an aggregation pipeline with a single <c>$set</c> stage (rendered expressions and <c>$literal</c>-wrapped constants can mix).
    /// </summary>
    private static UpdateDefinition<BsonDocument> BuildUpdate<TSource>(
        QueryContext queryContext,
        IReadOnlyEntityType entityType,
        BsonSerializerFactory bsonSerializerFactory,
        MongoNonQueryExpression nonQuery)
    {
        var setters = nonQuery.Setters;

        if (!setters.Any(s => s.IsSelfReferencing))
        {
            var setDoc = new BsonDocument();
            foreach (var setter in setters)
            {
                setDoc[setter.StoredPath] = SerializeConstant(queryContext, setter);
            }

            return new BsonDocumentUpdateDefinition<BsonDocument>(new BsonDocument("$set", setDoc));
        }

        var efSerializer = (IBsonSerializer<TSource>)bsonSerializerFactory.GetEntitySerializer(entityType);
        var entityParameter = Expression.Parameter(typeof(TSource), "e");
        var setStageDoc = new BsonDocument();

        foreach (var setter in setters)
        {
            if (setter.IsSelfReferencing)
            {
                setStageDoc[setter.StoredPath] = RenderSelfReferencingValue<TSource>(
                    queryContext, bsonSerializerFactory, entityParameter, efSerializer, setter);
            }
            else
            {
                // Inside an aggregation stage a '$'-prefixed string is a field path/variable and a document can be
                // an operator expression, so constant values must be wrapped in $literal to be stored as data.
                setStageDoc[setter.StoredPath] =
                    new BsonDocument("$literal", SerializeConstant(queryContext, setter));
            }
        }

        var setStage = new BsonDocument("$set", setStageDoc);
        var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(new[] { setStage });
        return Builders<BsonDocument>.Update.Pipeline(pipeline);
    }

    /// <summary>
    /// Evaluates a constant/parameter setter value to a CLR constant and serializes it to a <see cref="BsonValue"/>
    /// using the same property serializer the write pipeline uses, so enum / Guid / representation handling matches
    /// inserts and SaveChanges updates.
    /// </summary>
    private static BsonValue SerializeConstant(QueryContext queryContext, MongoNonQueryExpression.Setter setter)
    {
        var value = EvaluateToConstant(queryContext, setter.ValueExpression);
        if (setter.Property is IComplexProperty complexProperty)
        {
            // A null REQUIRED complex property or collection would be stored, then fail every read ("... is null for required
            // complex property ..."); SaveChanges rejects the same value (EF), so this does too, before any write.
            if (value is null && !complexProperty.IsOptional())
            {
                throw new InvalidOperationException(
                    ComplexTypeRefusals.BulkSetRequiredComplexToNull(complexProperty.DeclaringType.DisplayName(), complexProperty.Name));
            }

            // A whole complex value: the stored form SaveChanges writes (same writer), or BSON null for an optional one.
            return ComplexValueWriter.SerializeDetachedComplexValue(complexProperty, value);
        }

        var serializationInfo = BsonSerializerFactory.GetPropertySerializationInfo((IReadOnlyProperty)setter.Property);
        return serializationInfo.SerializeValue(value);
    }

    /// <summary>
    /// Renders a self-referencing setter value (e.g. <c>o =&gt; o.Quantity + 1</c>) to an aggregation-expression
    /// <see cref="BsonValue"/>: lowered through the EF→driver-LINQ visitor, rebound to one shared parameter, and
    /// rendered with the EF entity serializer so element names honor the EF model.
    /// </summary>
    private static BsonValue RenderSelfReferencingValue<TSource>(
        QueryContext queryContext,
        BsonSerializerFactory bsonSerializerFactory,
        ParameterExpression entityParameter,
        IBsonSerializer<TSource> efSerializer,
        MongoNonQueryExpression.Setter setter)
    {
        var translator = new MongoEFToLinqTranslatingExpressionVisitor(
            queryContext, Expression.Constant(null, typeof(IQueryable<TSource>)), bsonSerializerFactory)
        {
            // The value is written back to the document, so it must stay in the stored representation (EF-337).
            ReadStoredForm = true
        };
        var translatedBody = translator.Visit(setter.ValueExpression)!;

        translatedBody = new ParameterRebindingExpressionVisitor(entityParameter).Visit(translatedBody);

        var resultType = setter.Property.ClrType;
        var renderer = RenderAggregateExpressionMethodInfo.MakeGenericMethod(typeof(TSource), resultType);
        return (BsonValue)InvokeUnwrapped(renderer, [entityParameter, translatedBody, efSerializer])!;
    }

    private static BsonValue RenderAggregateExpression<TSource, TResult>(
        ParameterExpression entityParameter,
        Expression body,
        IBsonSerializer<TSource> efSerializer)
    {
        var lambda = Expression.Lambda<Func<TSource, TResult>>(
            body.Type == typeof(TResult) ? body : Expression.Convert(body, typeof(TResult)),
            entityParameter);
        return new ExpressionAggregateExpressionDefinition<TSource, TResult>(lambda)
            .Render(new RenderArgs<TSource>(efSerializer, BsonSerializer.SerializerRegistry));
    }

    /// <summary>
    /// Evaluates a setter value expression (constant, captured closure, or query parameter) to a CLR value.
    /// Unlike <c>MongoEFToLinqTranslatingExpressionVisitor.TryEvaluateToConstant</c>, this method intentionally
    /// lets compile-and-evaluate failures propagate as exceptions — throwing on an un-evaluatable setter value
    /// is preferable to silently writing null.
    /// </summary>
    private static object? EvaluateToConstant(QueryContext queryContext, Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
        {
            expression = convert.Operand;
        }

        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        if (expression is MemberExpression { Expression: ConstantExpression closureConstant } member)
        {
            return member.Member switch
            {
                FieldInfo field => field.GetValue(closureConstant.Value),
                PropertyInfo prop => prop.GetValue(closureConstant.Value),
                _ => CompileAndEvaluate(expression)
            };
        }

#if EF8 || EF9
        if (expression is ParameterExpression param
            && param.Name?.StartsWith(QueryCompilationContext.QueryParameterPrefix, StringComparison.Ordinal) == true
            && queryContext.ParameterValues.TryGetValue(param.Name, out var value))
        {
            return value;
        }
#else
        if (expression is Microsoft.EntityFrameworkCore.Query.QueryParameterExpression queryParam)
        {
            return queryContext.Parameters[queryParam.Name];
        }
#endif

        return CompileAndEvaluate(expression);
    }

    private static object? CompileAndEvaluate(Expression expression)
        => Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile()();

    /// <summary>Rebinds every parameter in a translated self-referencing setter body to one shared parameter, as the renderer requires.</summary>
    private sealed class ParameterRebindingExpressionVisitor(ParameterExpression target)
        : System.Linq.Expressions.ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            // Rebind by type: translation discards the original parameter identity, and the value body has exactly one TSource-typed parameter.
            => node.Type == target.Type ? target : base.VisitParameter(node);
    }

    private static readonly MethodInfo CreateBulkPlanMethodInfo =
        typeof(MongoBulkPlanBuilder)
            .GetTypeInfo()
            .DeclaredMethods
            .Single(m => m.Name == nameof(CreateBulkPlan));

    private static readonly MethodInfo RenderAggregateExpressionMethodInfo =
        typeof(MongoBulkPlanBuilder)
            .GetTypeInfo()
            .DeclaredMethods
            .Single(m => m.Name == nameof(RenderAggregateExpression));
}
#endif
