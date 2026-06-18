# Cross-collection reference-Include materializer shape

Captured from EF Core's post-injection materializer tree (the `injectedBody` produced by
`InjectStructuralTypeMaterializers`) inside
`src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.CompileShapedQuery`,
for the query:

```csharp
ctx.Reviews.Include(r => r.Product).ToList();
```

`Review` and `Product` each have their own `DbSet` (separate collections, non-owned), wired by:

```csharp
modelBuilder.Entity<Review>().HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId);
```

This query currently takes the DOM (driver-LINQ) path: the Include registers a pending `$lookup`, and the
streaming gate excludes any query with `GetPendingLookups().Count != 0`. We capture the tree anyway to design
the streaming rewriter's cross-collection reference branch.

## Captured materializer block (verbatim; scalar reads kept — they are minimal here)

```
Include(
    Entity:
    {
        BsonDocument bsonDoc1;
        bsonDoc1 = (ProjectionBindingExpression: 1 as BsonDocument);
        return bsonDoc1 == null ? null :
        {
            MaterializationContext materializationContext1;
            IEntityType entityType1;
            Review instance1;
            InternalEntityEntry entry1;
            bool hasNullKey1;
            materializationContext1 = new MaterializationContext(
                ProjectionBindingExpression: 1,
                queryContext.Context
            );
            instance1 = default(Review);
            entry1 = queryContext.TryGetEntry(
                key: Key: Review.Id PK,
                keyValues: new object[]{ ExpressionExtensions.ValueBufferTryReadValue<object>(
                    valueBuffer: materializationContext1.get_ValueBuffer(),
                    index: 0,
                    property: Property: Review.Id (ObjectId) Required PK AfterSave:Throw ValueGenerated.OnAdd) },
                throwOnNullKey: True,
                hasNullKey: hasNullKey1);
            !(hasNullKey1) ? entry1 != default(InternalEntityEntry) ?
            {
                entityType1 = entry1.EntityType;
                return instance1 = (Review)entry1.Entity;
            } :
            {
                ISnapshot shadowSnapshot1;
                shadowSnapshot1 = Snapshot;
                entityType1 = EntityType: Review;
                instance1 = switch (entityType1)
                {
                    case EntityType: Review:
                        {
                            return
                            {
                                Review instance;
                                instance = new Review();
                                instance.<Id>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<ObjectId>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 0,
                                    property: Property: Review.Id (ObjectId) Required PK AfterSave:Throw ValueGenerated.OnAdd);
                                instance.<ProductId>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<ObjectId>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 1,
                                    property: Property: Review.ProductId (ObjectId) Required FK Index);
                                instance.<Stars>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<int>(
                                    valueBuffer: materializationContext1.get_ValueBuffer(),
                                    index: 2,
                                    property: Property: Review.Stars (int) Required);
                                (instance is IInjectableService) ? ((IInjectableService)instance).Injected(...) : default(void);
                                return instance;
                            }}
                    default:
                        default(Review)
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
    Navigation: Product,
    {
        BsonDocument bsonDoc2;
        bsonDoc2 = (ProjectionBindingExpression: 0 as BsonDocument);
        return bsonDoc2 == null ? null :
        {
            MaterializationContext materializationContext2;
            IEntityType entityType2;
            Product instance2;
            InternalEntityEntry entry2;
            bool hasNullKey2;
            materializationContext2 = new MaterializationContext(
                ProjectionBindingExpression: 0,
                queryContext.Context
            );
            instance2 = default(Product);
            entry2 = queryContext.TryGetEntry(
                key: Key: Product.Id PK,
                keyValues: new object[]{ ExpressionExtensions.ValueBufferTryReadValue<object>(
                    valueBuffer: materializationContext2.get_ValueBuffer(),
                    index: 0,
                    property: Property: Product.Id (ObjectId) Required PK AfterSave:Throw ValueGenerated.OnAdd) },
                throwOnNullKey: True,
                hasNullKey: hasNullKey2);
            !(hasNullKey2) ? entry2 != default(InternalEntityEntry) ?
            {
                entityType2 = entry2.EntityType;
                return instance2 = (Product)entry2.Entity;
            } :
            {
                ISnapshot shadowSnapshot2;
                shadowSnapshot2 = Snapshot;
                entityType2 = EntityType: Product;
                instance2 = switch (entityType2)
                {
                    case EntityType: Product:
                        {
                            return
                            {
                                Product instance;
                                instance = new Product();
                                instance.<Id>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<ObjectId>(
                                    valueBuffer: materializationContext2.get_ValueBuffer(),
                                    index: 0,
                                    property: Property: Product.Id (ObjectId) Required PK AfterSave:Throw ValueGenerated.OnAdd);
                                instance.<Title>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<string>(
                                    valueBuffer: materializationContext2.get_ValueBuffer(),
                                    index: 1,
                                    property: Property: Product.Title (string) Required);
                                (instance is IInjectableService) ? ((IInjectableService)instance).Injected(...) : default(void);
                                return instance;
                            }}
                    default:
                        default(Product)
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

(Top-level node is an `IncludeExpression` with `EntityExpression` = the Review block, `Navigation` = `Product`,
and `NavigationExpression` = the Product block. The `SetLoaded` flag and the fixup helper are not surfaced by
`ExpressionPrinter`; they are reduced by the binding remover — see notes (a) and (d).)

## Notes

### (a) The `IncludeExpression` for `Product`

- `Navigation: Product` is a **non-owned reference**: `IsCollection = false`, and it is the principal side of a
  `Review (dependent) --HasOne--> Product (principal)` relationship with FK `Review.ProductId`. The property
  dump confirms `Review.ProductId (ObjectId) Required FK Index` and `Product.Id (ObjectId) ... PK`.
- The navigation arm (`NavigationExpression`, the Product block) reads its `MaterializationContext` /
  `ValueBuffer` from `ProjectionBindingExpression: 0`. The printer does not show the underlying access node, but
  the binding remover resolves a cross-collection reference's projection to an `ObjectAccessExpression` over a
  **root-level `_lookup_<NavigationName>` field** — here **`_lookup_Product`** — in flat `$lookup` + `$unwind`
  mode. See `MongoProjectionBindingRemovingExpressionVisitor.cs`:
  - `GetCrossCollectionFieldName(accessExpression)` (line ~505) returns the alias baked into the
    `ObjectAccessExpression.Name` — `_lookup_<Navigation>` in flat mode, or `_inner` in driver-native LeftJoin
    mode.
  - The switch at line ~284 (`case ObjectAccessExpression crossCollectionAccess when
    IsCrossCollectionAccess(...)`) sets `innerAccessExpression = GetCrossCollectionRootDocument(...)` (the
    absolute query root for a top-level reference) and `fieldName = GetCrossCollectionFieldName(...)`,
    i.e. `bsonDoc["_lookup_Product"]`.
  - Critically, `fieldRequired = false` is forced here (line 296) — the joined field is treated as optional even
    though the navigation is to a present principal (see (c)).

  So the navigation source is an `ObjectAccessExpression(Name = "_lookup_Product", root = query-root bsonDoc)`,
  field name **`_lookup_Product`** (one field per navigation, post-`$unwind` so it is a sub-document, not an
  array).

### (b) Inner `Product` construction block — own PK read

- `Product.Id` is read as a **normal `ValueBufferTryReadValue<ObjectId>` at `index: 0` from the joined
  sub-document's own value buffer** — NOT an owner-resolved / parent-key read. Verbatim:

  ```
  instance.<Id>k__BackingField = ExpressionExtensions.ValueBufferTryReadValue<ObjectId>(
      valueBuffer: materializationContext2.get_ValueBuffer(),
      index: 0,
      property: Property: Product.Id (ObjectId) Required PK AfterSave:Throw ValueGenerated.OnAdd);
  ```

  This is identical in shape to a root entity's own PK read — the Product value buffer is the `_lookup_Product`
  sub-document, and `index 0`/`index 1` are positions within that sub-document. There is no owner key resolution
  (unlike an owned type, whose PK is the owner's PK). `Product.Title` follows at `index: 1`.

- `TryGetEntry` / `StartTracking` for `Product` use **`Product`'s own PK** (`Key: Product.Id PK`), reading the
  key value via `ValueBufferTryReadValue<object>(... index: 0 ...)`:

  ```
  entry2 = queryContext.TryGetEntry(
      key: Key: Product.Id PK,
      keyValues: new object[]{ ValueBufferTryReadValue<object>(materializationContext2.ValueBuffer, 0, Product.Id) },
      throwOnNullKey: True, hasNullKey: hasNullKey2);
  ...
  entry2 = entityType2 == default(IEntityType) ? default : queryContext.StartTracking(EntityType: Product, instance2, shadowSnapshot2);
  ```

  This is a fully independent entity entry (its own identity-map slot), unlike an owned type which shares the
  owner's entry. Confirms cross-collection Product is tracked as a first-class entity.

### (c) The null guard (post-`$unwind` `_lookup_Product` may be Null on no match)

Two layers guard absence, both present in the captured tree:

1. **DOM presence guard** at the top of the Product navigation block:
   `bsonDoc2 = (ProjectionBindingExpression: 0 as BsonDocument); return bsonDoc2 == null ? null : { ... }`.
   The binding remover binds `ProjectionBindingExpression: 0` to `bsonDoc["_lookup_Product"]` with
   `fieldRequired = false` (note (a)), so a missing/Null `_lookup_Product` field yields a null `BsonDocument`,
   and the `bsonDoc2 == null ? null` arm returns a null Product — no materialization, no `StartTracking`.
2. **Null-key guard** inside materialization: `TryGetEntry(..., throwOnNullKey: True, hasNullKey: hasNullKey1/2)`
   followed by `!(hasNullKey1) ? <materialize/track> : default(void)`. If the joined PK reads as null the entity
   is skipped.

So absence is handled by `_lookup_Product == null -> null navigation`, with a secondary null-key short-circuit.
In `$unwind`-with-`preserveNullAndEmptyArrays` mode an unmatched parent carries a Null `_lookup_Product`
sub-document, which the `bsonDoc2 == null` arm converts to a null navigation. (For this benchmark every Review
matches a Product, so `withProduct == 100`.)

### (d) Does the reference fixup match the OWNED-reference shape the streaming rewriter already handles?

**The `IncludeExpression`/fixup node shape is the SAME generic `IncludeExpression` + `IncludeReference` fixup the
streaming rewriter's `SpliceReferenceInclude` already emits** — same EF reduction:
`include.Navigation` (an `INavigation`, `IsCollection == false`), `include.EntityExpression`,
`include.NavigationExpression`, `include.SetLoaded`, reduced to an `IncludeReference<TIncluding,TIncluded>` call
with a `GenerateReferenceFixup(includingClrType, relatedClrType, navigation, inverseNavigation)` delegate.
`SpliceReferenceInclude` (`MongoStreamingEntityMaterializerRewriter.cs` line ~489) is navigation-kind-agnostic:
it pulls the `instance`/`entityType`/`entry` locals out of the entity block and splices the
`IncludeReference` fixup before the trailing instance — that machinery is **reusable as-is** for a non-owned
reference. The fixup itself (FK/inverse handling) is the standard EF reference fixup; here `inverseNavigation`
is `null` (the relationship was configured `.WithMany()` with no inverse navigation on `Product`), which
`SpliceReferenceInclude` already passes as `Expression.Constant(inverseNavigation, typeof(INavigation))` —
null-tolerant.

**What DIFFERS is the navigation-arm source, NOT the fixup.** The streaming rewriter's `RewriteOwnedNavigation`
(line ~752) is owned-only: it expects the owned-entity block's value source to be redirected to a
**child `EntityPlan`'s locals** materialized from the SAME streaming row (the owned sub-document is inline in the
parent's RawBsonDocument), and it replaces the `bsonDocN == null` guard with a `!present` flag tracked during
the single forward `IBsonReader` pass. A cross-collection reference's data does NOT live inline in the parent
row in a way the current forward-reader plan models — it arrives in a separate root-level `_lookup_Product`
sub-document produced by `$lookup` + `$unwind`. So the rewriter needs a NEW navigation-arm rewrite that:
  - reads the joined `_lookup_Product` sub-document as its own value source (a nested reader scope keyed to the
    `_lookup_Product` field, not a `present` flag over an inline owned field), and
  - constructs/tracks Product as an **independent entity** with its own PK (note (b)), not an owner-keyed owned
    instance, then
  - feeds the resulting (possibly null, note (c)) instance into the existing `SpliceReferenceInclude` +
    `IncludeReference` fixup.

**Bottom line for the rewriter:** `SpliceReferenceInclude` / `IncludeReference` / `GenerateReferenceFixup` are
reusable unchanged for the fixup wiring. The new work is a `RewriteCrossCollectionReference` arm parallel to
`RewriteOwnedNavigation` that (1) sources the `_lookup_<Nav>` sub-document, (2) does a full independent-entity
TryGetEntry/StartTracking on the joined PK, and (3) maps the `_lookup_<Nav> == null` / null-key cases to a null
navigation. The pending-lookup streaming gate
(`mongoQueryExpression.GetPendingLookups().Count == 0` in `CompileShapedQuery`, and the matching gate in
`TranslateQuery`) must also be relaxed for the cross-collection-reference case so the native streaming pipeline
is actually built for these queries.
