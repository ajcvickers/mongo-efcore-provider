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
using Microsoft.EntityFrameworkCore.Infrastructure; // MakeMemberAccess()/Assign() expression extensions
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Query.Expressions;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.Query.Visitors;

/// <summary>
/// The single place that turns a complex property's stored BSON (a subdocument, or on EF10 an array of subdocuments)
/// into its CLR value. Used by the DOM shaper (<see cref="MongoProjectionBindingRemovingExpressionVisitor"/>, including
/// its mixed subclass) and by the one-pass streaming materializer (<c>MongoStreamingEntityMaterializerRewriter</c>),
/// both for a complex property of a materialized entity and for a projected whole complex value.
/// </summary>
/// <remarks>
/// <para>
/// <b>Source.</b> <see cref="Build"/> takes an expression yielding the property's stored element as a
/// <see cref="BsonValue"/>, or <see langword="null"/> when the element is MISSING. The DOM shaper reads it out of the
/// parent <see cref="BsonDocument"/> (<see cref="CreateGetElement"/>); the one-pass reader reads that single element off
/// the <c>IBsonReader</c> into a local. Everything below the element is then built here, once, for both.
/// </para>
/// <para>
/// <b>Read rules.</b> A complex value is structural, so it is read strictly, like a whole entity (not like a bare
/// scalar projection leaf, D-F10):
/// </para>
/// <list type="bullet">
/// <item>A required complex property whose element is missing or BSON null throws <see cref="InvalidOperationException"/>
/// ("Document element 'X' is missing/null for required complex property 'T.X'"): never a null instance or a default
/// struct. EF8, EF9 and EF10 all refuse to save a null required complex property, so such a document is malformed.</item>
/// <item>An optional complex property (EF10) reads <see langword="null"/> for a missing or null element. A present
/// <c>{}</c> is a value: an instance whose leaves follow the rule below.</item>
/// <item>Leaves are read exactly like entity scalar properties, through the same building block
/// (<see cref="BsonBinding.CreateGetValueExpression(Expression, string?, bool, Type, ITypeBase)"/>, so the property's own
/// serializer applies: value converters, <c>BsonRepresentation</c>, enums, Guid, DateTime kind, decimal). A missing
/// required leaf throws "Document element is missing for required non-nullable property"; an explicit null throws for a
/// reference type and reads <c>default</c> for a value type, as for an entity member.</item>
/// <item>A complex collection (EF10) that is missing or null reads empty when required (as an owned collection does)
/// and <see langword="null"/> when optional; a null element reads <see langword="null"/>.</item>
/// <item>A stored value of the wrong BSON type (not a document, not an array) throws <see cref="FormatException"/>, the
/// exception type a wrong-typed scalar throws through its serializer.</item>
/// <item>Elements are looked up by name, so their stored order doesn't matter; unmapped elements are ignored.</item>
/// </list>
/// <para>
/// <b>Construction</b> follows EF's own materializer for the complex type: its <see cref="ITypeBase.ConstructorBinding"/>
/// (a parameterless constructor, a struct's default, or a constructor binding properties, e.g. a positional record),
/// then member assignments for every non-shadow property and nested complex property the constructor didn't consume.
/// A struct is built on a local and assigned as a whole. Shadow leaves (possible on EF8/EF9 only) have no CLR member and
/// aren't materialized; see <see cref="MarkComplexPropertyAssignments"/>. A complex value has no entry of its own: a
/// tracked entity's snapshot takes its leaves off the materialized instance (EF's own snapshot factory), so a later leaf
/// change is detected.
/// </para>
/// </remarks>
internal static class ComplexTypeMaterializationBuilder
{
    /// <summary>
    /// Builds the expression that materializes <paramref name="complexProperty"/>'s value, typed as its CLR type.
    /// </summary>
    /// <param name="complexProperty">The complex property (single or, on EF10, a collection).</param>
    /// <param name="storedElement">
    /// An expression of type <see cref="BsonValue"/>: the stored element, or <see langword="null"/> when it is missing.
    /// </param>
    internal static Expression Build(IComplexProperty complexProperty, Expression storedElement)
    {
        var clrType = complexProperty.ClrType;
        var value = Expression.Variable(typeof(BsonValue), "complexValue");

        var present = complexProperty.IsCollection
            ? BuildCollection(complexProperty, value)
            : Expression.Condition(
                Expression.Property(value, IsBsonDocumentProperty),
                Construct(complexProperty.ComplexType, Expression.Property(value, AsBsonDocumentProperty)).ConvertIfRequired(clrType),
                Expression.Throw(Expression.Call(WrongBsonTypeMethod, Expression.Constant(complexProperty), value), clrType));

        return Expression.Block(
            clrType,
            [value],
            Expression.Assign(value, storedElement),
            Expression.Condition(
                Expression.Equal(value, Expression.Constant(null, typeof(BsonValue))),
                Absent(complexProperty, isNull: false),
                Expression.Condition(
                    Expression.Property(value, IsBsonNullProperty),
                    Absent(complexProperty, isNull: true),
                    present)));
    }

    /// <summary>
    /// The stored element named <paramref name="elementName"/> of <paramref name="document"/> (an expression of type
    /// <see cref="BsonDocument"/>), or <see langword="null"/> when the element or the document itself is missing.
    /// </summary>
    internal static Expression CreateGetElement(Expression document, string elementName)
        => Expression.Call(GetElementMethod, document, Expression.Constant(elementName));

    /// <summary>
    /// Replaces EF's inline construction of every complex property of an entity in an injected materializer with a
    /// <see cref="ComplexPropertyMaterializationExpression"/>, so the shaper materializes it from the property's own
    /// stored element instead of reading its leaves as columns of the entity's value buffer. Runs once over the
    /// injected shaper body, before the DOM or one-pass shaper sees it.
    /// </summary>
    /// <remarks>
    /// EF (8, 9 and 10 alike) assigns each non-shadow complex property of the concrete entity type inside the switch
    /// case for that type: <c>instance.&lt;Address&gt;k__BackingField = { complexType = new Address(); ... }</c> (an
    /// optional one on EF10 under a null test of its first required leaf; a complex collection as <c>default</c>). The
    /// whole right-hand side is replaced. A complex-leaf read left anywhere afterwards would read the entity's own
    /// document, so it fails loudly instead; that is defence in depth: the only producer would be a complex property bound
    /// to an entity constructor parameter, which EF (8 and 10 measured) rejects at model building. Shadow
    /// complex leaves (EF8/EF9 only; EF10 rejects them) have no CLR member and are not read. Measured: EF8/EF9 can't
    /// track such an entity from a query at all (EF's own shadow-value snapshot indexing throws, in every query mode),
    /// so only no-tracking queries of such a model materialize; see <c>ComplexTypeMaterializationTests.Shadow_leaf_on_a_complex_type</c>.
    /// </remarks>
    internal static Expression MarkComplexPropertyAssignments(Expression injectedBody)
    {
        var marked = new MarkingVisitor().Visit(injectedBody);
        new LeftoverComplexLeafReadFinder().Visit(marked);
        return marked;
    }

    private static Expression Absent(IComplexProperty complexProperty, bool isNull)
    {
        var clrType = complexProperty.ClrType;

        if (complexProperty.IsOptional())
        {
            return Expression.Default(clrType);
        }

        if (complexProperty.IsCollection)
        {
            // A required collection stored missing or null reads empty, as an owned collection does
            // (MongoProjectionBindingRemovingExpressionVisitor's CollectionShaperExpression case).
            return CreateCollection(complexProperty, Expression.Constant(new BsonArray()));
        }

        return Expression.Throw(
            Expression.New(
                InvalidOperationExceptionConstructor,
                Expression.Constant(
                    $"Document element '{complexProperty.GetElementName()}' is {(isNull ? "null" : "missing")} for required complex property '{DisplayName(complexProperty)}'.")),
            clrType);
    }

    private static Expression BuildCollection(IComplexProperty complexProperty, ParameterExpression value)
        => Expression.Condition(
            Expression.Property(value, IsBsonArrayProperty),
            CreateCollection(complexProperty, Expression.Property(value, AsBsonArrayProperty)),
            Expression.Throw(Expression.Call(WrongBsonTypeMethod, Expression.Constant(complexProperty), value), complexProperty.ClrType));

    private static Expression CreateCollection(IComplexProperty complexProperty, Expression array)
    {
#if EF8 || EF9
        throw new InvalidOperationException(
            $"Complex collection '{DisplayName(complexProperty)}' can't be materialized: complex collections need EF Core 10.");
#else
        var elementType = complexProperty.ComplexType.ClrType;
        var element = Expression.Parameter(typeof(BsonValue), "element");

        // An element is a subdocument or null; anything else is the wrong BSON type.
        var elementValue = Expression.Condition(
            Expression.Property(element, IsBsonNullProperty),
            Expression.Default(elementType),
            Expression.Condition(
                Expression.Property(element, IsBsonDocumentProperty),
                Construct(complexProperty.ComplexType, Expression.Property(element, AsBsonDocumentProperty)).ConvertIfRequired(elementType),
                Expression.Throw(Expression.Call(WrongBsonTypeMethod, Expression.Constant(complexProperty), element), elementType)));

        return Expression.Call(
            PopulateCollectionMethod.MakeGenericMethod(complexProperty.ClrType, elementType),
            Expression.Constant(complexProperty.GetCollectionAccessor(), typeof(IClrCollectionAccessor)),
            Expression.Constant(complexProperty),
            array,
            Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(BsonValue), elementType), elementValue, element));
#endif
    }

    /// <summary>Constructs an instance of <paramref name="complexType"/> from the subdocument <paramref name="document"/>.</summary>
    private static Expression Construct(IComplexType complexType, Expression document)
    {
        var binding = complexType.ConstructorBinding
                      ?? throw new InvalidOperationException(
                          $"Complex type '{complexType.DisplayName()}' has no constructor binding, so it can't be materialized.");

        var documentVariable = Expression.Variable(typeof(BsonDocument), "complexDocument");
        var instance = Expression.Variable(complexType.ClrType, "complexInstance");

        // The constructor expression reads its parameters from a materialization context's value buffer; redirect those
        // reads to the subdocument (the context parameter itself is then unreferenced).
        var constructor = new ConstructorParameterReadRewriter(documentVariable).Visit(
            binding.CreateConstructorExpression(
                new ParameterBindingInfo(complexType, Expression.Parameter(typeof(MaterializationContext), "unusedContext"))));

        var consumed = binding.ParameterBindings.SelectMany(p => p.ConsumedProperties).ToHashSet();
        var statements = new List<Expression>
        {
            Expression.Assign(documentVariable, document),
            Expression.Assign(instance, constructor)
        };

        foreach (var property in complexType.GetProperties())
        {
            if (!property.IsShadowProperty() && !consumed.Contains(property))
            {
                var member = property.GetMemberInfo(forMaterialization: true, forSet: true);
                statements.Add(instance.MakeMemberAccess(member).Assign(ReadLeaf(documentVariable, property, MemberType(member))));
            }
        }

        foreach (var nested in complexType.GetComplexProperties())
        {
            if (!nested.IsShadowProperty() && !consumed.Contains(nested))
            {
                var member = nested.GetMemberInfo(forMaterialization: true, forSet: true);
                statements.Add(instance.MakeMemberAccess(member).Assign(
                    Build(nested, CreateGetElement(documentVariable, nested.GetElementName())).ConvertIfRequired(MemberType(member))));
            }
        }

        statements.Add(instance);
        return Expression.Block(complexType.ClrType, [documentVariable, instance], statements);
    }

    // The entity-member read (the DOM shaper's CreateGetValueExpression ends in the same call): the property's own
    // serializer and nullability, and the same missing/null rules.
    private static Expression ReadLeaf(Expression document, IProperty property, Type type)
        => BsonBinding.CreateGetValueExpression(
                document, property.Name, !type.IsNullableType(), property.GetTypeMapping().ClrType, property.DeclaringType)
            .ConvertIfRequired(type);

    private static Type MemberType(MemberInfo member)
        => member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new InvalidOperationException($"Unexpected member '{member.Name}' of type '{member.GetType().Name}'.")
        };

    private static string DisplayName(IComplexProperty complexProperty)
        => complexProperty.DeclaringType.DisplayName() + "." + complexProperty.Name;

    private static readonly PropertyInfo IsBsonNullProperty = typeof(BsonValue).GetProperty(nameof(BsonValue.IsBsonNull))!;
    private static readonly PropertyInfo IsBsonDocumentProperty = typeof(BsonValue).GetProperty(nameof(BsonValue.IsBsonDocument))!;
    private static readonly PropertyInfo AsBsonDocumentProperty = typeof(BsonValue).GetProperty(nameof(BsonValue.AsBsonDocument))!;
    private static readonly PropertyInfo IsBsonArrayProperty = typeof(BsonValue).GetProperty(nameof(BsonValue.IsBsonArray))!;
    private static readonly PropertyInfo AsBsonArrayProperty = typeof(BsonValue).GetProperty(nameof(BsonValue.AsBsonArray))!;

    private static readonly ConstructorInfo InvalidOperationExceptionConstructor =
        typeof(InvalidOperationException).GetConstructor([typeof(string)])!;

    private static readonly MethodInfo GetElementMethod =
        typeof(ComplexTypeMaterializationBuilder).GetMethod(nameof(GetElement), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo WrongBsonTypeMethod =
        typeof(ComplexTypeMaterializationBuilder).GetMethod(nameof(WrongBsonType), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static BsonValue? GetElement(BsonDocument? document, string elementName)
        => document != null && document.TryGetValue(elementName, out var value) ? value : null;

    private static Exception WrongBsonType(IReadOnlyComplexProperty complexProperty, BsonValue value)
        => new FormatException(
            $"Cannot deserialize complex {(complexProperty.IsCollection ? "collection" : "property")} '{complexProperty.DeclaringType.DisplayName()}.{complexProperty.Name}' from BsonType '{value.BsonType}'.");

#if !EF8 && !EF9
    private static readonly MethodInfo PopulateCollectionMethod =
        typeof(ComplexTypeMaterializationBuilder).GetMethod(nameof(PopulateCollection), BindingFlags.NonPublic | BindingFlags.Static)!;

    // The collection is created by the property's own accessor (List<T>, ObservableCollection<T>, an IList<T>
    // property's List<T>), so every collection type EF accepts for a complex collection works.
    private static TCollection PopulateCollection<TCollection, TElement>(
        IClrCollectionAccessor accessor, IReadOnlyComplexProperty complexProperty, BsonArray array, Func<BsonValue, TElement> materializeElement)
    {
        if (accessor.Create() is not ICollection<TElement> collection)
        {
            throw new InvalidOperationException(
                $"Complex collection '{complexProperty.DeclaringType.DisplayName()}.{complexProperty.Name}' is typed as '{typeof(TCollection).ShortDisplayName()}', which does not implement '{typeof(ICollection<TElement>).ShortDisplayName()}'.");
        }

        foreach (var element in array)
        {
            collection.Add(materializeElement(element));
        }

        return (TCollection)collection;
    }
#endif

    /// <summary>Redirects a constructor binding's value-buffer reads to the leaf reads of a subdocument.</summary>
    private sealed class ConstructorParameterReadRewriter(Expression document) : System.Linq.Expressions.ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsGenericMethod
                && node.Method.GetGenericMethodDefinition() == Microsoft.EntityFrameworkCore.Infrastructure.ExpressionExtensions.ValueBufferTryReadValueMethod)
            {
                return ReadLeaf(document, node.Arguments[2].GetConstantValue<IProperty>(), node.Type);
            }

            return base.VisitMethodCall(node);
        }
    }

    /// <summary>See <see cref="MarkComplexPropertyAssignments"/>.</summary>
    private sealed class MarkingVisitor : System.Linq.Expressions.ExpressionVisitor
    {
        private readonly Stack<IEntityType> _entityTypes = new();
        private readonly Stack<ParameterExpression> _materializationContexts = new();

        protected override Expression VisitBlock(BlockExpression node)
        {
            // EF's entity block assigns its materialization context first, then materializes inside a type switch.
            var context = node.Expressions
                .OfType<BinaryExpression>()
                .Where(b => b.NodeType == ExpressionType.Assign && b.Left is ParameterExpression { Type: var t } && t == typeof(MaterializationContext))
                .Select(b => (ParameterExpression)b.Left)
                .FirstOrDefault();
            if (context == null)
            {
                return base.VisitBlock(node);
            }

            _materializationContexts.Push(context);
            try
            {
                return base.VisitBlock(node);
            }
            finally
            {
                _materializationContexts.Pop();
            }
        }

        protected override Expression VisitSwitch(SwitchExpression node)
        {
            // MaterializeEntity's switch over the concrete entity type: one case per concrete type, each case's body
            // that type's materializer.
            if (node.Cases.Count == 0
                || !node.Cases.All(c => c.TestValues is [ConstantExpression { Value: IEntityType }]))
            {
                return base.VisitSwitch(node);
            }

            var cases = new List<SwitchCase>(node.Cases.Count);
            foreach (var switchCase in node.Cases)
            {
                _entityTypes.Push((IEntityType)((ConstantExpression)switchCase.TestValues[0]).Value!);
                try
                {
                    cases.Add(switchCase.Update(switchCase.TestValues, Visit(switchCase.Body)));
                }
                finally
                {
                    _entityTypes.Pop();
                }
            }

            return node.Update(Visit(node.SwitchValue), cases, Visit(node.DefaultBody));
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Assign
                && node.Left is MemberExpression { Expression: ParameterExpression } member
                && _entityTypes.TryPeek(out var entityType)
                && _materializationContexts.TryPeek(out var context)
                && FindComplexProperty(entityType, member.Member) is { } complexProperty)
            {
                return member.Assign(new ComplexPropertyMaterializationExpression(complexProperty, context, member.Type));
            }

            return base.VisitBinary(node);
        }

        private static IComplexProperty? FindComplexProperty(IEntityType entityType, MemberInfo member)
        {
            foreach (var complexProperty in entityType.GetComplexProperties())
            {
                if (!complexProperty.IsShadowProperty()
                    && complexProperty.GetMemberInfo(forMaterialization: true, forSet: true) is { } candidate
                    && candidate.Name == member.Name
                    && candidate.DeclaringType == member.DeclaringType)
                {
                    return complexProperty;
                }
            }

            return null;
        }
    }

    /// <summary>See <see cref="MarkComplexPropertyAssignments"/>.</summary>
    private sealed class LeftoverComplexLeafReadFinder : System.Linq.Expressions.ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsGenericMethod
                && node.Method.GetGenericMethodDefinition() == Microsoft.EntityFrameworkCore.Infrastructure.ExpressionExtensions.ValueBufferTryReadValueMethod
                && node.Arguments[2].GetConstantValue<IProperty>() is { DeclaringType: IComplexType complexType } property)
            {
                throw new InvalidOperationException(
                    $"The complex type '{complexType.DisplayName()}' (leaf '{property.Name}') is read outside a complex-property "
                    + "assignment of its entity's materializer, which the MongoDB provider can't materialize.");
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitExtension(Expression node)
            => node is ComplexPropertyMaterializationExpression ? node : base.VisitExtension(node);
    }
}
