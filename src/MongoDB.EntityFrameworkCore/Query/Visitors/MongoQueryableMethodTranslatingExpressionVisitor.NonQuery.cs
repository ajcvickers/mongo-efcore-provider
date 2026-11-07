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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation.Stages;

namespace MongoDB.EntityFrameworkCore.Query.Visitors;

// ExecuteDelete/ExecuteUpdate translation into a MongoNonQueryExpression.
internal sealed partial class MongoQueryableMethodTranslatingExpressionVisitor
{
#if !EF8
    protected override Expression TranslateExecuteDelete(ShapedQueryExpression source)
    {
        var mongoQueryExpression = (MongoQueryExpression)source.QueryExpression;
        var strategy = ClassifyBulkSource(mongoQueryExpression);
        return new MongoNonQueryExpression(mongoQueryExpression, strategy);
    }

#if EF10 || EF11
    protected override Expression TranslateExecuteUpdate(
        ShapedQueryExpression source,
        IReadOnlyList<ExecuteUpdateSetter> setters)
    {
        var mongoQueryExpression = (MongoQueryExpression)source.QueryExpression;
        var strategy = ClassifyBulkSource(mongoQueryExpression);
        var parsed = setters
            .Select(s => BuildSetter(mongoQueryExpression, s.PropertySelector, s.ValueExpression))
            .ToList();
        ThrowIfSetterPathsOverlap(mongoQueryExpression, parsed);
        return new MongoNonQueryExpression(mongoQueryExpression, parsed, strategy);
    }
#else
    protected override Expression? TranslateExecuteUpdate(
        ShapedQueryExpression source,
        LambdaExpression setPropertyCalls)
    {
        var mongoQueryExpression = (MongoQueryExpression)source.QueryExpression;
        var strategy = ClassifyBulkSource(mongoQueryExpression);

        var parsed = new List<MongoNonQueryExpression.Setter>();
        var body = setPropertyCalls.Body;
        // Built outer-to-inner: s.SetProperty(a).SetProperty(b) parses as (s.SetProperty(a)).SetProperty(b), so walk Object
        // inward, inserting at the front to preserve the authored order.
        while (body is MethodCallExpression { Method.Name: "SetProperty" } call)
        {
            var selector = call.Arguments[0].UnwrapLambdaFromQuote();
            // For the self-referencing SetProperty overload the value arg is a quoted Func<T,TProp>
            // lambda; for the constant overload it is the value expression directly.
            var value = call.Arguments[1];
            parsed.Insert(0, BuildSetter(mongoQueryExpression, selector, value));
            body = call.Object!;
        }

        // EF10 validates "at least one SetProperty" before the provider, but EF9 hands the raw lambda through, so a setter
        // lambda with no SetProperty call must be rejected here rather than silently running a no-op updateMany.
        if (parsed.Count == 0)
        {
            AddTranslationErrorDetails(
                "An 'ExecuteUpdate' call must specify at least one 'SetProperty' invocation, "
                + "to indicate the properties to be updated.");
            throw MongoBulkPlanBuilder.NonQueryTranslationFailed(
                ExpressionShapePrinter.PrintQuery(mongoQueryExpression.CapturedExpression), TranslationErrorDetails);
        }

        ThrowIfSetterPathsOverlap(mongoQueryExpression, parsed);
        return new MongoNonQueryExpression(mongoQueryExpression, parsed, strategy);
    }
#endif

    /// <summary>
    /// Refuses two setters where one's stored path is a strict prefix of the other's (<c>SetProperty(c => c.Address, v)</c>
    /// with <c>SetProperty(c => c.Address.City, x)</c>): the server rejects such a <c>$set</c> ("would create a conflict")
    /// only when the command runs. Only complex targets have dotted paths, and the model validator keeps element names
    /// unique per document level, so root-scalar-only updates never overlap (a strict no-op for them). The same path set
    /// twice is not an overlap: the update document keeps the last value, as before.
    /// </summary>
    private void ThrowIfSetterPathsOverlap(
        MongoQueryExpression mongoQueryExpression,
        IReadOnlyList<MongoNonQueryExpression.Setter> setters)
    {
        for (var i = 1; i < setters.Count; i++)
        {
            var path = setters[i].StoredPath;
            for (var j = 0; j < i; j++)
            {
                var earlier = setters[j].StoredPath;
                if (path.StartsWith(earlier + ".", StringComparison.Ordinal)
                    || earlier.StartsWith(path + ".", StringComparison.Ordinal))
                {
                    AddTranslationErrorDetails(
                        ComplexTypeRefusals.BulkSetterPathsOverlap(
                            earlier, path, mongoQueryExpression.CollectionExpression.EntityType.DisplayName()));
                    throw MongoBulkPlanBuilder.NonQueryTranslationFailed(
                        ExpressionShapePrinter.PrintQuery(mongoQueryExpression.CapturedExpression), TranslationErrorDetails);
                }
            }
        }
    }

    /// <summary>
    /// Parses a <c>SetProperty(selector, value)</c> into a <see cref="MongoNonQueryExpression.Setter"/>. The selector must
    /// target a mapped root scalar property; anything else produces EF's canonical non-query translation failure.
    /// </summary>
    private MongoNonQueryExpression.Setter BuildSetter(
        MongoQueryExpression mongoQueryExpression,
        LambdaExpression propertySelector,
        Expression valueExpression)
    {
        var entityType = mongoQueryExpression.CollectionExpression.EntityType;

        var selectorBody = propertySelector.Body;
        while (selectorBody is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
        {
            selectorBody = convert.Operand;
        }

        IPropertyBase? property = null;
        string? complexElementPath = null;
        if (selectorBody is MemberExpression { Expression: var memberSource } member
            && memberSource != null
            && propertySelector.Parameters.Count == 1
            && IsParameterReference(memberSource, propertySelector.Parameters[0]))
        {
            property = entityType.FindProperty(member.Member.Name);
        }
        // Also accept an EF.Property<TProperty>(entity, "Name") selector, e.g.
        // SetProperty(c => EF.Property<string>(c, "ContactName"), ...).
        else if (selectorBody is MethodCallExpression efPropertyCall
                 && efPropertyCall.Method.IsEFPropertyMethod()
                 && propertySelector.Parameters.Count == 1
                 && IsParameterReference(efPropertyCall.Arguments[0], propertySelector.Parameters[0])
                 && efPropertyCall.Arguments[1] is ConstantExpression { Value: string efPropertyName })
        {
            property = entityType.FindProperty(efPropertyName);
        }

        // A complex target: a scalar leaf through single complex hops (`c => c.Address.City`, `c.Address.Location.Lat`), or
        // a whole complex property / collection (`c => c.Address`). Resolved structurally to the stored path, so element-name
        // overrides apply; a hop with an OPTIONAL ancestor is refused (see TryResolveComplexSetterTarget).
        if (property == null
            && TryResolveComplexSetterTarget(mongoQueryExpression, entityType, propertySelector, selectorBody, out var complexTarget, out var complexPath))
        {
            property = complexTarget;
            complexElementPath = complexPath;
        }

        if (property == null)
        {
            AddTranslationErrorDetails(
                ComplexTypeRefusals.BulkSetterTargetNotMapped(ExpressionShapePrinter.Print(propertySelector.Body), entityType.DisplayName()));
            throw MongoBulkPlanBuilder.NonQueryTranslationFailed(
                ExpressionShapePrinter.PrintQuery(mongoQueryExpression.CapturedExpression), TranslationErrorDetails);
        }

        // EF9 self-referencing: value is a quoted Func<T,TProp> lambda; unwrap and detect a reference to its parameter.
        // EF10 (and EF9 constants): value is the raw expression; detect a reference to the setter's lambda parameter.
        bool isSelfReferencing;
        Expression value;
        if (IsQuotedLambda(valueExpression))
        {
            var valueLambda = valueExpression.UnwrapLambdaFromQuote();
            value = valueLambda.Body;
            isSelfReferencing = valueLambda.Parameters.Any(p => value.ReferencesParameter(p));
        }
        else
        {
            value = valueExpression;
            isSelfReferencing = propertySelector.Parameters.Any(p => value.ReferencesParameter(p));
        }

        if (isSelfReferencing && property is IProperty scalar && scalar.GetTypeMapping().Converter != null)
        {
            AddTranslationErrorDetails(
                $"Self-referencing ExecuteUpdate on property '{property.Name}' is not supported because it uses a value converter.");
            throw MongoBulkPlanBuilder.NonQueryTranslationFailed(
                ExpressionShapePrinter.PrintQuery(mongoQueryExpression.CapturedExpression), TranslationErrorDetails);
        }

        // A self-referencing value for a complex LEAF stored with a BsonRepresentation would be written in the value's own
        // BSON type (an int into a field stored as a string), then miss every equality filter on the stored form. Refused for
        // complex leaves only: root scalars have the same pre-existing behaviour, which is pinned and left unchanged.
        if (isSelfReferencing && complexElementPath is not null && property is IProperty representedLeaf
            && representedLeaf.GetBsonRepresentation() is not null)
        {
            AddTranslationErrorDetails(ComplexTypeRefusals.BulkSelfReferencingBsonRepresentationLeaf(property.Name));
            throw MongoBulkPlanBuilder.NonQueryTranslationFailed(
                ExpressionShapePrinter.PrintQuery(mongoQueryExpression.CapturedExpression), TranslationErrorDetails);
        }

        // A whole complex value computed from the row (`c.Billing = c.Shipping`) would copy the STORED subdocument, whose
        // element names, converters and representations belong to the source property, and the driver can't render a
        // complex value: refused rather than written in the wrong stored form.
        if (isSelfReferencing && property is IComplexProperty selfReferencedComplex)
        {
            AddTranslationErrorDetails(
                ComplexTypeRefusals.BulkSelfReferencingComplexValue(
                    $"{selfReferencedComplex.DeclaringType.DisplayName()}.{selfReferencedComplex.Name}"));
            throw MongoBulkPlanBuilder.NonQueryTranslationFailed(
                ExpressionShapePrinter.PrintQuery(mongoQueryExpression.CapturedExpression), TranslationErrorDetails);
        }

        return new MongoNonQueryExpression.Setter(property, value, isSelfReferencing, complexElementPath);
    }

    /// <summary>
    /// Resolves a <c>SetProperty</c> target reached through complex properties: a scalar leaf of a (nested) complex property
    /// or a whole complex property / complex collection, to the property and its dotted stored path
    /// (<see cref="StructuralPath.TryResolve"/>, so element-name overrides apply). Leaves of complex COLLECTION elements are
    /// not addressable by one path and are not resolved here.
    /// </summary>
    /// <remarks>
    /// A target below an OPTIONAL complex property (EF10) is refused: the server's <c>$set</c> of a dotted path under a null
    /// parent is an error ("Cannot create field ... in element {Opt: null}") raised mid-operation (documents before it are
    /// already written), and the pipeline form silently creates a parent holding only that member (its other required
    /// members then read as missing). Setting the optional property itself is supported.
    /// </remarks>
    private bool TryResolveComplexSetterTarget(
        MongoQueryExpression mongoQueryExpression,
        IEntityType entityType,
        LambdaExpression propertySelector,
        Expression selectorBody,
        [NotNullWhen(true)] out IPropertyBase? target,
        [NotNullWhen(true)] out string? path)
    {
        target = null;
        path = null;
        if (propertySelector.Parameters.Count != 1)
        {
            return false;
        }

        var names = new List<string>();
        var current = selectorBody.RemoveConvert()!;
        while (current.TryGetMemberOrEFProperty(out var receiver, out var name))
        {
            names.Insert(0, name);
            current = receiver.RemoveConvert()!;
        }

        if (names.Count == 0 || !ReferenceEquals(current, propertySelector.Parameters[0])
            || !StructuralPath.TryResolve(entityType, names, names.Count - 1, out var resolved)
            || resolved.Leaf is not (IProperty or IComplexProperty))
        {
            return false;
        }

        // Every hop must be a complex property (an owned navigation hop is not a bulk-update target; complex properties
        // directly on the entity have no hop).
        ITypeBase hopScope = entityType;
        for (var i = 0; i < names.Count - 1; i++)
        {
            if (hopScope.FindComplexProperty(names[i]) is not { IsCollection: false } hop)
            {
                return false;
            }

            if (hop.IsOptional())
            {
                AddTranslationErrorDetails(
                    ComplexTypeRefusals.BulkSetUnderOptionalComplexHop(
                        ExpressionShapePrinter.Print(propertySelector.Body), $"{hop.DeclaringType.DisplayName()}.{hop.Name}"));
                throw MongoBulkPlanBuilder.NonQueryTranslationFailed(
                    ExpressionShapePrinter.PrintQuery(mongoQueryExpression.CapturedExpression), TranslationErrorDetails);
            }

            hopScope = hop.ComplexType;
        }

        target = (IPropertyBase)resolved.Leaf;
        path = string.Join(".", resolved.Segments);
        return true;
    }

    private static bool IsQuotedLambda(Expression expression)
        => expression is LambdaExpression
           || expression is UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression };

    private static bool IsParameterReference(Expression expression, ParameterExpression parameter)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
        {
            expression = convert.Operand;
        }

        return expression == parameter;
    }

    /// <summary>
    /// Classifies the captured source chain of a bulk delete/update. Only <c>Where</c> is the single-command atomic path;
    /// <c>OrderBy</c>/<c>ThenBy</c>/<c>Skip</c>/<c>Take</c>/<c>Distinct</c> require the two-phase path (query target
    /// <c>_id</c>s, then act by <c>$in</c>). Anything else produces EF's canonical non-query translation failure.
    /// </summary>
    private MongoNonQueryExpression.BulkStrategy ClassifyBulkSource(MongoQueryExpression mongoQueryExpression)
    {
        var expression = MongoNonQueryExpression.UnwrapBulkOperator(mongoQueryExpression.CapturedExpression);
        var strategy = MongoNonQueryExpression.BulkStrategy.SingleCommand;

        while (expression is MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType != typeof(Queryable))
            {
                ThrowBulkSourceNotSupported(mongoQueryExpression, methodCallExpression.Method.Name);
            }

            switch (methodCallExpression.Method.Name)
            {
                case nameof(Queryable.Where):
                    break;

                case nameof(Queryable.OrderBy):
                case nameof(Queryable.OrderByDescending):
                case nameof(Queryable.ThenBy):
                case nameof(Queryable.ThenByDescending):
                case nameof(Queryable.Skip):
                case nameof(Queryable.Take):
                case nameof(Queryable.Distinct):
                    strategy = MongoNonQueryExpression.BulkStrategy.TwoPhase;
                    break;

                default:
                    ThrowBulkSourceNotSupported(mongoQueryExpression, methodCallExpression.Method.Name);
                    break;
            }

            expression = methodCallExpression.Arguments[0];
        }

        return strategy;
    }

    [DoesNotReturn]
    private void ThrowBulkSourceNotSupported(MongoQueryExpression mongoQueryExpression, string operatorName)
    {
        AddTranslationErrorDetails(
            $"The '{operatorName}' operator is not supported in a bulk delete or update. Only 'Where' predicates "
            + "and the 'OrderBy', 'OrderByDescending', 'ThenBy', 'ThenByDescending', 'Skip', 'Take', and 'Distinct' "
            + "operators can scope a bulk operation.");
        throw MongoBulkPlanBuilder.NonQueryTranslationFailed(
            ExpressionShapePrinter.PrintQuery(mongoQueryExpression.CapturedExpression), TranslationErrorDetails);
    }
#endif
}
