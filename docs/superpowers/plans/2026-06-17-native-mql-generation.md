# Native MQL Generation (Sub-project B) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Translate the captured EF query expression directly into a MongoDB aggregation pipeline (`BsonDocument[]`) for a minimal single-collection slice (filter / sort / paging), execute it via `IMongoCollection.Aggregate`, and feed results to the existing DOM shaper — behind a per-query fallback to the driver-LINQ path.

**Architecture:** A native translator walks the same `CapturedExpression` the driver-LINQ visitor consumes and emits `$match`/`$sort`/`$skip`/`$limit` stages, ignoring a trailing `Select` (full docs returned; existing shaper projects client-side). `MongoExecutableQuery` carries an optional native pipeline; `MongoShapedQueryCompilingExpressionVisitor.TranslateQuery` tries native then falls back; `MongoClientWrapper.Execute` runs the aggregate when a native pipeline is present. A `MONGODB_EF_NATIVE_QUERY` env switch (`auto`/`force`/`off`) controls fallback so the existing test suite can surface native coverage.

**Tech Stack:** C#, EF Core 10 / `net10.0` (build config `Debug EF10`), MongoDB.Driver 3.9.0. The provider builds with `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`.

---

## Conventions for every task
- Build the provider: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`.
- Tests need MongoDB. A replica-set Mongo is running at `mongodb://localhost:27017` (container `ef-bench-mongo`). Run tests with `MONGODB_URI=mongodb://localhost:27017` so they reuse it instead of spinning testcontainers.
- New code obeys `<Nullable>enable</Nullable>`. Preserve file BOMs. `#if` only where needed (this slice is EF-version-agnostic; no `#if` expected).
- The provider intentionally uses EF internals (`<NoWarn>EF1001</NoWarn>`).

---

## Task 1: Diagnostic — capture the real captured-expression shapes

The predicate/sort translators must match the actual node shapes EF produces in `MongoQueryExpression.CapturedExpression`. Capture them before writing the translator.

**Files:**
- Temporarily modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Create: `docs/superpowers/specs/2026-06-17-native-captured-expression-shapes.md`

- [ ] **Step 1: Add temporary instrumentation in `TranslateQuery`**

In `TranslateQuery<TSource>`, immediately after `var mongoQueryContext = (MongoQueryContext)queryContext;`, add:
```csharp
System.Console.Error.WriteLine("CAPTURED-EXPR >>> " + (queryExpression.CapturedExpression?.ToString() ?? "<null>"));
```

- [ ] **Step 2: Build and run the five benchmark query shapes**

```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 dotnet run -c "Release EF10" -- --smoke 2>&1 | grep "CAPTURED-EXPR"
```
The `--smoke` run exercises `Where(c=>c.Active)`, `Select(c=>new{c.Name,c.Count})`, `OrderBy(c=>c.Count).Take(10)`, `ToList()` (tracked), `AsNoTracking().ToList()`. Capture every `CAPTURED-EXPR >>>` line.

- [ ] **Step 3: Record findings**

Create `docs/superpowers/specs/2026-06-17-native-captured-expression-shapes.md` with, for each of the five shapes: the verbatim captured-expression string, and notes on (a) how a property access is represented (`EF.Property<T>(e,"Name")` call vs `e.Name` member access), (b) how the `Take(10)` count appears (literal `ConstantExpression` vs an EF query parameter), (c) whether a trailing `Select` appears in the chain or only in the shaper. These notes drive Tasks 4–5.

- [ ] **Step 4: Revert the instrumentation**

Remove the `Console.Error.WriteLine` line from `TranslateQuery`. Rebuild to confirm clean:
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```
Expected: builds; `git diff src/` is empty.

- [ ] **Step 5: Commit the findings doc**
```bash
git add docs/superpowers/specs/2026-06-17-native-captured-expression-shapes.md
git commit -m "Native MQL: capture real captured-expression shapes (diagnostic)"
```

---

## Task 2: Native query mode switch

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeQueryMode.cs`

- [ ] **Step 1: Mode enum + env reader**

`NativeQueryMode.cs`:
```csharp
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

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>How the provider chooses between native MQL generation and the driver-LINQ path.</summary>
internal enum NativeQueryMode
{
    /// <summary>Try native; silently fall back to the driver-LINQ path on anything unsupported.</summary>
    Auto,

    /// <summary>Use native or throw (no fallback) — surfaces native coverage gaps in tests.</summary>
    Force,

    /// <summary>Always use the driver-LINQ path.</summary>
    Off
}

internal static class NativeQuery
{
    /// <summary>The active mode, read once from the <c>MONGODB_EF_NATIVE_QUERY</c> environment variable.</summary>
    public static readonly NativeQueryMode Mode = Parse(Environment.GetEnvironmentVariable("MONGODB_EF_NATIVE_QUERY"));

    private static NativeQueryMode Parse(string? value)
        => value?.Trim().ToLowerInvariant() switch
        {
            "force" => NativeQueryMode.Force,
            "off" => NativeQueryMode.Off,
            _ => NativeQueryMode.Auto
        };
}
```

- [ ] **Step 2: Build**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```
Expected: succeeds.

- [ ] **Step 3: Commit**
```bash
git add src/
git commit -m "Native MQL: NativeQueryMode env switch"
```

---

## Task 3: Sentinel exception + MongoExecutableQuery native fields

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/NativeTranslationNotSupportedException.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/MongoExecutableQuery.cs`

- [ ] **Step 1: Sentinel exception**

`NativeTranslationNotSupportedException.cs`:
```csharp
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

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// Thrown by the native MQL translator when it encounters a query shape it does not yet support.
/// In <see cref="NativeQueryMode.Auto"/> this triggers fallback to the driver-LINQ path; in
/// <see cref="NativeQueryMode.Force"/> it propagates so tests surface the gap.
/// </summary>
internal sealed class NativeTranslationNotSupportedException : Exception
{
    public NativeTranslationNotSupportedException(string message) : base(message)
    {
    }
}
```

- [ ] **Step 2: Add native fields to `MongoExecutableQuery`**

In `src/MongoDB.EntityFrameworkCore/Query/MongoExecutableQuery.cs`, add `using` for `System.Collections.Generic`, `MongoDB.Bson`, and `MongoDB.Driver` if not present, then add two init-only properties inside the record body (after the existing `const` declarations):
```csharp
    /// <summary>When set, the query is executed as this native aggregation pipeline instead of via the LINQ <see cref="Provider"/>.</summary>
    public IReadOnlyList<BsonDocument>? NativePipeline { get; init; }

    /// <summary>The session for the native pipeline execution (the ambient transaction's session, if any).</summary>
    public IClientSessionHandle? Session { get; init; }
```

- [ ] **Step 3: Build**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```
Expected: succeeds (existing constructor call sites are unaffected — new members are optional init properties).

- [ ] **Step 4: Commit**
```bash
git add src/
git commit -m "Native MQL: sentinel exception + MongoExecutableQuery native pipeline/session fields"
```

---

## Task 4: Predicate translator (`Where` body → `$match`)

This is the core. Build it against the shapes recorded in Task 1. It resolves property accesses to BSON element names and comparison values to `BsonValue` via the property's serializer, resolving EF query parameters from `QueryContext`.

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoPredicateTranslator.cs`

- [ ] **Step 1: Implement the predicate translator**

`MongoPredicateTranslator.cs`:
```csharp
/* Copyright 2023-present MongoDB Inc.  (full Apache header as in sibling files) */

using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Serializers;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>Translates a <c>Where</c> predicate lambda body into a MongoDB <c>$match</c> filter document.</summary>
internal sealed class MongoPredicateTranslator
{
    private readonly IEntityType _entityType;
    private readonly QueryContext _queryContext;
    private readonly BsonSerializerFactory _serializerFactory;

    public MongoPredicateTranslator(IEntityType entityType, QueryContext queryContext, BsonSerializerFactory serializerFactory)
    {
        _entityType = entityType;
        _queryContext = queryContext;
        _serializerFactory = serializerFactory;
    }

    /// <summary>Translate a predicate body to a filter document. Throws <see cref="NativeTranslationNotSupportedException"/> on unsupported nodes.</summary>
    public BsonDocument Translate(Expression body)
        => TranslateNode(Unwrap(body));

    private static Expression Unwrap(Expression e)
        => e is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u ? Unwrap(u.Operand) : e;

    private BsonDocument TranslateNode(Expression node)
    {
        switch (node)
        {
            case BinaryExpression { NodeType: ExpressionType.AndAlso } and:
                return new BsonDocument("$and", new BsonArray { TranslateNode(Unwrap(and.Left)), TranslateNode(Unwrap(and.Right)) });

            case BinaryExpression { NodeType: ExpressionType.OrElse } or:
                return new BsonDocument("$or", new BsonArray { TranslateNode(Unwrap(or.Left)), TranslateNode(Unwrap(or.Right)) });

            case BinaryExpression be when IsComparison(be.NodeType):
                return TranslateComparison(be);

            case UnaryExpression { NodeType: ExpressionType.Not } not:
                // !boolProperty  =>  { field: false }
                if (TryResolveProperty(Unwrap(not.Operand), out var notProp, out var notElement))
                    return new BsonDocument(notElement, ToBsonValue(notProp, false));
                throw NotSupported(node);

            default:
                // bare boolean property:  c.Active  =>  { field: true }
                if (TryResolveProperty(node, out var prop, out var element) && prop.ClrType == typeof(bool))
                    return new BsonDocument(element, ToBsonValue(prop, true));
                throw NotSupported(node);
        }
    }

    private BsonDocument TranslateComparison(BinaryExpression be)
    {
        // One side is a property access, the other is a value.
        IProperty? property; string? element; Expression valueNode;
        if (TryResolveProperty(Unwrap(be.Left), out property, out element))
            valueNode = Unwrap(be.Right);
        else if (TryResolveProperty(Unwrap(be.Right), out property, out element))
            valueNode = Unwrap(be.Left);
        else
            throw NotSupported(be);

        var value = ToBsonValue(property!, EvaluateValue(valueNode));
        var op = be.NodeType switch
        {
            ExpressionType.Equal => (string?)null,
            ExpressionType.NotEqual => "$ne",
            ExpressionType.LessThan => "$lt",
            ExpressionType.LessThanOrEqual => "$lte",
            ExpressionType.GreaterThan => "$gt",
            ExpressionType.GreaterThanOrEqual => "$gte",
            _ => throw NotSupported(be)
        };
        return op is null
            ? new BsonDocument(element!, value)
            : new BsonDocument(element!, new BsonDocument(op, value));
    }

    private static bool IsComparison(ExpressionType t)
        => t is ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.LessThan
            or ExpressionType.LessThanOrEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual;

    // Recognizes EF.Property<T>(entity, "Name") calls and entity.Member access; maps to (IProperty, elementName).
    private bool TryResolveProperty(Expression node, out IProperty? property, out string? element)
    {
        property = null;
        element = null;
        string? name = node switch
        {
            MethodCallExpression mc when mc.Method.Name == "Property" && mc.Arguments.Count == 2
                && mc.Arguments[1] is ConstantExpression { Value: string s } => s,
            MemberExpression me when me.Expression is ParameterExpression => me.Member.Name,
            _ => null
        };
        if (name is null)
            return false;
        property = _entityType.FindProperty(name);
        if (property is null)
            return false;
        element = property.GetElementName();
        return true;
    }

    // Evaluate a value node: literal constant, EF query parameter, or closure capture.
    private object? EvaluateValue(Expression node)
    {
        switch (node)
        {
            case ConstantExpression c:
                return c.Value;
            // EF query parameter: a parameter whose value lives in QueryContext.ParameterValues.
            case ParameterExpression p when _queryContext.ParameterValues.TryGetValue(p.Name!, out var pv):
                return pv;
            default:
                try
                {
                    return Expression.Lambda(Expression.Convert(node, typeof(object))).Compile().DynamicInvoke();
                }
                catch
                {
                    throw NotSupported(node);
                }
        }
    }

    // Convert a CLR value to a BsonValue using the property's serializer (correct element representation).
    private BsonValue ToBsonValue(IProperty property, object? value)
    {
        var info = BsonSerializerFactory.GetPropertySerializationInfo(property);
        var doc = new BsonDocument();
        using (var writer = new BsonDocumentWriter(doc))
        {
            writer.WriteStartDocument();
            writer.WriteName("v");
            var context = BsonSerializationContext.CreateRoot(writer);
            info.Serializer.Serialize(context, value);
            writer.WriteEndDocument();
        }
        return doc["v"];
    }

    private static NativeTranslationNotSupportedException NotSupported(Expression node)
        => new($"Native predicate translation does not support: {node.NodeType} ({node}).");
}
```
NOTE: if Task 1 showed property access or parameter values in a different form than handled above (e.g. a `QueryParameterExpression` type rather than `ParameterExpression`, or `EF.Property` not used), adjust `TryResolveProperty`/`EvaluateValue` to match the observed shapes — keep the throw-on-unknown behavior so unsupported forms fall back. Verify `BsonSerializerFactory.GetPropertySerializationInfo` is the correct accessor name (it is used by `Storage/MongoUpdate.cs`); confirm `QueryContext.ParameterValues` exists in EF10 (it does — `IReadOnlyDictionary<string, object?>`).

- [ ] **Step 2: Build**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```
Expected: succeeds. (Behavior is exercised in Task 6 via the test suite in `force` mode; pure-unit testing is impractical because inputs are EF-internal expression trees — the suite is the validation per the spec.)

- [ ] **Step 3: Commit**
```bash
git add src/
git commit -m "Native MQL: predicate translator (Where body -> \$match)"
```

---

## Task 5: Pipeline translator (chain → stages)

**Files:**
- Create: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoPipelineTranslator.cs`

- [ ] **Step 1: Implement the chain translator**

`MongoPipelineTranslator.cs`:
```csharp
/* Copyright 2023-present MongoDB Inc.  (full Apache header as in sibling files) */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Serializers;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// Walks the captured EF query method chain (single collection) and produces an aggregation
/// pipeline of <c>$match</c>/<c>$sort</c>/<c>$skip</c>/<c>$limit</c> stages. A trailing pure
/// <c>Select</c> is ignored (full documents returned; the existing shaper projects client-side).
/// Throws <see cref="NativeTranslationNotSupportedException"/> on anything else.
/// </summary>
internal sealed class MongoPipelineTranslator
{
    private readonly IEntityType _entityType;
    private readonly QueryContext _queryContext;
    private readonly BsonSerializerFactory _serializerFactory;
    private readonly MongoPredicateTranslator _predicates;

    public MongoPipelineTranslator(IEntityType entityType, QueryContext queryContext, BsonSerializerFactory serializerFactory)
    {
        _entityType = entityType;
        _queryContext = queryContext;
        _serializerFactory = serializerFactory;
        _predicates = new MongoPredicateTranslator(entityType, queryContext, serializerFactory);
    }

    public IReadOnlyList<BsonDocument> Translate(Expression? capturedExpression)
    {
        // Collect the method chain from outermost to the root source.
        var calls = new List<MethodCallExpression>();
        var node = capturedExpression;
        while (node is MethodCallExpression call)
        {
            calls.Add(call);
            node = call.Arguments.Count > 0 ? call.Arguments[0] : null;
        }
        // node is now the root (a constant queryable / collection source). Anything else => unsupported.
        calls.Reverse(); // root-first order

        BsonDocument? match = null;
        BsonDocument? sort = null;
        int? skip = null, limit = null;

        foreach (var call in calls)
        {
            switch (call.Method.Name)
            {
                case "Where":
                    var pred = UnquoteLambda(call.Arguments[1]);
                    var filter = _predicates.Translate(pred.Body);
                    match = match is null ? filter : new BsonDocument("$and", new BsonArray { match, filter });
                    break;

                case "OrderBy" or "ThenBy":
                    AddSort(ref sort, call, ascending: true);
                    break;
                case "OrderByDescending" or "ThenByDescending":
                    AddSort(ref sort, call, ascending: false);
                    break;

                case "Skip":
                    skip = Convert.ToInt32(EvaluateCount(call.Arguments[1]));
                    break;
                case "Take":
                    limit = Convert.ToInt32(EvaluateCount(call.Arguments[1]));
                    break;

                case "Select":
                    // Pure projection: ignored — full docs returned, shaper projects client-side.
                    break;

                default:
                    throw new NativeTranslationNotSupportedException($"Native pipeline does not support operator '{call.Method.Name}'.");
            }
        }

        var pipeline = new List<BsonDocument>();
        if (match is not null) pipeline.Add(new BsonDocument("$match", match));
        if (sort is not null) pipeline.Add(new BsonDocument("$sort", sort));
        if (skip is { } s) pipeline.Add(new BsonDocument("$skip", s));
        if (limit is { } l) pipeline.Add(new BsonDocument("$limit", l));
        return pipeline;
    }

    private void AddSort(ref BsonDocument? sort, MethodCallExpression call, bool ascending)
    {
        var key = UnquoteLambda(call.Arguments[1]);
        var body = key.Body is UnaryExpression { NodeType: ExpressionType.Convert } u ? u.Operand : key.Body;
        var name = body switch
        {
            MethodCallExpression mc when mc.Method.Name == "Property" && mc.Arguments.Count == 2
                && mc.Arguments[1] is ConstantExpression { Value: string s } => s,
            MemberExpression me when me.Expression is ParameterExpression => me.Member.Name,
            _ => throw new NativeTranslationNotSupportedException($"Unsupported sort key: {body}.")
        };
        var property = _entityType.FindProperty(name)
            ?? throw new NativeTranslationNotSupportedException($"Sort key '{name}' is not a mapped property.");
        sort ??= new BsonDocument();
        sort.Add(property.GetElementName(), ascending ? 1 : -1);
    }

    private object EvaluateCount(Expression node)
        => node switch
        {
            ConstantExpression c => c.Value!,
            ParameterExpression p when _queryContext.ParameterValues.TryGetValue(p.Name!, out var v) => v!,
            _ => Expression.Lambda(Expression.Convert(node, typeof(object))).Compile().DynamicInvoke()!
        };

    private static LambdaExpression UnquoteLambda(Expression e)
        => e is UnaryExpression { NodeType: ExpressionType.Quote } q ? (LambdaExpression)q.Operand : (LambdaExpression)e;
}
```
NOTE: Task 1's findings govern the exact `Where`/`OrderBy` argument forms (e.g. whether selectors are `Quote`d). Adjust `UnquoteLambda` / argument indices to match; keep throwing on anything unrecognized so it falls back.

- [ ] **Step 2: Build**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```
Expected: succeeds.

- [ ] **Step 3: Commit**
```bash
git add src/
git commit -m "Native MQL: pipeline translator (chain -> stages)"
```

---

## Task 6: Wire native path into translation + execution

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` (the `TranslateQuery<TSource>` method)
- Modify: `src/MongoDB.EntityFrameworkCore/Storage/MongoClientWrapper.cs` (the `Execute<T>` method)

- [ ] **Step 1: Branch in `TranslateQuery`**

In `TranslateQuery<TSource>`, after `source` is built and the `transaction` local is available (the method already computes `var transaction = ...CurrentTransaction as MongoTransaction;`), and BEFORE constructing the driver-LINQ `MongoEFToLinqTranslatingExpressionVisitor`, insert:
```csharp
if (NativeQuery.Mode != NativeQueryMode.Off
    && resultCardinality == ResultCardinality.Enumerable
    && queryExpression.GetPendingLookups().Count == 0)
{
    try
    {
        var pipeline = new MongoPipelineTranslator(
                (IEntityType)entityType, queryContext, bsonSerializerFactory)
            .Translate(queryExpression.CapturedExpression);

        var nativeExecutable = new MongoExecutableQuery(
            Expression.Empty(),
            resultCardinality,
            (IMongoQueryProvider)source.Provider,
            collection.CollectionNamespace,
            new(new Dictionary<string, object>()))
        {
            NativePipeline = pipeline,
            Session = transaction?.Session
        };
        return (mongoQueryContext, nativeExecutable);
    }
    catch (NativeTranslationNotSupportedException) when (NativeQuery.Mode != NativeQueryMode.Force)
    {
        // fall through to the driver-LINQ path
    }
}
```
Add `using`s as needed: `MongoDB.EntityFrameworkCore.Query.NativeTranslation;`, `Microsoft.EntityFrameworkCore.Metadata;`, `System.Collections.Generic;`. (Confirm `entityType` is castable to `IEntityType` here; if the parameter is already `IEntityType`, drop the cast. `GetPendingLookups()` returns a collection with `.Count` — confirm member from `MongoQueryExpression.Lookup.cs`.)

- [ ] **Step 2: Branch in `MongoClientWrapper.Execute<T>`**

Replace the body of `Execute<T>` with:
```csharp
public IEnumerable<T> Execute<T>(MongoExecutableQuery executableQuery, out Action log)
{
    log = () => { };

    if (executableQuery.Cardinality != ResultCardinality.Enumerable)
        return ExecuteScalar<T>(executableQuery);

    if (executableQuery.NativePipeline is { } stages)
    {
        var collection = Database.GetCollection<BsonDocument>(executableQuery.CollectionNamespace.CollectionName);
        PipelineDefinition<BsonDocument, BsonDocument> pipeline = stages.ToArray();
        var cursor = executableQuery.Session is { } session
            ? collection.Aggregate(session, pipeline)
            : collection.Aggregate(pipeline);
        log = () => _commandLogger.ExecutedMqlQuery(executableQuery);
        return (IEnumerable<T>)cursor.ToEnumerable();
    }

    var queryable = executableQuery.Provider.CreateQuery<T>(executableQuery.Query);
    log = () => _commandLogger.ExecutedMqlQuery(executableQuery);
    return queryable;
}
```
Add `using`s as needed: `System.Linq;` (for `ToArray`/`ToEnumerable`), `MongoDB.Bson;`, `MongoDB.Driver;`. (`cursor.ToEnumerable()` is `IMongoCursor`/`IAsyncCursorSourceExtensions` — confirm `Aggregate(...)` returns `IAsyncCursor<BsonDocument>` and use `.ToEnumerable()` from `MongoDB.Driver`. `T` is `BsonDocument` at runtime in the shaped-query path, so the cast is safe.)

- [ ] **Step 3: Build**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
```
Expected: succeeds.

- [ ] **Step 4: Smoke the native path end-to-end (`force` mode)**
```bash
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 MONGODB_EF_NATIVE_QUERY=force dotnet run -c "Release EF10" -- --smoke
```
Expected: prints `SMOKE OK: where=50, proj=100, ordered=10, tracked=100, noTrack=100, withCity=100`. In `force` mode every benchmark shape must go native (no fallback) and still return correct results. If it throws `NativeTranslationNotSupportedException`, the message names the unsupported node — fix the relevant translator (Task 4/5) to handle that real shape, rebuild, re-run. If results are wrong (e.g. `where` count off), debug the predicate/sort translation; do NOT weaken the smoke assertions.

- [ ] **Step 5: Commit**
```bash
git add src/
git commit -m "Native MQL: wire native translation + aggregate execution with fallback"
```

---

## Task 7: Validate against the existing suite + benchmark

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-17-native-B.md`

- [ ] **Step 1: No regressions in `auto` mode**

Run the functional query tests (fallback must cover everything not yet native):
```bash
MONGODB_URI=mongodb://localhost:27017 dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~Query"
```
Expected: all pass (same as before this sub-project — `auto` falls back on anything unsupported). If any FAIL that passed before, the native path produced a wrong result for a shape it accepted — fix the translator (tighten to throw, or correct the translation). Do not disable tests.

- [ ] **Step 2: Measure native coverage in `force` mode**

```bash
MONGODB_URI=mongodb://localhost:27017 MONGODB_EF_NATIVE_QUERY=force dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --filter "FullyQualifiedName~Query" 2>&1 | tee /tmp/force-run.txt
```
Many tests will fail with `NativeTranslationNotSupportedException` (joins, grouping, projection-heavy, scalar) — that is expected and is the coverage signal, not a regression. From the output, record: how many pass natively, and the distinct unsupported reasons. Do NOT try to make everything pass — B's slice is filter/sort/paging only.

- [ ] **Step 3: Re-run the benchmark (native `auto`) vs baseline**

```bash
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 dotnet run -c "Release EF10"
```
(10-min Bash timeout.) Capture the results table.

- [ ] **Step 4: Record results**

Create `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-17-native-B.md`: paste the benchmark table; compare each shape's Mean/Allocated to `results/2026-06-16-baseline.md`; and summarize the `force`-mode coverage (X/Y query tests pass natively; list the top unsupported reasons). Note that B keeps the DOM shaper so allocation gains are modest (the materialization win is C); the expected B effect is on translation overhead and correct server-side filter/sort/paging.

- [ ] **Step 5: Commit**
```bash
git add benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-17-native-B.md
git commit -m "Native MQL: record B coverage + benchmark vs baseline"
```

---

## Notes for the executor

- **Task 1 governs Tasks 4–5.** If the real captured-expression shapes differ from the `EF.Property`/`MemberExpression` forms the translators assume, adapt the node-matching to reality — but always keep "throw `NativeTranslationNotSupportedException` on anything unrecognized" so `auto` mode falls back safely.
- **`force` mode is the development driver.** Build the translator by running the smoke (Task 6) and the query test subset (Task 7) in `force` mode and handling the real shapes that appear, one unsupported-node message at a time.
- **Never weaken a test or assertion to get green.** A test that passed before B must still pass in `auto` mode; if it doesn't, the native path mistranslated a shape it should have rejected.
- **No `$project`, no scalar, no joins** in B — those must hit fallback (or throw in `force`). That's intended.
- Leave the `ef-bench-mongo` container running; it's the test/bench MongoDB.
