# Injected-materializer block shape (EF10) — diagnostic capture

Captured 2026-06-18 on branch `spike/low-level-provider` by instrumenting
`MongoShapedQueryCompilingExpressionVisitor.CompileShapedQuery` to print the
post-`InjectStructuralTypeMaterializers` `shaperBody` (via
`new ExpressionPrinter().PrintExpression(shaperBody)`) **before** the binding
remover (`MongoProjectionBindingRemovingExpressionVisitor`) runs.

Driver: benchmark `Customer` entity. Note the actual benchmark `Customer` in this
branch has **`ObjectId Id`, 11 scalars (`Active`, `Big`, `Count`, `Created`,
`Description`, `Kind`, `Name`, `Price`, `Quantity`, `Rate`, `Ref`), and an owned
`Address` sub-document** — there is **no `string[] Tags`** property on the current
entity. See note (d) below for what that means for the rewriter.

The `--smoke` harness runs five queries; the whole-entity ones
(`Where(c=>c.Active)`, `OrderBy(c=>c.Count).Take(10)`, `ToList()` tracked) all
emit the **identical** tracked materializer block below. `AsNoTracking().ToList()`
emits a structurally similar block with the tracking machinery removed (diff in
note (e)). Smoke result: `SMOKE OK: where=50, proj=100, ordered=10, tracked=100,
noTrack=100, withCity=100`.

This is the tree **as EF emits it** (ValueBuffer-based). The binding remover that
runs immediately after rewrites every `ValueBufferTryReadValue(...)` into a
`BsonDocument` read, and rewrites the `new MaterializationContext(...)` into one
taking `ValueBuffer.Empty`. The streaming rewriter we are going to build is an
alternative to that binding remover (or a post-pass over its output) — it must
match the same `ValueBufferTryReadValue` calls and the same `MaterializationContext`
construction.

---

## Captured tracked Customer materializer block (verbatim, EF10)

```
Include(
    Entity:
    {
        BsonDocument bsonDoc1;
        bsonDoc1 = (ProjectionBindingExpression: 0 as BsonDocument);
        return bsonDoc1 == null ? null :
        {
            MaterializationContext materializationContext1;
            IEntityType entityType1;
            Customer instance1;
            InternalEntityEntry entry1;
            bool hasNullKey1;
            materializationContext1 = new MaterializationContext(
                ProjectionBindingExpression: 0,
                queryContext.Context
            );
            instance1 = default(Customer);
            entry1 = queryContext.TryGetEntry(
                key: Key: Customer.Id PK,
                keyValues: new object[]{ ExpressionExtensions.ValueBufferTryReadValue<object>(
                    valueBuffer: materializationContext1.get_ValueBuffer(),
                    index: 0,
                    property: Property: Customer.Id (ObjectId) Required PK AfterSave:Throw ValueGenerated.OnAdd) },
                throwOnNullKey: True,
                hasNullKey: hasNullKey1);
            !(hasNullKey1) ? entry1 != default(InternalEntityEntry) ?
            {
                entityType1 = entry1.EntityType;
                return instance1 = (Customer)entry1.Entity;
            } :
            {
                ISnapshot shadowSnapshot1;
                shadowSnapshot1 = Snapshot;
                entityType1 = EntityType: Customer;
                instance1 = switch (entityType1)
                {
                    case EntityType: Customer:
                        {
                            return
                            {
                                Customer instance;
                                instance = new Customer();
                                instance.<Id>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<ObjectId>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 0,
                                    property: Property: Customer.Id (ObjectId) Required PK AfterSave:Throw ValueGenerated.OnAdd);
                                instance.<Active>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<bool>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 1,
                                    property: Property: Customer.Active (bool) Required);
                                instance.<Big>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<long>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 2,
                                    property: Property: Customer.Big (long) Required);
                                instance.<Count>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<int>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 3,
                                    property: Property: Customer.Count (int) Required);
                                instance.<Created>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<DateTime>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 4,
                                    property: Property: Customer.Created (DateTime) Required);
                                instance.<Description>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<string>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 5,
                                    property: Property: Customer.Description (string) Required);
                                instance.<Kind>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<CustomerKind>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 6,
                                    property: Property: Customer.Kind (CustomerKind) Required);
                                instance.<Name>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<string>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 7,
                                    property: Property: Customer.Name (string) Required);
                                instance.<Price>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<decimal>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 8,
                                    property: Property: Customer.Price (decimal) Required);
                                instance.<Quantity>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<int>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 9,
                                    property: Property: Customer.Quantity (int) Required);
                                instance.<Rate>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<double>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 10,
                                    property: Property: Customer.Rate (double) Required);
                                instance.<Ref>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<Guid>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 11,
                                    property: Property: Customer.Ref (Guid) Required);
                                (instance is IInjectableService) ? ((IInjectableService)instance).Injected(
                                    context: materializationContext1.Context,
                                    entity: instance,
                                    queryTrackingBehavior: TrackAll,
                                    structuralType: EntityType: Customer) : default(void);
                                return instance;
                            }}
                    default:
                        default(Customer)
                }
                ;
                entry1 = entityType1 == default(IEntityType) ? default(InternalEntityEntry) : queryContext.StartTracking(
                    entityType: entityType1,
                    entity: instance1,
                    snapshot: shadowSnapshot1);
                return instance1;
            } : default(void);
            return instance1;
        };
    },
    Navigation: Address,
    {
        BsonDocument bsonDoc2;
        bsonDoc2 = ((ValueBuffer)(object)bsonDoc["Address"] as BsonDocument);
        return bsonDoc2 == null ? null :
        {
            MaterializationContext materializationContext2;
            IEntityType entityType2;
            Address instance2;
            InternalEntityEntry entry2;
            bool hasNullKey2;
            materializationContext2 = new MaterializationContext(
                (ValueBuffer)(object)bsonDoc["Address"],
                queryContext.Context
            );
            instance2 = default(Address);
            entry2 = queryContext.TryGetEntry(
                key: Key: Address.CustomerId PK,
                keyValues: new object[]{ ExpressionExtensions.ValueBufferTryReadValue<object>(
                    valueBuffer: materializationContext2.get_ValueBuffer(),
                    index: 0,
                    property: Property: Address.CustomerId (no field, ObjectId) Shadow Required PK FK AfterSave:Throw ValueGenerated.OnAdd) },
                throwOnNullKey: False,
                hasNullKey: hasNullKey2);
            !(hasNullKey2) ? entry2 != default(InternalEntityEntry) ?
            {
                entityType2 = entry2.EntityType;
                return instance2 = (Address)entry2.Entity;
            } :
            {
                ISnapshot shadowSnapshot2;
                shadowSnapshot2 = Snapshot;
                entityType2 = EntityType: Address Owned;
                instance2 = switch (entityType2)
                {
                    case EntityType: Address Owned:
                        {
                            shadowSnapshot2 = (ISnapshot)new Snapshot<ObjectId>(ExpressionExtensions.ValueBufferTryReadValue<ObjectId>(
                                valueBuffer: materializationContext2.get_ValueBuffer(),
                                index: 0,
                                property: Property: Address.CustomerId (no field, ObjectId) Shadow Required PK FK AfterSave:Throw ValueGenerated.OnAdd));
                            return
                            {
                                Address instance;
                                instance = new Address();
                                instance.<City>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<string>(
                                    valueBuffer: materializationContext2.get_ValueBuffer(),
                                    index: 1,
                                    property: Property: Address.City (string) Required);
                                instance.<Street>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<string>(
                                    valueBuffer: materializationContext2.get_ValueBuffer(),
                                    index: 2,
                                    property: Property: Address.Street (string) Required);
                                instance.<Zip>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<int>(
                                    valueBuffer: materializationContext2.get_ValueBuffer(),
                                    index: 3,
                                    property: Property: Address.Zip (int) Required);
                                (instance is IInjectableService) ? ((IInjectableService)instance).Injected(
                                    context: materializationContext2.Context,
                                    entity: instance,
                                    queryTrackingBehavior: TrackAll,
                                    structuralType: EntityType: Address Owned) : default(void);
                                return instance;
                            }}
                    default:
                        default(Address)
                }
                ;
                entry2 = entityType2 == default(IEntityType) ? default(InternalEntityEntry) : queryContext.StartTracking(
                    entityType: entityType2,
                    entity: instance2,
                    snapshot: shadowSnapshot2);
                return instance2;
            } : default(void);
            return instance2;
        };
    }
)
```

### Structural anatomy (for the rewriter)

- Outermost node is an EF `IncludeExpression` (`Include(Entity: ..., Navigation: Address, <nav-shaper>)`).
  The owned `Address` rides as the navigation shaper.
- **Entity (Customer) shaper** is a `Block`:
  - `BsonDocument bsonDoc1; bsonDoc1 = (ProjectionBindingExpression: 0 as BsonDocument);` — at this
    pre-binding-remover stage the source is still a `ProjectionBindingExpression`; the binding remover
    later turns `bsonDoc1 = ...` into a read off the `bsonDoc` lambda parameter (root document).
  - `return bsonDoc1 == null ? null : <inner-block>` — null guard.
  - Inner block declares `materializationContext1`, `entityType1`, `Customer instance1`,
    `InternalEntityEntry entry1`, `bool hasNullKey1`.
  - `materializationContext1 = new MaterializationContext(ProjectionBindingExpression: 0, queryContext.Context)`.
  - `entry1 = queryContext.TryGetEntry(key, keyValues: new object[]{ <PK read> }, throwOnNullKey: True, hasNullKey: hasNullKey1)`.
  - Identity-map branch: `entry1 != default ? { entityType1 = entry1.EntityType; return instance1 = (Customer)entry1.Entity; }`.
  - Materialize branch: `switch (entityType1) { case Customer: { instance = new Customer(); <11+1 backing-field assignments>; IInjectableService hook; return instance; } default: default(Customer) }`.
  - `entry1 = ... queryContext.StartTracking(entityType1, instance1, shadowSnapshot1)`.

---

## (a) Exact form of a `ValueBufferTryReadValue` call & where the `IProperty` lives

Each scalar read is:

```
ExpressionExtensions.ValueBufferTryReadValue<TClr>(
    valueBuffer: materializationContext1.get_ValueBuffer(),   // arg[0]
    index:       <int>,                                        // arg[1]
    property:    <ConstantExpression wrapping the IProperty>)  // arg[2]
```

- It is a generic `MethodCallExpression`; the binding remover matches it by
  `method.GetGenericMethodDefinition() == ExpressionExtensions.ValueBufferTryReadValueMethod`
  (see `MongoProjectionBindingRemovingExpressionVisitor.VisitMethodCall`, lines 384–402).
- The `IProperty` is **`Arguments[2]`**, a `ConstantExpression`, extracted via
  `methodCallExpression.Arguments[2].GetConstantValue<IProperty>()` (line 386).
- `Arguments[0]` is `materializationContext1.get_ValueBuffer()` — a `MethodCallExpression`
  whose `.Object` is the `MaterializationContext` `ParameterExpression`. The binding remover
  uses that parameter to look up the bound source document in `_materializationContextBindings`
  (lines 396–399). (In the non-tracked Customer-PK case the arg[0] form is the same; in the
  aliased-projection case `Arguments[0]` is instead a `ProjectionBindingExpression`.)
- `Arguments[1]` (the `index`) is EF's value-buffer ordinal; the Mongo binding remover **ignores
  it** and reads by `property.Name` / element name instead. **The streaming rewriter must do the
  same** — map by `IProperty` (from arg[2]), not by the value-buffer index.

**Rewriter takeaway:** to replace each read with a typed local, match the same way the binding
remover does — `genericMethodDef == ValueBufferTryReadValueMethod`, pull `IProperty` from
`Arguments[2]` via `GetConstantValue<IProperty>()`, and key the local off that `IProperty`. The
`<TClr>` generic arg of the call is the target local type (e.g. `ObjectId`, `bool`, `long`,
`string`, `CustomerKind`, `decimal`, `Guid`).

## (b) Constructor invocation & `new MaterializationContext(...)`

- The CLR instance is built by `instance = new Customer();` (a parameterless `NewExpression`)
  followed by **individual backing-field assignments** (`instance.<Name>k__BackingField = <read>;`),
  not by a constructor that takes the property values. (Customer has only the implicit ctor.)
- `MaterializationContext` is constructed once per entity:
  `materializationContext1 = new MaterializationContext(ProjectionBindingExpression: 0, queryContext.Context)`.
  At this pre-binding-remover stage arg0 is still the `ProjectionBindingExpression`. The Mongo
  binding remover (`VisitBinary`, lines 337–360) rewrites this assignment so arg0 becomes
  `Expression.Constant(ValueBuffer.Empty)` — i.e. the materialized output is
  `new MaterializationContext(ValueBuffer.Empty, queryContext.Context)`. It also records the
  binding `_materializationContextBindings[mc-param] = entityProjectionExpression.ParentAccessExpression`
  so subsequent `ValueBufferTryReadValue(mc.get_ValueBuffer(), ...)` reads know which document to
  read from. **The streaming rewriter must preserve / replicate this `MaterializationContext`
  rewrite** (the EF tracking machinery — `TryGetEntry`, `StartTracking`, snapshots, the
  `IInjectableService` hook — all dereference `materializationContext.Context`, and EF requires the
  buffer to be `ValueBuffer.Empty` once the reads are removed).

## (c) How the owned `Address` appears

- As the **navigation shaper of the outer `IncludeExpression`** (`Navigation: Address`), a full
  second `Block` mirroring the Customer block (its own `materializationContext2`, `instance2`,
  `entry2`, `hasNullKey2`, `TryGetEntry`/`StartTracking`).
- It is **NOT** read via a separate driver-injected `BsonDocument` parameter from
  `BsonDocumentInjectingExpressionVisitor` at this stage. Instead the source is read **inline from
  the root `bsonDoc` by element name**:
  `bsonDoc2 = ((ValueBuffer)(object)bsonDoc["Address"] as BsonDocument);`
  and
  `materializationContext2 = new MaterializationContext((ValueBuffer)(object)bsonDoc["Address"], queryContext.Context)`.
  **Element name = `"Address"`** (literal `bsonDoc["Address"]`). So the owned sub-document is reached
  by indexing the parent document with the owned navigation's element name.
- The Address block has its own null guard (`bsonDoc2 == null ? null : ...`).
- The owned PK is a **shadow FK**: `Property: Address.CustomerId (no field, ObjectId) Shadow Required
  PK FK ...`, read at index 0 and also used to build the snapshot (note e). Address scalars
  (`City`/`Street`/`Zip`) are read at indices 1–3.

**Rewriter takeaway:** owned-entity reads form a nested block whose source document is obtained by a
parent-document element access (`bsonDoc["Address"]`). The rewriter's per-entity local map must be
scoped — each nested entity block has its own `MaterializationContext` and its own set of
`ValueBufferTryReadValue` calls reading from its own sub-document. A forward-only `IBsonReader`
streaming materializer must descend into the `Address` sub-document at the right point in the wire
order and read its fields there.

## (d) How `Tags` (string[]) appears

- **Not present in this capture.** The current benchmark `Customer` has no `string[] Tags` property
  (only `ObjectId Id`, 11 scalars, owned `Address`). So this run does not exercise an array/collection
  read.
- General expectation for the rewriter (from the binding-remover code, not from this capture): a
  primitive array mapped as a single scalar property would surface as **one
  `ValueBufferTryReadValue<string[]>(...)`** with an array serializer (handled identically to other
  scalars — match by `IProperty`, target local type is the array type). A collection **navigation**
  (owned collection) would instead surface as a `CollectionShaperExpression` /
  `ObjectArrayProjectionExpression` with an inner per-element block (see
  `MongoProjectionBindingRemovingExpressionVisitor.VisitExtension`, `CollectionShaperExpression`
  case, lines 133–189) — a different shape the rewriter must handle separately.
- **Action for a later task:** if the rewriter must cover `Tags`, add a `string[] Tags` property to
  the benchmark `Customer` and re-capture to see which of the two shapes EF emits (mapped-array
  scalar vs. collection shaper). Do not assume; the two paths differ materially.

## (e) Second read for the tracking snapshot

- **For the Customer (root) entity:** in the tracked block there is **no** second read of the scalar
  properties for a snapshot. `shadowSnapshot1 = Snapshot;` is the empty/no-op snapshot sentinel
  (Customer has no shadow properties), and tracking is established via
  `queryContext.StartTracking(entityType1, instance1, shadowSnapshot1)`. The PK *is* read a second
  time, but for a different purpose — once inside `TryGetEntry`'s `keyValues` (`<object>` read,
  index 0) and once as the `instance.<Id>k__BackingField` assignment (`<ObjectId>` read, index 0).
  That's the identity-map lookup vs. the field assignment, not a snapshot duplicate.
- **For the owned `Address`:** there **is** a snapshot read. The shadow PK `Address.CustomerId` is
  read into the snapshot:
  `shadowSnapshot2 = (ISnapshot)new Snapshot<ObjectId>(ExpressionExtensions.ValueBufferTryReadValue<ObjectId>(..., index: 0, property: Address.CustomerId ...));`
  i.e. `Address.CustomerId` is read twice — once for `TryGetEntry.keyValues` (`<object>`) and once
  for the `Snapshot<ObjectId>` (`<ObjectId>`), both at index 0.
- **No-tracking variant** (`AsNoTracking().ToList()`): the entire `TryGetEntry` / `StartTracking` /
  snapshot machinery is removed. The null-key check becomes
  `ValueBufferTryReadValue<object>(... PK ...) != null ? <materialize> : <CreateNullKeyValueInNoTrackingQuery(...)>`,
  the `IInjectableService.Injected` `queryTrackingBehavior` argument is `NoTracking` instead of
  `TrackAll`, and the `Snapshot<ObjectId>` line for Address is gone. The scalar
  `ValueBufferTryReadValue` reads themselves are byte-for-byte identical between tracked and
  no-tracking.

**Rewriter takeaway:** any property may be read **more than once** in the same block (PK for the
identity lookup + for the field assignment; shadow keys also for the snapshot). A correct streaming
rewriter cannot consume the forward reader once per textual `ValueBufferTryReadValue` occurrence — it
must read each *property* once into a local and then substitute **every** occurrence of that
property's read (regardless of the `<T>` generic — `<object>` vs `<ObjectId>` for the same PK) with
references to that local (converting type as needed). Mapping is by `IProperty` identity, and the
local's type should be the property's CLR type; `<object>`-typed call sites get a boxing convert.

## (f) Row-type plumbing — what must change for `RawBsonDocument`

Today the entity / mixed path threads `BsonDocument` as the wire row type end-to-end:

1. **`CompileShapedQuery`** (`MongoShapedQueryCompilingExpressionVisitor.cs`):
   - `bsonDocParameter = Expression.Parameter(typeof(BsonDocument), "bsonDoc")` (line 162) — the
     shaper lambda's row parameter.
   - `shaperLambda = Expression.Lambda(shaperBody, QueryContextParameter, bsonDocParameter)` (185–188);
     `compiledShaper` has delegate type `Func<QueryContext, BsonDocument, projectedType>`.
   - Dispatch: `ExecuteShapedQueryMethodInfo.MakeGenericMethod(rootEntityType.ClrType, projectedType)`
     (line 196). **Generic args: `<TSource, TResult>` = `<rootEntityType.ClrType, projectedType>`**
     (e.g. `<Customer, Customer>` for the whole-entity read). Note `TSource` here is the **CLR entity
     type**, used only by `TranslateQuery<TSource>` to get the typed collection/serializer; it is
     **not** the shaper row type.

2. **`ExecuteShapedQuery<TSource, TResult>`** (lines 325–349):
   - Return type is `QueryingEnumerable<BsonDocument, TResult>` — the **row type is hard-coded to
     `BsonDocument`**, decoupled from `TSource`.
   - `shaper` parameter is `Func<QueryContext, BsonDocument, TResult>`.
   - `TranslateQuery<TSource>(...)` builds the executable query against the CLR-typed collection
     (`GetCollection<TSource>` + `.As(entitySerializer)`), but the *cursor* the driver returns is
     `BsonDocument` (see the native path / `As(...)`); the shaper consumes `BsonDocument` rows.

3. **`QueryingEnumerable<TSource, TTarget>`** (`QueryingEnumerable.cs`):
   - Here `TSource` is the **row type** (instantiated as `BsonDocument` from
     `ExecuteShapedQuery`), `TTarget` the shaped result.
   - `_shaper` is `Func<MongoQueryContext, TSource, TTarget>` (line 32).
   - The cursor is obtained via `_queryContext.MongoClient.Execute<TSource>(_executableQuery, out logAction)`
     (line 168) and each row fed to `_shaper(_queryContext, _enumerator.Current)` (line 190).

4. **`MongoClientWrapper.Execute<T>`** (`MongoClientWrapper.cs`, lines 88–112):
   - For the native pipeline path it does
     `Database.GetCollection<BsonDocument>(...).Aggregate(pipeline)` and casts
     `(IEnumerable<T>)cursor.ToEnumerable()` (lines 101–106) — so `T` is effectively `BsonDocument`.
   - For the driver-LINQ path: `executableQuery.Provider.CreateQuery<T>(executableQuery.Query)` (line 109).
   - `IMongoClientWrapper.Execute<T>` signature: `IEnumerable<T> Execute<T>(MongoExecutableQuery, out Action log)`.

**To make the streaming path use `RawBsonDocument` as the row type, the changes are:**

- `CompileShapedQuery`: build the shaper lambda's row parameter as
  `Expression.Parameter(typeof(RawBsonDocument), "bsonDoc")` (or a parallel streaming lambda), so
  `compiledShaper : Func<QueryContext, RawBsonDocument, projectedType>`.
- `ExecuteShapedQuery<TSource, TResult>`: change the return/row type from
  `QueryingEnumerable<BsonDocument, TResult>` to `QueryingEnumerable<RawBsonDocument, TResult>` and
  the `shaper` parameter to `Func<QueryContext, RawBsonDocument, TResult>`. `TSource` (CLR entity
  type) stays as-is for `TranslateQuery<TSource>` / serializer lookup.
- `QueryingEnumerable<TSource, TTarget>`: instantiated with `TSource = RawBsonDocument`; no signature
  change needed (it's already generic over the row type) — it will call `Execute<RawBsonDocument>`
  and feed `RawBsonDocument` rows to the shaper.
- `MongoClientWrapper.Execute<T>`: the native path's `Aggregate` must return raw documents — i.e.
  `Database.GetCollection<RawBsonDocument>(...)` (or configure the cursor to yield `RawBsonDocument`)
  so `(IEnumerable<T>)cursor.ToEnumerable()` with `T = RawBsonDocument` is valid. (The driver-LINQ
  `CreateQuery<T>` path likely cannot yield `RawBsonDocument` directly — the streaming path should be
  gated to the native-pipeline branch.)
- The streaming rewriter then opens an `IBsonReader` over each `RawBsonDocument` and replaces the
  `ValueBufferTryReadValue` reads (matched per (a)) with forward-read fills into typed locals (per
  (e), one local per `IProperty`, all occurrences substituted), descending into sub-documents for
  owned types (per (c)).

---

## Instrumentation used (reverted before commit)

In `MongoShapedQueryCompilingExpressionVisitor.CompileShapedQuery`, immediately after the
`#if EF8 || EF9 / #else / #endif` materializer-injection block and before the
`createBindingRemover(...).Visit(...)` line:

```csharp
System.Console.Error.WriteLine("MATERIALIZER-BLOCK >>>\n" + new Microsoft.EntityFrameworkCore.Query.ExpressionPrinter().PrintExpression(shaperBody) + "\n<<<MATERIALIZER-BLOCK");
```

Capture command:

```bash
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 dotnet run -c "Release EF10" -- --smoke 2>&1 \
  | sed -n '/MATERIALIZER-BLOCK >>>/,/<<<MATERIALIZER-BLOCK/p'
```
