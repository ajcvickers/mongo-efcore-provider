# Owned-collection materializer shape (Basket → BasketItem)

Diagnostic capture to inform extending the forward-only streaming materializer to owned
collections. The streaming rewriter currently rejects collections (`StreamingEligibility`),
so `Basket` falls back to the DOM path. We captured the EF materializer tree to understand
what the rewriter must learn to handle.

## Entity

`Basket` (document root): `ObjectId Id` (PK), `string Owner`, `int Code`, `List<BasketItem> Items`
(owned collection via `OwnsMany`). `BasketItem`: `string Sku`, `int Qty`, `decimal Price`.

## Capture method

Temporary instrumentation in
`src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`,
`CompileShapedQuery`, printing the **post-injection** materializer body (`injectedBody`, i.e.
after `InjectStructuralTypeMaterializers`, EF10) via `ExpressionPrinter`, immediately before
`createBindingRemover` runs. The instrumentation was reverted after capture.

Important: this is the **pre-binding-removal** tree. The `CollectionShaper` node and the
`StructuralTypeShaperExpression`-derived materializer blocks are still present; the lowering to
`PopulateCollection<...>` / `SelectWithOrdinal` and the resolution of the ordinal key happen
**later**, in `MongoProjectionBindingRemovingExpressionVisitor` (the `createBindingRemover`
step). That visitor is the DOM path; notes (b)–(d) cross-reference it because the captured tree
shows the shape the rewriter receives, and the binding-remover shows what the DOM path turns it
into.

Smoke (DOM fallback path) confirmed correct round-trip:

```
BASKET OK: baskets=100, items=300
```

## Captured materializer block (verbatim; scalar reads trimmed only where noted)

```
MAT-BLOCK Basket >>>
Include(
    Entity:
    {
        BsonDocument bsonDoc1;
        bsonDoc1 = (ProjectionBindingExpression: 0 as BsonDocument);
        return bsonDoc1 == null ? null :
        {
            MaterializationContext materializationContext1;
            IEntityType entityType1;
            Basket instance1;
            InternalEntityEntry entry1;
            bool hasNullKey1;
            materializationContext1 = new MaterializationContext(
                ProjectionBindingExpression: 0,
                queryContext.Context
            );
            instance1 = default(Basket);
            entry1 = queryContext.TryGetEntry(
                key: Key: Basket.Id PK,
                keyValues: new object[]{ ExpressionExtensions.ValueBufferTryReadValue<object>(
                    valueBuffer: materializationContext1.get_ValueBuffer(),
                    index: 0,
                    property: Property: Basket.Id (ObjectId) Required PK AfterSave:Throw ValueGenerated.OnAdd) },
                throwOnNullKey: True,
                hasNullKey: hasNullKey1);
            !(hasNullKey1) ? entry1 != default(InternalEntityEntry) ?
            {
                entityType1 = entry1.EntityType;
                return instance1 = (Basket)entry1.Entity;
            } :
            {
                ISnapshot shadowSnapshot1;
                shadowSnapshot1 = Snapshot;
                entityType1 = EntityType: Basket;
                instance1 = switch (entityType1)
                {
                    case EntityType: Basket:
                        {
                            return
                            {
                                Basket instance;
                                instance = new Basket();
                                instance.<Id>k__BackingField   = ValueBufferTryReadValue<ObjectId>(... index: 0, Basket.Id ...);
                                instance.<Code>k__BackingField = ValueBufferTryReadValue<int>(... index: 1, Basket.Code ...);
                                instance.<Owner>k__BackingField= ValueBufferTryReadValue<string>(... index: 2, Basket.Owner ...);
                                (instance is IInjectableService) ? ...Injected(... EntityType: Basket) : default(void);
                                return instance;
                            }}
                    default:
                        default(Basket)
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
    Navigation: Items,
    {
        BsonArray bsonArray2;
        bsonArray2 = (bsonDoc["Items"] as BsonArray);
        return bsonArray2 == null ? null : CollectionShaper:
            (bsonDoc["Items"],
            {
                MaterializationContext materializationContext2;
                IEntityType entityType2;
                BasketItem instance2;
                InternalEntityEntry entry2;
                bool hasNullKey2;
                materializationContext2 = new MaterializationContext(
                    (ValueBuffer)(object)bsonDoc
                    ,
                    queryContext.Context
                );
                instance2 = default(BasketItem);
                entry2 = queryContext.TryGetEntry(
                    key: Key: BasketItem.BasketId, BasketItem.Id PK,
                    keyValues: new object[]
                    {
                        ValueBufferTryReadValue<object>(... index: 0,
                            Property: BasketItem.BasketId (no field, ObjectId) Shadow Required PK FK AfterSave:Throw),
                        ValueBufferTryReadValue<object>(... index: 1,
                            Property: BasketItem.Id (no field, int) Shadow Required PK BeforeSave:Ignore AfterSave:Throw ValueGenerated.OnAddOrUpdate)
                    },
                    throwOnNullKey: False,
                    hasNullKey: hasNullKey2);
                !(hasNullKey2) ? entry2 != default(InternalEntityEntry) ?
                {
                    entityType2 = entry2.EntityType;
                    return instance2 = (BasketItem)entry2.Entity;
                } :
                {
                    ISnapshot shadowSnapshot2;
                    shadowSnapshot2 = Snapshot;
                    entityType2 = EntityType: BasketItem Owned;
                    instance2 = switch (entityType2)
                    {
                        case EntityType: BasketItem Owned:
                            {
                                shadowSnapshot2 = (ISnapshot)new Snapshot<ObjectId, int>(
                                    ValueBufferTryReadValue<ObjectId>(... index: 0, BasketItem.BasketId ...),
                                    ValueBufferTryReadValue<int>(... index: 1, BasketItem.Id ...)
                                );
                                return
                                {
                                    BasketItem instance;
                                    instance = new BasketItem();
                                    instance.<Price>k__BackingField = ValueBufferTryReadValue<decimal>(... index: 2, BasketItem.Price ...);
                                    instance.<Qty>k__BackingField   = ValueBufferTryReadValue<int>(... index: 3, BasketItem.Qty ...);
                                    instance.<Sku>k__BackingField   = ValueBufferTryReadValue<string>(... index: 4, BasketItem.Sku ...);
                                    (instance is IInjectableService) ? ...Injected(... EntityType: BasketItem Owned) : default(void);
                                    return instance;
                                }}
                        default:
                            default(BasketItem)
                    }
                    ;
                    entry2 = entityType2 == default(IEntityType) ? default(InternalEntityEntry) : queryContext.StartTracking(
                        entityType: entityType2,
                        entity: instance2,
                        snapshot: shadowSnapshot2);
                    return instance2;
                } : default(void);
                return instance2;
            }, Items)
        ;
    }
<<<MAT-BLOCK
```

## Notes

### (a) How does the owned collection (`Items`) appear?

As an **`IncludeExpression`** whose `Navigation` is `Items` and whose `Navigation.IsCollection`
is true (`OwnsMany`). The `Include(...)` node has two arms: the `Entity:` arm materializes the
root `Basket`; the `Navigation: Items, { ... }` arm produces the collection. Inside that arm the
collection itself is a **`CollectionShaperExpression`** (printed as `CollectionShaper: (...)`):

```
return bsonArray2 == null ? null : CollectionShaper:
    (bsonDoc["Items"], <inner BasketItem shaper>, Items)
```

The collection is fed from the BSON array read by **element name `"Items"`** —
`bsonArray2 = (bsonDoc["Items"] as BsonArray)`, with the `CollectionShaper`'s first projection
argument also `bsonDoc["Items"]`. The PascalCase `Items` matches the navigation name (no camel-case
convention applied here). So: an `IncludeExpression` (collection navigation) wrapping a
`CollectionShaperExpression`, **not** a bare `ObjectArrayProjectionExpression` at this stage —
though the binding-remover resolves the `CollectionShaper`'s projection to an
`ObjectArrayProjectionExpression` when it lowers it (see `MongoProjectionBindingRemovingExpressionVisitor`
`VisitExtension`, `case CollectionShaperExpression`, which expects an `ObjectArrayProjectionExpression`).

### (b) Inner per-element (`BasketItem`) shaper

The element materializer (`instance2`) reads the three mapped scalars by
`ValueBufferTryReadValue` against `materializationContext2` (whose value buffer is
`(ValueBuffer)(object)bsonDoc` — the per-element BSON document):

- `index: 2` → `BasketItem.Price` (`decimal`)
- `index: 3` → `BasketItem.Qty` (`int`)
- `index: 4` → `BasketItem.Sku` (`string`)

The synthetic key is a **two-property composite PK**: `Key: BasketItem.BasketId, BasketItem.Id PK`:

- `index: 0` → **owner FK** `BasketItem.BasketId (no field, ObjectId) Shadow Required PK FK AfterSave:Throw`
  — the shadow foreign key back to the owner `Basket.Id`.
- `index: 1` → **synthetic ordinal key** `BasketItem.Id (no field, int) Shadow Required PK
  BeforeSave:Ignore AfterSave:Throw ValueGenerated.OnAddOrUpdate` — the synthesized owned-collection
  ordinal `Id`. This is the property for which `IsOwnedTypeOrdinalKey()` returns true (confirmed:
  `MongoValueGeneratorSelector` and `MongoUpdate.FindOrdinalKeyProperty` both gate on
  `property.IsOwnedTypeOrdinalKey()`, matching the PK property with an empty element name).

Both key reads feed `queryContext.TryGetEntry(... throwOnNullKey: False ...)` and the
`Snapshot<ObjectId, int>` shadow snapshot.

In the **captured (pre-binding-removal) tree**, the ordinal `Id` is just another
`ValueBufferTryReadValue<int>(index: 1, ...)`. It is **not** yet sourced from a loop index — that
substitution is the binding-remover's job. In `MongoProjectionBindingRemovingExpressionVisitor.
CreateGetValueExpression`, when `property.IsOwnedTypeOrdinalKey()` and the ownership is a
collection (`ownership.IsUnique == false`), the read is replaced by `_ordinalMappings[docExpression]`,
which is `Expression.Add(ordinalParameter, Expression.Constant(1))` — i.e. **loop index + 1**. The
`ordinalParameter` is the second parameter of the `SelectWithOrdinal` lambda (see (c)). So the DOM
path does *not* read the ordinal from BSON; it synthesizes it from the array position (1-based).

### (c) How the collection is populated

In the lowered (DOM) form, `MongoProjectionBindingRemovingExpressionVisitor` (around lines 165–188)
builds:

```
PopulateCollection<BasketItem, List<BasketItem>>(
    navigation.GetCollectionAccessor(),               // IClrCollectionAccessor
    SelectWithOrdinal<BsonDocument, BasketItem>(       // EnumerableMethods.SelectWithOrdinal
        Cast<BsonDocument>(bsonArrayExpression),        // the "Items" BsonArray cast to IEnumerable<BsonDocument>
        (jObjectParameter, ordinalParameter) => <innerShaper>))   // lambda over (element doc, ordinal)
```

`PopulateCollection<TEntity, TCollection>(IClrCollectionAccessor accessor, IEnumerable<TEntity>
entities)` creates the collection via `accessor.Create()`, `foreach`-adds each materialized
element, and returns it. The element enumerable is built with **`SelectWithOrdinal`** (the provider's
`EnumerableMethods.SelectWithOrdinal`), whose projection lambda takes two parameters: the per-element
`BsonDocument` (`<arrayName>Object`) and the zero-based ordinal (`<arrayName>Ordinal`). The ordinal
parameter is what feeds `_ordinalMappings` (ordinal + 1) for the synthetic key in (b).

### (d) Differences from the single-owned-reference `IncludeExpression` (which the streaming rewriter already handles)

The single owned reference (`Customer.Address` via `OwnsOne`) is an `IncludeExpression` whose
`Navigation.IsCollection` is false: the navigation arm materializes a **single** nested entity from
a single nested BSON document (`bsonDoc["Address"]`), with no array iteration, no ordinal, and no
collection-populate call. The owned **collection** differs in three ways the rewriter must learn:

1. **Array iteration.** The element shaper runs once per array element. The collection arm reads a
   `BsonArray` (`bsonDoc["Items"]`) and iterates it, rather than reading one nested document. In
   streaming terms, the rewriter must read a BSON array via the forward `IBsonReader` and loop the
   element shaper per array entry (start array / read each document / end array) instead of a single
   nested-document read.

2. **Synthetic ordinal key.** The element PK is composite — owner FK (`BasketItem.BasketId`) +
   synthetic ordinal (`BasketItem.Id`, `IsOwnedTypeOrdinalKey`). The ordinal is **not** in BSON; the
   DOM path supplies `loopIndex + 1` via the `SelectWithOrdinal` ordinal parameter
   (`_ordinalMappings`). A streaming rewriter must maintain a 1-based counter while iterating the
   array and feed it where the materializer reads the ordinal key (index 1 here), since there is no
   BSON element to read for it. The single owned reference has no ordinal at all.

3. **Populate-collection node.** The result is wrapped in `PopulateCollection<TEntity,TCollection>`
   over a `SelectWithOrdinal` enumerable, using the navigation's `IClrCollectionAccessor` to create
   and fill the target collection. The single reference simply assigns the materialized instance to
   the navigation; there is no accessor/create/add. A streaming rewriter must create the collection
   via the accessor and add each streamed element in order.

Also note the owner FK (index 0, `BasketItem.BasketId`) is the shadow foreign key to `Basket.Id`;
in the DOM path it is read from the element doc context via `_ownerMappings`
(`CreateGetValueExpression` → `FindFirstPrincipal`), not from an `"BasketId"` element in the array
document. The streaming rewriter must likewise supply the owner key from the parent rather than
expecting it inline in each array element.
