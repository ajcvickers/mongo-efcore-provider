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

namespace MongoDB.EntityFrameworkCore.Query;

/// <summary>
/// The user-visible wording of every complex-type refusal (complex properties, complex types and complex collections).
/// Call sites choose the exception type and throw; the text lives here so it is reviewed, and kept consistent, in one place.
/// Tests assert on these strings, so changing a word is a behavior change.
/// </summary>
internal static class ComplexTypeRefusals
{
    // ---- Model validation ----

    internal static string ConcurrencyTokenInComplexType(string member)
        => member +
           " is configured as a concurrency token or row version, but it belongs to a complex type." +
           " Concurrency tokens are only supported on the document-root entity, where they" +
           " guard the whole document including its complex properties.";

    internal static string ComplexCollectionClrTypeNotMaterializable(string member, string clrTypeName)
        => member + $" is a complex collection of CLR type '{clrTypeName}', which"
           + " EF Core cannot materialize or save: collection types without a public parameterless"
           + " constructor are not supported. Declare it as List<T>, IList<T>, an array, or another mutable"
           + " collection type with a public parameterless constructor.";

    internal static string EncryptionInComplexType(string member, bool memberIsComplexProperty, string complexTypeName)
        => member
           + (memberIsComplexProperty ? $" (complex type '{complexTypeName}')" : "")
           + " is configured for encryption."
           + " Encryption (Queryable Encryption or client-side field level encryption) is not supported for complex"
           + " properties or for properties inside complex types; map the value as a property of an entity type"
           + " or an owned entity type to encrypt it.";

    internal static string EmptyComplexElementName(string member)
        => member
           + " may not map to an empty or whitespace element name. A complex property is always stored; map it"
           + " to a non-empty BSON element name.";

    // ---- Writes (SaveChanges / bulk) ----

    internal static string BulkSetRequiredComplexToNull(string declaringTypeName, string propertyName)
        => $"ExecuteUpdate cannot set the required complex property '{declaringTypeName}."
           + $"{propertyName}' to null. No document was modified.";

    internal static string BulkSetComplexRequiredMemberNull(string path, string member)
        => $"ExecuteUpdate cannot set the complex property '{path}' because its required member "
           + $"'{member}' is null: the stored document would no longer be readable. "
           + "No document was modified.";

    internal static string BulkSetComplexRequiredComplexMemberNull(string path, string member)
        => $"ExecuteUpdate cannot set the complex property '{path}' because its required complex member "
           + $"'{member}' is null: the stored document would no longer be readable. "
           + "No document was modified.";

    internal static string BulkSetComplexShadowProperty(string complexTypeName, string propertyName)
        => $"The complex type '{complexTypeName}' has the shadow property '{propertyName}', which a value "
           + "outside the change tracker does not carry, so it cannot be written by ExecuteUpdate.";

    internal static string BulkSetterTargetNotMapped(string setterTarget, string entityTypeName)
        => "Only mapped root scalar properties can be updated by a bulk update, and, through complex properties, their scalar "
           + $"properties and whole complex properties. The setter target '{setterTarget}' is not a mapped scalar "
           + $"property of '{entityTypeName}'.";

    internal static string BulkSetterPathsOverlap(string earlierPath, string path, string entityTypeName)
        => $"ExecuteUpdate cannot set both '{earlierPath}' and '{path}' of '{entityTypeName}': "
           + "one is stored inside the other, and MongoDB rejects an update that sets both. Set either the whole value "
           + "or its members, not both.";

    internal static string BulkSetUnderOptionalComplexHop(string setterTarget, string hopName)
        => $"ExecuteUpdate cannot set '{setterTarget}' because '{hopName}' is an "
           + "optional complex property, which may be null in a stored document: the server cannot set a member of a null "
           + "value. Set the whole optional complex property instead (SetProperty(e => e.Optional, value)).";

    internal static string BulkSelfReferencingBsonRepresentationLeaf(string propertyName)
        => $"Self-referencing ExecuteUpdate on property '{propertyName}' is not supported because it is stored with a "
           + "BsonRepresentation, which the computed value would not be written in.";

    internal static string BulkSelfReferencingComplexValue(string complexPropertyName)
        => $"ExecuteUpdate cannot set the complex property '{complexPropertyName}' "
           + "from a value that reads the entity being updated. Set its scalar properties individually "
           + "(SetProperty(e => e.Complex.Member, e => ...)), or set it to a value computed on the client.";

    // ---- Reads / materialization ----

    internal static string RequiredComplexElementMissing(string elementName, bool isNull, string complexPropertyName)
        => $"Document element '{elementName}' is {(isNull ? "null" : "missing")} for required complex property '{complexPropertyName}'.";

    internal static string ComplexCollectionNeedsEF10(string complexPropertyName)
        => $"Complex collection '{complexPropertyName}' can't be materialized: complex collections need EF Core 10.";

    internal static string ComplexTypeHasNoConstructorBinding(string complexTypeName)
        => $"Complex type '{complexTypeName}' has no constructor binding, so it can't be materialized.";

    internal static string WrongBsonType(bool isCollection, string complexPropertyName, string bsonType)
        => $"Cannot deserialize complex {(isCollection ? "collection" : "property")} '{complexPropertyName}' from BsonType '{bsonType}'.";

    internal static string ComplexCollectionNotICollection(string complexPropertyName, string collectionTypeName, string iCollectionTypeName)
        => $"Complex collection '{complexPropertyName}' is typed as '{collectionTypeName}', which does not implement '{iCollectionTypeName}'.";

    // ---- Query refusals ----

    // Whole-value operand and GroupBy (one message, shared by every refusal site).
    internal static string WholeValueOperand(string operatorName)
        => $"A projected whole complex value cannot be the operand of '{operatorName}': the MongoDB provider "
           + "can't compare or combine complex values. Project the complex type's properties you need instead, or apply "
           + "the operator after 'AsEnumerable()'.";

    internal static string ComplexTypeOfTypeNotSupported(string complexTypeName)
        => $"Complex type '{complexTypeName}' not supported in MongoDB.";

    // Null-element refusal scanner (query fallback).
    internal static string CollectionComparedWithNullOnDriverLinq(string collection)
        => $"The query is not natively translatable and would run on driver-LINQ, which evaluates a comparison of the "
           + $"complex collection '{collection}' with null incorrectly: its null filter also matches an array that "
           + "contains a null element (and a missing or null array of a required collection, which reads as empty), so the "
           + "fallback would return wrong rows. To test for a null or empty collection use '!c.Collection.Any()' or "
           + "'c.Collection.Count == 0', which are translated natively; to tell null from empty, project the collection "
           + "and test it on the client, or use MongoQueryMode.DriverLinq to opt in to the driver-LINQ execution of this "
           + "query.";

    internal static string NullElementOnDriverLinq(string shape, string collection)
        => $"The query is not natively translatable and would run on driver-LINQ, which evaluates {shape} over an "
           + $"element of the complex collection '{collection}' incorrectly when the element is null (the members of "
           + "a null element are missing, which the server orders below every value and does not equate with null), so "
           + "the fallback would return wrong rows. Rewrite the element predicate to a natively translatable form, "
           + "project the values you need and evaluate the condition on the client, or use MongoQueryMode.DriverLinq to "
           + "opt in to the driver-LINQ execution of this query.";

    // Bulk allow-list.
    internal static string BulkMisreadsNullElements(string shape, string collection)
        => $"ExecuteUpdate and ExecuteDelete run their filter and SetProperty values on driver-LINQ, which evaluates {shape} "
           + $"over an element of the complex collection '{collection}' differently from a query when the element is null "
           + "(the members of a null element are missing, which the server orders below every value and does not equate with "
           + "null) or when the collection is null, so the operation could update or delete rows the same query does not "
           + "return. No document was modified. Inside a complex collection's element predicate a bulk operation supports "
           + "'==' and '!=' between a member and a non-null value, a bool member and its negation, a non-nullable local "
           + "list's Contains of a member, and '&&', '||' and '!' over those; select the keys with a query and update or "
           + "delete by key, or use MongoQueryMode.DriverLinq to opt in to the driver-LINQ evaluation.";

    // The "shape" fragments the bulk allow-list scan reports (spliced into BulkMisreadsNullElements).
    internal static string BulkUnboundElementLambdaShape(string printedLambda)
        => $"a lambda over complex collection elements ('{printedLambda}') that the bulk check cannot bind to its collection";

    internal static string BulkOperatorOverOptionalCollectionShape(string methodName)
        => $"'{methodName}' over an optional complex collection";

    internal static string BulkOperatorOverNonListCollectionShape(string methodName)
        => $"'{methodName}' over a complex collection whose CLR type cannot hold a List<T> (declare it as List<T> "
           + "or IList<T> so a null or missing array reads as empty)";

    internal static string BulkOperatorNotAllowedShape(string methodName)
        => $"the operator '{methodName}'";

    // ---- Serializers ----

    internal static string DriverCannotReadComplexType(string complexTypeName)
        => $"Reading complex type '{complexTypeName}' through the driver's serializer is not supported: a complex value is "
           + "materialized by the provider's shaper. Project the complex type's properties you need instead, or apply the operation "
           + "after 'AsEnumerable()'.";

    internal static string WholeValueComparisonNotSupported(string complexTypeName)
        => $"Comparing complex type '{complexTypeName}' as a whole value is not supported. Compare its properties instead.";

    internal static string UnsupportedComplexCollectionType(string typeName)
        => $"Unsupported complex collection type '{typeName}'.";
}
