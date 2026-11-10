---
area: Query / LINQ translation
scope: ["src/MongoDB.EntityFrameworkCore/Query/**"]
reviewer-agent: query-reviewer
adjacent-areas: [Storage, Metadata, Serializers, "C# driver LINQ v3"]
---

# Query — AGENTS.md

Translates `IQueryable<T>` into aggregation pipelines via two paths, chosen at compile time by `MongoQueryMode`:

- **Native (default):** the provider builds the `BsonDocument[]` pipeline from its own query tree.
- **Driver-LINQ (fallback):** anything not representable natively goes to the driver's LINQ v3 provider.
  `MongoQueryMode.NativeOnly` throws at compile time instead of falling back.

Which path a query takes, the exact MQL, and the exception type for an unsupported shape are not contract.

## Pipeline

The QMTEV populates the native IR (`MongoSelectDefinition`) via `Native*Binder`s and always also captures the raw chain
(`CapturedExpression`) for the fallback. `MongoShapedQueryCompilingExpressionVisitor` is the gate
(`ClassifyNativeDisposition`): native goes `MongoSelectLowerer` (BSON-free) -> `MongoPipelineFactory` (template +
placeholders); fallback rewrites `CapturedExpression` via `MongoEFToLinqTranslatingExpressionVisitor`.

The template is built once at compile time; only `factory.Build(parameterValues)` runs per execution. `Select.Route` is
the authoritative is-native signal. `MongoExpression.VisitChildren` is a no-op; every consumer hand-rolls a `switch`.
`MongoQueryTranslationPreprocessor` lifts `VectorSearch` out before nav-expansion (which crashes on it) and re-inserts
it after; re-check if you reorder the preprocessor.

## Invariants

Breaking these usually yields **silently wrong rows**, not a failure.

Gate and fallback:

- **The gate must call the fix's predicate, not restate it.** A gate admitting more than the fix handles yields wrong rows.
- **A recognizer must not mutate then decline.** Stage into locals, commit only after every gate passes.
- **Post-terminal gating.** After a terminal operator (`GroupBy`, `Distinct`, set op, `SelectMany` unwind;
  `HasTerminalOperator`) every later operator must decide explicitly if it can stay native. A `Translate*` override
  bypasses the shared gates and must replicate the check.
- **Structural classification beats metadata/depth/CLR-type shortcuts** (query filters, join-hop depth,
  self-referencing types defeat them). `DriverLinq` is no escape hatch for a misclassification.
- **Unsupported shapes call `MarkNotNativelyRepresentable()`** so `Route` becomes `Fallback` instead of emitting a
  pipeline that drops the operator.
- **Bulk `ExecuteUpdate`/`ExecuteDelete` (EF9+) also uses the driver-LINQ bridge** (`MongoEFToLinqTranslatingExpressionVisitor`).
  Retiring the query fallback does not retire it. Its filter and `SetProperty` values ALWAYS run on the driver, so over a
  complex collection's elements (EF10) it is guarded by a POSITIVE list, not the query net's deny-list: every element read must be an
  atom the driver answers like the native null-element rule (`member ==/!= v` with `v` non-null and not a `false` bool; a bare non-nullable, default-serialized bool member
  and `!`; a list that can't hold null `.Contains(member)`; `&&`/`||`/`!` over those) under `Any`/`All`/`Count`/`LongCount`/
  `Where`/`Skip`/`Take` over a REQUIRED collection whose CLR type a `List<T>` fits (`IsNormalizableToEmptyList`, the bridge's own
  coalesce condition: `T[]`/`ObservableCollection<T>` are refused, as are unknown extension nodes); anything else (relational, null tests, `Select`, aggregates, an optional
  collection's operators or null check: the driver's `{p: null}` also matches `[null]`, an element lambda the keying can't
  bind) is refused before any write (`ComplexElementNullGuardRefusal.ThrowIfBulkMisreadsNullElements`, called in every bulk
  plan delegate with this execution's parameter values; explicit `DriverLinq` opts out). A wrong bulk filter deletes data,
  so bulk takes the refusal cost where queries take the net's documented limits. Setters: a complex leaf through REQUIRED single
  complex hops (dotted stored path, `StructuralPath`), a whole complex property/collection from a captured value (serialized by
  `ComplexValueWriter`, as SaveChanges writes it; a required one can't be null); refused: a leaf under an optional complex
  hop (the server's `$set` of a dotted path under a null parent errors mid-operation), a leaf whose element name is empty (not stored),
  a required leaf set to null, a computed value that reads through an optional complex hop unless null-coalesced (a missing value would
  remove or null the target), a whole complex value read from the row,
  a self-referencing value for a BsonRepresentation leaf (root scalars keep their pre-existing behaviour), and two setters
  whose stored paths contain one another (`ThrowIfSetterPathsOverlap`: the server rejects such a `$set`). Refusal messages
  print expressions through `ExpressionShapePrinter` (constants as `?`), never `Expression.ToString()`.
- **A recognizer turning a correlated `DbSet` subquery into a navigation** (`db.Orders.Where(o => o.CustomerId == c.X)`
  -> `c.Orders`) must prove `c.X` is that navigation's FK principal key via `NativeCorrelationMatcher` (single-property FK;
  alternate key via `ForeignKey.PrincipalKey`; composite declines). Binding emits `$lookup {localField: principalKey}`,
  so any other outer member silently reads key-correlated rows.

Scope, joins, grouping:

- **Scope resolves by parameter identity (`ReferenceEquals`), never member name** (shared property names across types is
  the regression). Primitive: `MongoExpressionTranslator.TryBeginOwnedHopWalk`.
- **Collection Include over a join scope** is native only on the root scope. A join holding the Include's
  `_lookup_<Nav>` alias would be collapsed into it by `AddLookup`; renames use `MongoQueryExpression.GetUnusedLookupAlias`.
  A join level the binding would take as the Include's intermediate
  (`LookupExpression.IsCollectionIncludeIntermediateFor`) declines: the binding nests by type, not alias.
- **Bare join-scope leaves.** A bare scalar leaf over a chain re-roots onto the ONE scope its hop chain names
  (`TryTranslateSingleScope`), never by CLR type; it declines when paging, ordering or a deferred paging snapshot exists
  (the lowerer places a chain's paging after every `$lookup`). A bare INNER entity leaf (`HasBareJoinInnerEntityLeaf`)
  is not the whole-entity streaming shape (that rewriter is typed by the root entity); it is read as a projection leaf instead,
  the inner document under its join level's prefix (`StreamedCollectionSubstituter.TryStreamJoinInner`, which also serves the inner
  entity beside other projected values). A reference Include on the inner entity keeps the DOM (see below). A construction whose members are ALL whole-entity
  inner leaves stages each level whole; `BindResultMember` rebinds each shaper with `alias: null` so a sibling
  `o1.o2` is the same instance.
- **A required reference Include is filtered by the cursor, not the server** (`LookupExpression.CanBeFilteredOnClient`,
  `MongoUnwindStage.ClientFiltered`). In a document database "required" means non-null if present, and a document may point at
  nothing. A server `$match` on a `$lookup`'s output costs about as much as the lookup, so once the whole-entity streaming shaper is chosen
  the compile step sets `MongoQueryExpression.FiltersReferenceLookupsOnClient` (only if the shaper checks EVERY such reference) and rebuilds
  the factory: the stage then renders nothing, the shaper reads the `[doc]` array form (`BuildLookupReferenceDescent` also reads the `$set`
  document form) and returns `default` for a row whose reference is absent, which `QueryingEnumerable` skips (`isDropped`). The lowerer keeps
  the server `$match` + `$set` when any stage after the first such lookup is not a further lookup/unwind (paging, sort or `Where` over
  the join, set operations, a count), because those depend on the dropped rows. Dropped rows must record no tracking.
- **ThenInclude off a reference Include is streamed** (`IsStreamableThenIncludeLookup`, `LookupReferencePlan.AtRoot`). A further reference is a
  `$lookup` of its own at the document's ROOT (`_lookup_Customer`, `localField` `_lookup_Order.CustomerID`), so the root fill loop reads it and
  the parent target's materializer uses it. A collection is an array INSIDE its parent's document (`_lookup_Customer._lookup_Notes`) and is
  streamed only under a REQUIRED parent: under an optional one that matched nothing, `$lookup` still creates the parent document to hold the
  array, a joined entity with no key (the DOM shaper reads it as no reference: `BsonBinding.ReferenceDocumentOrNull`). Nested plans hang only off the reference the shaper includes (two
  navigations to one type must not claim each other's lookup), and the cursor filters no row when a ThenInclude is present.
- **Collection lookups the streaming shaper does not read.** A Join onto a collection navigation is a flattened `$lookup` (`IsFlattenedCollectionJoin`):
  the whole-entity shaper ignores it, and the joined entity it returns is read as a projection leaf (`TryStreamEntityLeaf`'s collection-navigation
  branch). A ThenInclude collection under an optional parent that matched nothing leaves a key-less placeholder document, which the reader reads as
  no match (`BuildLookupReferenceDescent`, `tolerateNoKey`). The array of a correlated subquery, or of a collection navigation, projected beside other
  values is read into a `List<T>` of entities (`TryStreamEntityCollection`, `MongoEntityListSerializer`); a tracked query's elements are remapped to the
  instances the state manager tracks (`CanonicalList`). An element that includes a collection reads it from the `_lookup_<Nav>` array inside the element
  (`BuildRootEntityReader(readIncludedCollections: true)`), like a ThenInclude collection.
- **Lookups no shaper reads do not block streaming** (`IsUnreadLookup`): an explicit join (`ForceUnwind`) whose navigation the shaper does not include, the
  unfiltered array behind a `Count` (`IsBareCountSizeSource`) and a correlated keyed array only a predicate uses. A join lookup whose navigation IS included
  (two ThenIncludes through one type: `_lookup_Leaf`, `_lookup_Leaf_1`) is not skipped, because the reader would fill both branches from one array. A
  collection Include renamed to leave its plain alias to such an array (`RenamedToAvoidJoinCollision`) is read from the alias it was given
  (`GetStreamingCollectionAliases`); filtered, paged and discriminator-narrowed Includes only change which elements the array holds.
- **A dotted projection alias is a path of sub-documents.** The projection row reader descends one level per segment (`BuildNameLoop`), so an owned collection
  under owned references (`b.Home.Notes`) and a nested construction's scalars (`GetPropertyValueAtPath`, `GetElementValueAtPath`) are slots like any other.
  An owned array read both whole and through a fused element `Select` is read whole, the `Select` re-applied (one element is read once).
- **A collection Include on a join's inner entity** is the array inside the joined document and streams with it; a REFERENCE Include on it is a `$lookup`
  of its own at the root, which a single-document reader cannot see, so it keeps the DOM. A whole-element `SelectMany` over an owned collection streams: its
  documents are the elements, with no owner document to take keys from.
- **A reference or collection Include on an entity that is a slot of its own** (a join's inner entity, the root beside other values) is done by the row reader
  once both slots are read (`MongoStreamedIncludeExpression`, `MongoIncludeFixups` while the row's tracking is deferred): the including entity's tracking is replayed
  AFTER the entities it includes (`MongoQueryContext.TrackAfterIncluded`), which is what makes the state manager wire the graph. A root's own included
  collection is an array inside its document and is read by its reader, not by this. One entity read twice in an untracked projection is two instances, so the
  row reader copies the sub-document and reads each slot from the copy (`ReadSlotsFromCopy`); a tracked query reads it once.
- **ThenInclude through an owned navigation** (`Include(o => o.Buyer).ThenInclude(b => b.Address).ThenInclude(a => a.Region)`) is the same root-level lookup
  as any ThenInclude reference (`IsOwnedWithin`); two ThenIncludes of one navigation under different parents, or through itself (`Parent.Parent`), have lookups
  `_lookup_Leaf`, `_lookup_Leaf_1`, which the reader finds by parent (`GetStreamingNestedAliases`).
- **An entity with no `$project` is the row's whole document** (`WholeRowAlias`): a client method over it, or a conditional beside its own lookups. The row reader
  copies the document, reads that slot from one copy and every other slot (the lookups inside it) from a second. Anything else the shaper needs of the
  document is read from the lookups it holds; the polymorphic storage conflicts that cannot be read by one plan (sibling derived types storing one element
  differently, or owning different navigations under one element name) stay on the DOM.
- **Filter joins before a reference Include** (`TryAdmitFilterJoins`): extra joins are admitted only when each is a
  resolved, flattened, eligible reference join and the Where that created them left its predicate (and later
  paging/Distinct) in `PostJoinOps`. A REQUIRED navigation's inner join can drop rows the page counted and stays
  declined. Confirmations must equal `Joins.Count`.
- **A filtered join inner is carried on the join's own `$lookup`** (`LookupExpression.InnerPredicate`), set by
  `TryCarryInnerFilterOnLookup` only for `IsFilterOnlyCollectionScan` on an otherwise-eligible, single-pair, un-stamped
  lookup. It renders in `MongoPipelineFactory.RenderLookup` through the placeholder table so parameters and clocks are
  read per execution; never bake it into `PipelineStages`. The predicate is relative to the joined document: nothing is
  re-rooted (a translated predicate cannot be rebased).
  - **No non-native consumer may see such a lookup.** The driver-LINQ bridge drops a join's inner source and with it the
    filter; `MongoEFToLinqTranslatingExpressionVisitor`'s constructor refuses any pending lookup with `IsJoinWithNativeOnlyInnerFilter`
    (or `HasElementStagesWithoutLeaf`). Call those predicates; don't restate them.
  - **A TPH-derived entity set is narrowed in the preprocessor** (`DerivedEntityRootNarrower`:
    `Set<Dog>()` -> `Set<Animal>().OfType<Dog>()` before nav-expansion), so one discriminator predicate applies in every
    mode, on any join side or subquery. Not applied with a vector search.
  - **Paging written before a join stays ahead of its `$lookup` whenever the join can drop rows**
    (`JoinInfo.PagingBeforeItMustStayAhead`): collection/navigation-less joins, inner joins onto optional or
    principal-side references, any lookup with an `InnerPredicate`. Only a REQUIRED dependent-side reference defers it
    (dangling data is the accepted divergence). Paging written AFTER the join applies to joined rows.
- **Aggregates and quantifiers over a referenced (cross-document) collection** (`c.Orders.Any/All/Count(pred)`,
  `.Sum/Min/Max/Average(o => o.Prop)`, GroupJoin aggregates) are projection leaves over the not-unwound `$lookup` array
  `_lookup_<Nav>`. `MongoExpressionTranslator.ReferencedCollectionResolver` (set only on the projection binder's root
  translator) maps the strict FK correlation (`NativeCorrelationMatcher`, never a target-type guess) to that array.
  Value-position `Any(pred)` is `Count(pred) >= 1` and `All(pred)` is `Count(not pred) == 0` (`$elemMatch` is query
  dialect only). Empty group: `$sum` is 0; `$min/$max/$avg` are null, read strictly for a non-nullable result (throws
  like LINQ). The read side binds the whole call only once the native binder accepted the projection
  (`Route == Projection`). Owned collections keep their existing behavior (no `Sum/Min/Max/Average`, no value-position
  `Any/All`). Element leaves and the driver-LINQ bridge are below.
  - **A computed selector** (`Sum(o => o.Price * o.Qty)`) is `MongoMappedArrayReduceExpression`: the reduction of a per-element `$map`
    (`TranslateReferencedComputedAggregate`), where `MongoArrayReduceExpression` reduces the path through the array to one property.
    Only arithmetic over the element's own default-stored numeric properties, constants and query parameters, and numeric casts of them
    (`IsNumericArithmeticOverElement`); anything else (a string `Length`, a conditional, a date part) declines, because a null or missing
    operand would be read as 0 where LINQ throws. Integer division follows the result type, so it truncates per element as C# does; the
    driver-LINQ bridge cannot answer that one (the driver turns `int / int` into a fractional `$divide` and fails on truncation), so it
    fails loudly there.
  - **A correlation with no collection navigation** (`o => c.Key != null && c.Key == o.Fk`) is a keyed array lookup
    (`IsCorrelatedArraySource`, `NativeCorrelationMatcher.TryMatchRawKeyCorrelation`; exactly one such `Where`, the rest
    are element filters). `$lookup` matches a null/missing local key to null/missing foreign values (C# `==` for an
    unguarded subquery); a guarded outer key means null matches NOTHING, which only the `let`/`pipeline` form
    (`ExcludesNullLocalKey`) expresses. Each distinct element-filter set (structural comparer) gets its own alias suffix
    `_fN`; a filter referencing the outer entity is a second correlation and declines.
  - **A correlation on several properties** (an anonymous-type `GroupJoin` key, or `r.A == s.A && r.B == s.B`) is a composite-key
    keyed array: `NativeCorrelationMatcher.TryMatchCorrelationKeys` returns `CorrelationKeyPart`s (the single-pair match is its one-part
    case). The predicate must be a pure conjunction of equalities, each compared property used once per side, plus at most one null guard
    per compared OUTER property (or per outer condition, below); a guard on a property not compared, a conjunct that is not an equality
    or an `||` declines. A guarded part excludes a null outer value (`LookupKeyPair.ExcludesNullLocal`), an unguarded part equates null
    with null, as in a `Join`.
    - **An equality that is not outer-vs-inner is a condition on one side** (`TryMatchCorrelation`; `TryMatchCorrelationKeys` is the
      strict form that admits none). On the ELEMENT alone against a constant or query parameter (`r.Zone == 1`, `1996 == r.OrderDate.Year`,
      `r.Zone + 1 == 2`, `== p`) it becomes an element filter (`CorrelationConditions.Element`, a lambda into `InnerPredicate`). On the OUTER
      row alone (`c.City == "London"`, an outer property against a non-null constant or parameter) it gates the group
      (`LookupExpression.OuterConditions`): `RenderLookup` adds a `let` of the outer field (`$ifNull`-normalized) and a `$match $expr`, so the
      group is empty for an outer row that fails it, a parameter read per execution. At least one outer/inner key pair must remain, and
      only equalities are admitted: `r.Price > 100` is still never dropped. EF's GroupJoin rewrite writes the parts as
      `object.Equals((object)x, (object)y)` and guards a constant (`!(1 == null)`, ignored); both are normalized by the matcher. The
      driver-LINQ bridge applies element conditions as `Enumerable.Where` over its plain array and leaves an outer gate UNRESOLVED (it
      fails at translation, never empty): a gated lookup is `IsNativeOnlyArray`/`IsBridgeIgnorable`, and an element leaf's gated lookup is
      rendered natively like any other (`RenderElementLeafLookup`). Each distinct gate gets its own `_fN` alias.
    EF10's GroupJoin rewrite guards every outer part, so the group is EMPTY for an outer row with a null part. EF8/EF9 keep the key whole
    (`Equals(new { s.A, s.B }, new { r.A, r.B })` behind a guard on a constructed object that is never false): member-wise, null equals
    null; `TryExpandAnonymousKeys` expands it to unguarded pairs. Each version's own rewrite is answered, so the two differ for a null
    part. A composite lookup renders `let` + `$match $expr` and can carry an element filter, ordering and paging in the same
    sub-pipeline (`LookupExpression.CompositeKeyLetAndMatch`).
  - **A keyed lookup has no TPH discriminator narrowing**, so `TryStageKeyedArrayLookup` declines a derived target.
  - **Element leaves, `ToList`/`FirstOrDefault` of the group** (`c.Orders.Where(..).OrderBy(..).Skip(..).Take(..).ToList()`,
    `.FirstOrDefault(pred)`, off a GroupJoin group, `Select(o => o.Total).ToList()`) match `NativeReferencedElementMatcher` (structural,
    shared by both sides) and stage ONE keyed array lookup, whether or not a navigation exists
    (`TryTranslateReferencedElementLeaf`, correlation via `TryMatchCorrelationKeys`, never the navigation). Filters ride in
    `InnerPredicate`; ordering, `Skip` and the limit are the typed `ElementSort`/`ElementSkip`/`ElementLimit`, rendered by `RenderLookup`
    after the filter (not `PipelineStages`: `HasPipeline` would make the lookup non-native). A filter moved ahead of the ordering is
    equivalent, but `Skip`/`Take` must be the OUTERMOST operators: a `Where` after them filters the page, so it declines. `Take` on a
    `FirstOrDefault`, a non-positive CONSTANT `Take`, a second `OrderBy` and a sort key that is not a default-stored scalar in CLR order
    also decline. `Skip`/`Take` may be a constant or a query parameter (`LookupExpression.ElementSkip`/`ElementLimit` are
    `MongoExpression`s rendered through the placeholder table, so the value is read per execution); a parameter's value is checked at
    `Build` (zero is an empty array, a negative count throws), see Paging below. Each distinct (filters, sort, skip, limit) gets its own
    `_fN` alias; parameters are told apart by name (`LookupExpression.SameElementCount`), never by value.
    - **The terminal is explicit** (`ElementTerminal`): `ToList`, `First`/`FirstOrDefault`, `Single`/`SingleOrDefault`, `Last`/`LastOrDefault`,
      `ElementAt`/`ElementAtOrDefault`, optionally under a scalar `Select` (applied to the materialized entities, as for a list). The lookup keeps what
      the terminal needs and the shaper runs the real LINQ operator over it, so empty and ambiguous groups throw as LINQ does: one element for
      `First`/`Last`/`ElementAt`, **two for `Single`** (a second match must be visible to throw), the `Skip` count or the `ElementAt` index as the lookup
      skip, and `Last` as the first element of the REVERSED ordering (`ReferencedElementShape.ReversesSort`; it needs an ordering and declines with a
      `Skip` ahead of it). Limit and skip come from one place, `NativeProjectionBinder.TranslateElementLimit/Skip`, so the emit and read sides cannot
      disagree. `ElementAt` with a `Skip` in the chain declines.
    - **Bare bodies are admitted** (`Select(c => c.Orders.OrderBy(..).FirstOrDefault())`). The emit side stages the leaf under the provisional bare alias and
      `TryBindAsBareProjection` remaps it to the derived alias; the read side resolves a null member name through the bare-body alias override.
    - **Known gap (#32): a scalar selector runs on the materialized, and under `TrackAll` tracked, entity**, so an unsaved change to an already tracked row
      is returned instead of the stored value. The `ToList` + `Select` form had it already. The fix is to read a direct scalar member from the element
      document in both materialization paths, not to materialize the entity.
    - **`ElementAtOrDefault` of a negative index selects nothing** where `ElementAt` and `Skip` throw: the `$skip` stage carries a marker
      (`LookupExpression.ElementSkipNegativeIsEmpty`) that `MongoPipelineFactory.NormalizePagingStages` turns into an always-false `$match` and strips; it is part of
      lookup identity, so the two never share an alias.
    - **`let c = <single-result subquery>` plus `c.Member` reads** is rewritten before nav-expansion (`LetSingleResultMemberRewriter`, EF11) to one
      member-selecting single-result subquery per read, all sharing one keyed lookup. Only a `let` whose every use is a member read; an `OrDefault`
      terminal needs a nullable member (a missing element's value-type member would read `default`, where EF reads null).
    - The read side finds the lookup by the member's alias in `LookupExpression.ElementLeafAliases` (written at the binder's commit),
      never by re-deriving it (`TryBindReferencedElementLeaf`, native route only). The shaper is a `CollectionShaperExpression` with NO
      navigation over an `ArrayAliasProjectionExpression`: the list is assigned plainly and never fixed up into the principal's own
      collection (it would hold only the filtered subset). A scalar `Select` is re-applied to the materialized entities. The leaf is
      not an `isArrayLeaf` (siblings such as `Count` stay readable) but sets `HasArrayProjectionLeaf`.
    - Two element leaves of one element type in a projection need distinct `Identity` (the projection member) on their
      `ArrayAliasProjectionExpression` and element `RootReferenceExpression`: otherwise they are structurally equal and the shaper
      pipeline merges them into one variable declared only in the first array's lambda.
  - **`Where` and `OrderBy`/`ThenBy` keys** use the same resolver via `NativeReferencedCollectionBinder`. Accept on "the
    resolver resolved a source", never on "it staged a new lookup" (an earlier op may have registered it). Stage nothing
    until the whole predicate/key translated, then relocate a trailing sort
    (`DeferTrailingSortPastConfirmedJoin`: one ordering must stay one `$sort`) and flip to `PostJoinOps`. Ops recorded
    earlier stay ahead (`Take(n).Where(Any(..))` pages first). A join scope or terminal operator declines.
  - **`MinBy`/`MaxBy(key).Member` as a `Where`/`OrderBy`/aggregate-operand value** (nav-expansion's `Where(corr).OrderBy(key).Select(member).FirstOrDefault()`)
    is `MongoMappedArrayReduceExpression` with `MinByOperator`/`MaxByOperator` (`TranslateReferencedOrderedFirst`): a `$map` of `{k: key, m: member}` pairs,
    a `$reduce` that keeps the FIRST pair with the smallest (largest) key (strict `$lt`/`$gt`, so a tie keeps the earlier element as LINQ does, which a
    `$sort` + `$limit` would not guarantee), and `$ifNull: ["$$best.m", null]` (`$$best.m` of an empty group is MISSING, and `$eq: [missing, null]` is false).
    An empty group answers null; LINQ throws for a non-nullable member, as for the aggregates above. Only the referenced-collection binder wires
    `ReferencedCollectionWithNestedResolver` (the projection binder does not, so projections are unchanged: the arm declines there).
    - **The key may read the collections its element is correlated with** (`5 + o.OrderDetails.Max(d => d.ProductID)`, or another `MinBy`). Each becomes a
      keyed lookup in `LookupExpression.ElementNestedLookups`, rendered as a `$lookup` stage inside the element array's sub-pipeline (after the element
      filter, before the element ordering), so every element carries the arrays the key reads; the element-relative key then reads them as `$$e.<alias>`.
      The array gets its own `_fN` alias (never the shared navigation lookup, which holds no nested arrays); `SameNestedLookups` is part of what makes two
      arrays the same. Only a plain correlation resolves in the element scope (no extra filter, no condition on a side, no derived target), the key
      and member lambdas may not read the enclosing entity (single-scope translation resolves a member by name), and the key must be CLR-ordered
      (`SortKeyPreservesClrOrdering`). A lookup with nested lookups has `HasElementStages`, so the driver-LINQ bridge refuses it, as for other element stages.
  - **The driver-LINQ bridge answers the aggregate, quantifier and count leaves itself**
    (`MongoEFToLinqTranslatingExpressionVisitor.ReferencedCollection.cs`): the terminal operator and the `Where`/`Select`/`OrderBy`
    chain under it become the `Enumerable` operators over `Mql.Field(outerDoc, <array alias>, serializer)`. It reads only PLAIN arrays
    (`IsBridgeReadableArray`) and stages its own for the query root's correlations (`StageBridgeOwnedLookups`), applying the filter as
    `Enumerable.Where`, because the binder folds the filter into the lookup it stages; that filtered array is `IsBridgeIgnorable`.
    - The correlation `Where` may sit anywhere in the chain (`TryFindCorrelationBase`); any other `Where` is an element filter and two
      outer-reading predicates are ambiguous. A query parameter is not the outer entity.
    - Never resolve a subquery correlated with a non-root document (an aggregate inside another aggregate's lambda reads the wrong
      rows) nor a set operation's other operands (`$unionWith` cannot see an array staged after the root). A set operation with any
      referenced terminal fails at translation (`MultiBranchNavigationCountFinder`), not at the server.
    - A relational comparison of a nullable rewritten aggregate is null-guarded (`VisitBinary`): the aggregation dialect orders null
      below every number, so `Max(..) < 650` would match the empty group.
    - **Element leaves are answered by the native lookup, not by driver LINQ.** The emit side stages their keyed lookups in every mode and
      the shaper reads them from the member's alias in a whole document, so the bridge renders each one as the native pipeline would
      (`RenderElementLeafLookup`: a one-stage `MongoPipelineFactory`, built with this execution's parameters, so a filter parameter is
      read per execution) and copies its array to every alias in `ElementLeafAliases` with `$set`. The bridge's constructor refuses a
      lookup that has element stages but no leaf, a join-inner predicate, and an alias that is a stored field of the root or dotted
      (the `$set` would overwrite what another leaf reads). A computed sibling leaf (`N = c.Orders.Count`) beside an element leaf cannot
      be read from whole documents and fails at compile time under `DriverLinq`; the native modes answer it.
- **Navigation-less joins** are native iff `RebindInnerShaperToOuterQuery`'s raw-key branch resolved both keys
  (`JoinInfo.Lookup != null`); a non-simple key declines. Composite-PK components live at `_id.<Name>`
  (`LookupExpression.GetFieldPath`). "Simple" includes an anonymous key of scalar properties (same anonymous type both
  sides, `StoredAlike` per pair), rendered as `let` + `$and`.
- **A join key is resolved by property NAME only if it is a direct read**, on BOTH sides (`IsDirectKeyRead`: the key
  parameter or an `Outer`/`Inner` chain, no owned/complex hop): `x => x.Ship.City` shares the root `City`'s simple name, and
  `a => a.Ref.Id` the PK's, so the raw-key `$lookup` and navigation matching silently joined on them. Hop keys decline
  natively. Selector-addressed fields elsewhere (bridge left-join keys, filtered-Include sort keys) resolve the whole chain
  (`StructuralPath.TryResolveSelectorLeaf`). On the bridge, join-result complex hops read through the complex
  serializer, and the mixed reader's `_outer` redirect applies to every hop's source (`ResolveWholeDocumentSource`).
- **Paging vs. joins.** EF hoists `Skip`/`Take`/`Where`/`OrderBy` ahead of a join's result selector; recorded ops are
  deferred until after the join unless the join is a left-outer reference navigation (reducers there decline). Paging
  both before and after a join declines; paging *between* joins declines (`HasPagingRecordedBetweenJoins`). Deferring
  paging past a non-1:1 join in a GroupBy silently changes group counts, so that declines.
- **GroupBy over a join scope** needs a `TransparentIdentifier` key parameter and is confirmed only at the binder's
  commit point. Key parts/accumulators/conditions that may read an unmatched left-outer side decline unless every
  operator propagates missing as null (`MongoGroupElementTranslator.PropagatesMissingAsNull`).
- **Post-group operators resolve by the Select's output alias** (`MongoProjectedAliasScope`), never key-part name or
  entity property. `Where`, `OrderBy`/`ThenBy` (bare alias key only) and `Skip`/`Take` are native, in `PostGroupOps`
  after the flatten `$project`; over a join they decline. An accumulator whose operand is the key itself
  (`g.Max(e => g.Key)`) reads the key part's raw per-document expression, never `_id`. A `$push` list is null-safe for
  nullable/reference elements; entity elements decline.
- **Set ops form a tree; each `Union`'s dedup belongs to its own link**, never hoisted or flattened
  (`A.Concat(B.Union(C))` differs from a left chain). Ops recorded between links go to `TrailingOps`, then move to the new
  link's `PrecedingOps` (`AppendSetOperation`). `Intersect`/`Except` never nest.
- **Projected set-op operands are read through source1's shaper.** Bare scalars with different aliases re-alias source2
  (`CanAlignBareScalarAliases`). A constant/parameter on source1 declines unless `CanRebindConstantLeafToDocument`.
  source1 may be a single-level confirmed join scope; source2 has no lookups. Operands must store each alias the same way
  (`OperandSerializationsMatch`).
- **A composite `$group` `_id` omits a missing sub-key**; `RenderCompositeKeyPart` `$ifNull`-normalizes every possibly-null
  part once. Key-only accumulator conditions use `NullSafeKeyRead`.

Rendering (null/missing/dialect semantics):

- **Every string constant/parameter in the aggregation dialect is `$literal`-wrapped** (else `"$Field"` user input is read
  as a field path). New operand positions must go through `RenderBranch` or `RenderOperand`.
- **Negation is exact complement or decline.** `$eq`/`$ne` may be inverted; `$lt/$lte/$gt/$gte` must be `$not`-wrapped
  (they don't partition missing/null). A negated `&&`/`||` is De Morgan'd by `MongoExpressionNegator` or declines, never
  an aggregation `$not`.
- **The aggregation dialect orders null/missing below every value**, so bare `$lt`/`$lte` (or `$gt`/`$gte` vs null) answer
  true where C# answers false. The renderer conjoins `$gt: [<lower side>, null]` when `MayBeNull`. Non-nullable values
  that may be *missing* (unmatched left-join side) get no guard; conditions over them decline.
- **`$expr` inside `$elemMatch` is a hard server error**; reject at `IsQueryDialectRenderable`. `$size` on a missing/null
  array also errors: `$ifNull` around `$size`/`$filter` is mandatory.
- **Relational comparisons, sort keys and aggregates run on the stored form**: over a converted or non-default-represented
  property they decline natively and the driver-LINQ bridge throws (EF-337). Shared predicates:
  `StoredOrdering.PreservesClrOrdering` and `PreservesClrOrderingForAggregateAndSort`; equality is never gated. Native
  aggregates stay on `HasDefaultKeySerialization`. The bridge walks the whole key/operand; an unclassifiable construct is
  refused if the query reads such a property.
  A numeric property stored as a string is the exception: the bridge reads it through `$convert` (`StoredValueConversion`) and
  doesn't refuse it; native still declines it.
  - **Opt-in (EF-472)**: `HasOrderPreservingStorage()` / `HaveOrderPreservingStorage()` (annotation `Mongo:OrderPreservingStorage`) and EF's
    `DateTimeToTicks`/`TimeSpanToTicks`/`BoolToZeroOne` converters count as order-preserving (`StoredOrdering.IsDeclaredOrderPreserving`,
    folded into `PreservesClrOrdering`). It is an ordering claim only: `Sum`/`Average` (bridge use `SumOrAverage`), functions
    (`PreservesClrValueForComputation`), the numeric reductions and every `HasDefaultKeySerialization` gate ignore it. The bridge honors it only
    for a bare read: the walker counts enclosing computations (`_computed`: arithmetic, `??`, `?:`, a method, a type-changing convert) and a
    relational comparison passes `allowDeclared` only against a value-only operand (`IsValueOnly`, no lambda parameter); anything else falls
    back to `...ByConstruction`. Native member-vs-value comparisons and bare-field sort/Min/Max follow the shared predicate. A new gate that
    reads a stored value must say whether a declaration is enough; default to no.
  - **A function computed from such a property is refused too** (EF-465): a string method, `Length`/date part, arithmetic
    or `Math` over it, in a filter, projection or group key. Predicate: `StoredOrdering.PreservesClrValueForComputation`
    (default storage, or a number the server holds exactly). The bridge runs it per node
    (`ThrowIfComputedFromStoredValue`, the walker with `StoredOrderingUse.Computation`); native computed leaves already
    require `AllFieldsDefaultSerialized`, and `IsRegexSearchableString` (Contains/StartsWith/EndsWith/Like/case-folded
    equality) declines any converter. Reading the property and equality stay exact. Not covered: properties of EF complex types
    (`FindMappedProperties` only sees entity types; the ordering checks share the gap).
  - **A converted bool as a conditional test is refused** (EF-466): `$cond` tests by truthiness, so the renderer
    (`RenderConditionTest`, `CanRender`) declines an `IsUnsafeTruthinessRoot` test and the bridge refuses a conditional whose test
    reads a non-preserving property.
  - **Equality or `Contains` between two stored operands must be stored alike** (EF-467, `ThrowIfStoredDifferently`): a property
    and a stored primitive collection (or its element) with different converter/representation compare stored forms, unless both
    store the CLR value (`PreservesClrValueForComputation`). A constant, parameter or captured list goes through the property's
    own serializer and is exact.
    Best-effort: operands reached through `Where`/`Distinct` receivers, anonymous projections, `g.Key` or dictionary values are not
    resolved for this check.
- **Non-default-serialized bools are truthiness-tested wrongly** (`HasConversion<string>()` stores `"True"`/`"False"`, both
  truthy): gate `$not`/`$and`/`$or` operands and bare boolean roots via `MongoExpressionTranslator.IsUnsafeTruthinessRoot`.
- **Negator, dialect classifier and both renderers must agree** (`MongoExpressionNodeCoverageTests` checks every
  `MongoExpression` subtype x dispatcher). It is blind to shape-conditional dispatch (e.g. `MongoRegexExpression`
  constant vs field-to-field); those need dedicated tests.
- **String operators that don't propagate null are null-guarded at render** (`NullPropagating`; `$concat` coalesces to
  `""`). A non-nullable projection leaf over a guarded `Length`/`IndexOf` is flagged `MongoProjection.ThrowsOnNull` and
  read as `T?` (throws "Nullable object must have a value."); one whose null an operator may absorb (`$max`/`$min`,
  `Sign`) declines (`ClassifyNonNullableValueRead`). Group keys, accumulators and `Min`/`Max`/`Average` over such values
  decline rather than read null as `0`, including via an upstream alias (`MongoElementRefExpression.ThrowsOnNull`).
- **`$toLower`/`$toUpper` are never emitted natively** (ASCII-only): `Where` becomes an anchored case-insensitive regex;
  `Select` projects the raw string and re-applies the mapping client-side. After such a leaf
  (`HasClientCaseMappingProjectionLeaf`) value-reading operators decline. A GroupBy query that would run on driver-LINQ
  with a case mapping anywhere in its chain throws (`ThrowIfDriverLinqCaseMapsGroupedResult`); ungrouped driver-LINQ
  queries still emit `$toUpper`/`$toLower`.
- **A retained pushed-down Select (`Select(..).Distinct()` / set op beside an owned reference) returns projected documents keyed by member
  name** (EF-469): the mixed reader reads an owned reference through the member that selects its navigation
  (`ResolveProjectedOwnedReferenceAlias`) and a bare root scalar through its alias (`CreateGetPropertyValueAtPath`); a server-evaluated
  case mapping there fails loudly (the server answers `""` for null). Not covered: a retained Select over a Join.
- **DateTime read-back must honour the property's `DateTimeKind`** (`NativeDateTimeKindReadBack`): untraceable values,
  whole-document class-map reads with kind-sensitive members, and set ops whose operands differ in kind decline. Don't fold
  this into `HasDefaultKeySerialization`. Date parts over Local-kind properties compute in UTC on both paths (EF-459).
- **Clock members (`DateTime.Now/UtcNow/Today`, `DateTimeOffset.Now/UtcNow`) are never baked into the template.** A maximal
  closed subtree holding one becomes a `MongoParameterExpression` with a `RuntimeEvaluator`, evaluated per `Build`; one
  predicate (`RuntimeClock.IsRuntimeEvaluable`) gates `IsSimpleValue` and the factory. `TryEvaluateClosedSubtree` declines
  any clock.
- **Local-collection folding never folds a non-deterministic call or a clock member**
  (`LocalCollectionFilterFoldingVisitor`). The fold runs in the preprocessor for every mode, so a folded
  `Guid.NewGuid()`/`DateTime.UtcNow` would be baked into the cached plan.
- **A non-deterministic call must stay in the tree for `NonDeterministicCalls` to refuse it.** EF's default filter only keeps
  `Random.Next` and `Guid.NewGuid`; `MongoEvaluatableExpressionFilter` keeps every call `IsNonDeterministic` recognizes
  (`NextDouble`, `NextSingle`, `NextInt64`, `Guid.CreateVersion7`), or EF parameterizes it and every row shares one value (EF-464).
- **Stored data that contradicts the model is not recovered from.** A string in an `int` property throws from the driver's batch
  deserialization (it deserializes the whole batch before yielding row 1, as under driver-LINQ), as EF Core does when a column breaks
  its declared shape. The native serializers hold no per-row reader bookmark, `try`/`catch` or deferred row failure; don't add one back
  (it cost ~130 B/row on every streamed query). Tracking is still deferred to the enumerator (`BeginDeferral`).
- **Read modes: how a non-nullable value read back treats null and MISSING.** Driver-LINQ is the oracle: a malformed
  document must answer what it answered there, never a silent `0`. Emit side decides once per value leaf
  (`MongoAggregationExpressionRenderer.ClassifyNonNullableValueRead`; projected Distinct over a bare field uses
  `ClassifyMalformedFieldRead`):
  - `Plain`: a bare field leaf, read through its property (D-F10). Non-nullable fields are never flagged on the emit side
    (it would decline `Min`/`Max`/`Average`/group keys over every `x.A + x.B`).
  - `ThrowOnNull`: every operator between a null and the leaf propagates it (`Length`/`IndexOf`, date operator over a
    nullable date, `x.Score.Value + 1`): read as `T?`; an operator that may absorb the null declines. Group
    keys/accumulators over a date operator over a nullable date decline (EF-461); `x.Score.Value + 1` there still reads `0`.
  - `ThrowOnMalformedNull`: computed leaf over a non-nullable field (`x.Rank + 1`), null only when a document omits the
    field: strict on the `Projection` route; read side only (emit-side callers treat it as `Plain`).
  - `DefaultOnMalformedMissing`: the leaf selects the field itself through `$cond`/`$ifNull` (`x.Rank > 1 ? -1 : x.Rank`):
    MISSING reads `default`, null throws, only where native answers MISSING exactly as the driver (`MayAnswerMissing`); a
    widening the translator dropped (the driver's `$toLong` answers null) makes it strict (`ReclassifyMalformedReadAs`).
- **D-F10: on the `Projection` route a MISSING bare stored scalar reads `default(T)`, as driver-LINQ's `$project` push-down
  did; an explicit BSON null throws** (a `bool` reads false). Covers root leaves
  (`BsonBinding.GetScalarProjectionValueAtElement`) and join-scope leaves (`ReadsJoinScopeBareScalarThroughProperty`).
  Whole-entity reads stay strict. The exception TYPE for malformed stored data is not contract; value-vs-throw is.
- **A null parameter for a non-nullable property serializes as BSON null** (`BsonValueSerializer.SerializeNullAware`), so
  `x.Rank == p` with `int? p = null` matches null/missing rows, as driver-LINQ did.
- **`StructuralPath` is the single walker of mixed owned/complex hop chains for the native translator's member-chain and
  field-path resolution, and for join/sort keys** (`TryResolve`; `TryResolveCollection` for a chain ending in an owned or
  complex collection; `TryResolveSelectorLeaf` for selector-addressed keys; `IsComplexElementScope` for "is this a complex
  element scope"). Don't add another walker there or resolve a hop by simple name; a failed resolve's partial segments are
  not usable. Collection hops without a quantifier decline. Other trees have their own hop walkers, kept in step with it by
  hand: the bridge's `FindComplexHopTypes`/`FindComplexCollectionElementTypes` (`...LinqTranslatingExpressionVisitor.StoredOrdering.cs`,
  also used by the empty-list coalesce), `MongoProjectionBindingExpressionVisitor.ResolveHopType`/`ResolveComplexHopType`,
  and `MongoProjectionBindingRemovingExpressionVisitor.TryResolveComplexPropertyDocument` (read side).
- **A binder that can't bind a selector node fails the translation, never `default(T)`**:
  `MongoProjectionBindingExpressionVisitor.TranslationFailed` (set where `MatchTypes` receives a null binding and substitutes
  a default stand-in; reset per `Translate`) makes `TranslateSelect` decline (NativeOnly throws, Native falls back). Silent null rows were the
  bug class it closes.
- **A new `MongoExpression` node must be handled in every consumer switch** (both renderers, `IsQueryDialectRenderable`,
  `CanRender`, the negator, `MongoFieldPrefixRewriter`, projection-binder gates); `MongoExpressionNodeCoverageTests` fails
  for an unhandled node (complex types added `MongoElementNullCheckExpression` and `MongoCurrentElementNullCheckExpression`).
  The read-side EF extension nodes `ComplexValueProjectionExpression` and `ComplexPropertyMaterializationExpression` are
  outside that matrix: every shaper visitor that walks a shaper (DOM, one-pass, mixed reader) must lower or reject them.
- **Complex-property leaves are stored scalars at a dotted path** (`StructuralPath`). The read side resolves the same
  hops (`MongoProjectionBindingRemovingExpressionVisitor.TryResolveComplexPropertyDocument`), so a leaf reads through its
  `IProperty` (D-F10, serializer); the EF-337 bridge refusal walks them too (`FindComplexLeafProperties`). A bare dotted
  field (owned, complex or composite-key hop) takes the Synthetic `_v` tier (`TryDeriveSyntheticAlias` gate 1f; this
  replaced the EF-362 owned-hop decline). The EF-362 bug (a dotted alias read back as a literal key but rendered
  nested) can't recur: `_v` is never dotted and is what the driver's un-stripped fallback writes. Converted dotted
  fields stay declined (gate 1f checks `HasDefaultKeySerialization`).
  A field under an optional complex parent is MISSING whenever the parent is null/absent: ordinary data, so a projected
  `Distinct` key over a required reference-typed scalar (`string`, `byte[]`, primitive collection) is marked (`NativeGroupByBinder.IsRequiredReferenceTypedFieldKey`).
- **Complex values are materialized by one builder** (`Visitors/ComplexTypeMaterializationBuilder`). EF inlines each
  complex property's construction into the entity materializer, reading its leaves as value-buffer columns;
  `MarkComplexPropertyAssignments` replaces each with a `ComplexPropertyMaterializationExpression`, which the DOM shaper
  and the one-pass rewriter both lower through `Build` over the property's own stored element (one-pass reads that element
  whole off the reader into a `BsonValue` local, reset per pass). A projected whole complex value (`c.Address`,
  `c.Address.Location`, a complex collection) is a `MongoElementRefExpression { ComplexValue }` leaf on the emit side
  (`TryTranslateComplexValue`; a dotted one takes `_v`, gate 1g) and a `ComplexValueProjectionExpression` on the read side
  (`ResolveComplexValueProperty`), which keeps it off the driver's typed push-down (`ComplexTypeSerializer` can't
  deserialize). Read rules are strict, like a whole entity, not D-F10: a REQUIRED complex property missing or BSON null
  throws ("... is missing/null for required complex property ..."), never a null instance or default struct; an OPTIONAL
  one (EF10) reads null; `{}` is an instance; leaves use the entity-member read (`BsonBinding.CreateGetValueExpression`);
  a required collection missing/null reads empty; a wrong BSON type throws `FormatException`. The projected document is
  the STORED form and the driver can't read a complex value back, so EVERY value-reading operator over a projection that
  reads a whole complex value anywhere throws `NotSupportedException` ("... cannot be the operand of '<Op>' ...") in every
  mode (`ThrowIfComplexValueOperand` in `VisitMethodCall`): Distinct (incl. `Distinct().Count()`), Union,
  Concat, Intersect, Except, Contains, Cast, Select/Where/OrderBy written after a paging operator, Min/Max/Sum/Average
  without a selector, Join, ElementAt, ElementAtOrDefault, DefaultIfEmpty, etc. Only `IsProjectedValueFreeOperator`
  passes: Take, Skip, predicate-less First/Single/Last(OrDefault), Count, LongCount, Any. ElementAt/ElementAtOrDefault/
  DefaultIfEmpty read no value but stay refused: none has a native translation even for a scalar projection, and the
  fallback cannot read a complex value (EF8/EF9 reject DefaultIfEmpty first with EF's own message;
  `ComplexValueElementOperatorTests`). EF folds Where/OrderBy/All/Min/Max(selector) written directly after
  the Select onto the source, so those never see the projection (native, correct). The flag
  (`HasComplexValueShaperLeaf`) is set by ONE predicate (`RecordComplexValueShaperLeaf`) on the result of every
  `TranslateSelect` arm (shaper holds a `ComplexValueProjectionExpression`, or the native projection staged a complex
  value). A positional-ctor/container projection never stages a complex argument (`IsScalarPositionalConstruction`: its
  index shaper would read it through a class map); it declines. On the fallback (mixed reader, whole documents) a
  construction whose arguments are all whole complex values is served, and one mixing a complex value with a scalar is
  refused by the member-less construction refusal (`AliasedConstructionMembers`). A one-argument record/constructor of a
  whole complex value (`new Holder(x.Home)`, renamed members too) and member-init DTOs are served in every mode
  (`ComplexValueConstructorProjectionTests`, `ComplexValueSameTypeArgumentTests`). **A whole-document read of a complex
  value resolves from the node's own `ComplexValueProjectionExpression.Source`, never from the registration under its
  projection member**: a member-less construction's or method call's arguments share one member whose registration is
  the last argument's, so `new P(x.Home, x.Work)` read `W|W` (fixed).
  GroupBy in which a whole complex value takes part (key, key part, read off the grouped elements in a post-group Select:
  `ThrowIfGroupByOverComplexValue`) is refused with the same message naming GroupBy. Where EF erases the value
  (`Select(c => c.Address).GroupBy(a => a.City).Select(g => g.Key)` becomes a leaf-key grouping over the entity; the
  `GroupBy(key, resultSelector)` overload) the outcome is the pre-existing one of its scalar analogue. Complex types are new
  in this slice, so none of this changes released behaviour.
  The wording of every complex-type refusal (model validation, bulk setters, materialization, the whole-value operand and GroupBy
  refusals, the null-element scanner and bulk allow-list, the serializers) lives in `ComplexTypeRefusals`; call sites pick the
  exception type. Tests assert on the text, so edit it there, not inline.
  The query root's own shaper (bound to THIS query's empty projection member) resolves to the root document even when
  typed as a derived type after `OfType` and even in a join (`IsRootProjectionShaper`, by binding not CLR type); any other
  derived shaper does only outside a join. This also corrected a pre-existing NON-complex wrong read (an intentional
  behavior fix): `OfType<E>().Select(e => new { e, e.Name, R = e.Referrer!.Name })` read `e.Name` off the joined
  referrer at 040cecdf (`Dev|Boss|Boss`, and so did published 10.0.4), now `Dev|Dev|Boss`. A shaper of the root's OWN CLR
  type in a join is the root only through that binding (`!IsJoinQuery || IsRootProjectionShaper`): a same-type joined row
  (self-join inner side, same-type reference navigation, a BASE-typed referrer after `OfType`) projected beside the whole
  outer entity reads the joined document (an intentional behavior fix; every released version read the outer document).
  A bare-nav SelectMany's ITEM shaper
  (`BuildBareNavWrappedShaper`) is bound to the same empty member, so `IsRootProjectionShaper` is true for it too; this
  is harmless today because the arm is never reached for a served row (an item type in the root's EF hierarchy needs a
  reference unwind, which has no driver-LINQ fallback; the native path binds by scope depth and reads by alias; an owned
  element type is never in the root's hierarchy). Pinned per mode in `ComplexTypeDerivedSelectManyItemTests`; if a
  reference-unwind fallback is ever added, distinguish the item shaper first.
- **A whole complex value in `==`/`!=`/`.Equals` is owned by `MongoExpressionTranslator.ComplexEquality.cs`** (its decline is
  final): member-wise, never by example. Null/missing of a value is `MongoElementNullCheckExpression` (`{p: null}`, `$ifNull`
  in `$expr`; flips exactly); each comparison is built with its complement (`EqualityPair`), so negation never wraps; stored
  pairs need the same mapped members (both orders agree) and `StoredAlike` leaves; a captured comparand is read per execution
  (`RuntimeEvaluator` parameters). A value with ANY optional ancestor (owned or complex) equals an instance only when its
  own element is present; leaves are `NullSafe` so element scopes agree with the null check.
- **Complex COLLECTIONS (EF10) share the owned-collection element machinery through one resolver**
  (`StructuralPath.TryResolveCollection` → `TryResolveEmbeddedCollectionPath`): the array path plus the element's structural
  type (`IComplexType` for a complex collection); the translator's scope is an `ITypeBase`, so an element-scoped predicate
  resolves members (and nested collections) against the element's complex type. Entity-only arms (key equality, `$$ROOT`,
  type folds) are gated on `IsSelfParamTheEntity`, false in a complex scope. EF10 stores a null ELEMENT as BSON null, and
  `$elemMatch` never matches a non-document element, so a complex-collection quantifier always renders in an aggregation
  element scope (`$anyElementTrue`/`$allElementsTrue` over `$map`, `RequiresAggregationElementScope`); owned collections keep
  `$elemMatch`. A null complex element reads every leaf as null. Field reads in a complex element scope are
  `NullSafe` (`ScopeField`) and rendered through `Render(field)` (`$ifNull`), incl. `$in` needles and `UtcDateTime`; the
  DateTimeOffset `$dateAdd` reconstruction and the array inputs read raw paths, safe because they propagate null / are
  `$ifNull`'d to `[]`. `MayBeNull` is structural for the null-propagating nodes (NullSafe fields/refs, arithmetic, convert,
  math, date part, DTO local, and date-add: start or amount; coalesce: both sides; conditional: either branch)
  with the CLR-type check as fallback, so relational comparisons over them get the null guard. Those operands are judged
  by `MayBeNullOperand` (a captured non-nullable parameter is never null), so root queries over captured values render
  the base MQL. A non-nullable,
  default-serialized bool leaf compared as a value reads `$ifNull: [f, false]` (`ScopeValue`), so `== false`/`!= true`/
  `== p` agree with `!a.Verified` (true for a null element; two-valued; no owned pin exists); a `bool?` keeps null semantics;
  owned element scopes keep the RenderBinary no-`$ifNull` rule (a missing owned leaf is malformed data, a null complex element
  is EF-written). Driver-LINQ answers such rows differently (pinned per mode). A query the native path
  DECLINES whose complex-element predicate holds a null-guard-requiring shape (relational, `==`/`!=` null, local-list
  `Contains` of a member, a `Contains` over a primitive-collection member of the element that the native path declined (a non-constant, non-parameter item, or an
  unsupported array; the driver's `$in` over a null element's missing array fails the whole query, #40), non-nullable bool equality) is refused at the compile-time gate under `Native` instead of falling
  back to those wrong rows (`ComplexElementNullGuardRefusal`, called where the gate commits to the fallback; structural,
  so it refuses even over data with no null element; explicit `DriverLinq` runs the driver; owned scopes untouched).
  It is a BEST-EFFORT net for structurally keyable shapes, not a guarantee. The scanner RESTATES the node kinds
  that need the null-element guards (`ScopeField`/`MayBeNull`/`ScopeValue`) rather than calling those predicates (design debt): a
  new null-guard-requiring node kind must be added to both, and to the bulk allow-list's refusal side. Documented limits, pinned as
  characterization in `ComplexCollectionNativeQueryTests.Known_limits_of_the_null_element_refusal_scanner_...`: element predicates
  reached through a navigation/join root (`s.Store.Tags.Any(t => t.Rank < 1)`: lambda roots are keyed by
  `FindEntityTypes(param.Type)`, never a `TransparentIdentifier`); element-leaf `Select` chains
  (`Select(s => s.Verified).Contains(false)`; `Select` is opaque to the keying); null tests not spelled against a null
  CONSTANT (`Zip.HasValue`, `Zip == nullParameter`, `string.IsNullOrEmpty(Note)`, `Note == r.NullableRootMember`); an element
  read inside a nested lambda whose result feeds a relational (`Select(s2 => s.Floor).First() < 1`: the operand check stops
  at lambdas so `Count(s2 => s2.City == s.City) >= 1` isn't refused). Indexed overloads and `SelectMany` over a complex
  collection are refused by EF in every mode. In those limits the default mode serves the driver's rows. The structural
  cost: a keyed shape is refused even over clean data and even where the driver's rows would have been right
  (`s.Tags.Count(...) >= 0`). The same net refuses a complex COLLECTION itself compared with null
  (`r.Detours == null`, `!=`, in a predicate, projection, ternary or sort key; keyed through the scanner's structural
  resolver, owned collections untouched): native declines it, and the driver's `{Detours: null}` also matches `[null]` (and,
  for a required collection, a missing/null array that reads empty). Remedies the message names: `!c.X.Any()`/`c.X.Count == 0`
  (native), or explicit `DriverLinq`.
  `c.Lines.Contains(x)` / `Any(l => l == x)` is member-wise equality with the element as the value
  (`MongoCurrentElementNullCheckExpression`, `$$e`), `!Contains` its exact `All` complement. The bridge coalesces a REQUIRED
  complex collection used as an operator source to `[]` (`TryRewriteRequiredComplexCollectionSource`; it reads empty, and
  the driver's `$size`/`$map` over null is a server error; it also serves bulk ExecuteUpdate/Delete); an optional one, or a
  `T[]`-typed one (no `List<T>` fits), is not coalesced and stays a loud server error on the fallback. EF-337's walker resolves an
  element parameter of a complex collection to its complex type (`ComplexCollectionElementExpression`), so a relational
  comparison over a converted element leaf is refused on the bridge.
- **`TranslateOperand` may return an enum-typed `MongoFieldExpression` for `(int)x.E`** over a default-serialized
  enum field (`IsEnumUnderlyingRelabel`: the stored value is already the integer), so an operand's `Type` may be the
  enum, not the cast target. Callers comparing or reading by type must allow for it.
- **A null parameter for a non-nullable property serializes as BSON null** (`BsonValueSerializer.SerializeNullAware`),
  never through the property's non-nullable serializer, so `x.Rank == p` with `int? p = null` matches the null/missing
  rows, as driver-LINQ did.

Shapers and projections:

- **The mixed reader reads a computed scalar leaf whole** (`IsClientComputedLeaf`, `TryBindClientComputedLeaf`);
  operand-by-operand binding makes every operand read as the last one bound. Ambiguous member-less construction arguments
  are recorded in `AliasedConstructionMembers` and the mixed reader declines them loudly.
- **A ternary over constructions is read whole only if its type's class map reads each member by name**
  (`IsMisreadWholeValueLeaf`, called by every binder staging a ternary/coalesce leaf; `Id` maps to `_id`, so
  `new P { Id = ... }` would throw FormatException). Otherwise only the test is staged and the shaper evaluates the
  ternary (`TryCollectClientConditionalBranches`). Branch reads are staged as `_cr<N>` and the read side takes each alias
  from the staged read (`TryGetClientConditionalReadAlias`), never the member path. `HasClientConditionalProjectionLeaf`
  declines set ops, value-reading operators and `Distinct`.
- **Client-only bodies over the whole entity** (client method on it, a combinator around one, or a construction with a
  whole-entity operand) fetch whole documents (`HasClientWrappedWholeEntityShaper`, so set ops decline). Binder and shaper
  gate both call `NativeClientWholeEntityShape`; at least one whole-entity operand is required (`new[] { x.Id }` must not
  match).
- **Positional containers of scalars** (`new[] { x.A, x.B }`) are `_ctorArg<N>` leaves read by index
  (`HasPositionalCtorProjectionShaper`); both sides peel the boxing `Convert` with
  `NativeProjectionBinder.UnwrapContainerElementBoxing`. A whole-entity element never takes this path
  (`IsScalarPositionalConstruction`). Containers are an opt-in of `TryGetProjectionMembers` (`allowContainerElements`,
  plain-`Select` callers only); never widen `allowPositionalConstructorArguments`, which `GroupBy`/`SelectMany` result
  selectors share. `new List<T> { ... }` is not a container.
- **An element `Select` over a root-declared owned collection** (`e.Kids.Select(k => k.W).ToList()`) is staged as the
  collection's whole array (`TryMatchOwnedArraySelectLeaf`, the one predicate both sides call); the shaper re-applies the
  `Select` over materialized owned entities. The lambda must be self-contained (`IsSelfContainedLambda`). A lambda reading
  the outer entity crashes shaper build in every mode (pre-existing).
  - **The streaming shaper evaluates such a Select in place** (`StreamedCollectionSubstituter.TryStreamElementSelect`,
    `MongoElementSelectorRewriter`): when the lambda reads only scalar members of its element, no element (nor its nested owned
    types) is constructed, and the lambda runs inside the driver's batch deserialization. So it must be side-effect free: member
    reads, operators, conditionals, anonymous types, and `string`/`Math`/`Nullable<T>` methods only; anything else declines to the
    whole-array read. Once that shaper is chosen the pipeline is narrowed to the scalars the lambda reads
    (`MongoSelectDefinition.TryNarrowArrayProjection`, factory rebuilt), so a stored element missing a required scalar the lambda
    does NOT read no longer throws; the pruning above never drops a scalar otherwise.
- **`Mql.Field(p, "element", serializer)` resolves like a mapped field** (`TryResolveMqlField`, shared by Where, OrderBy,
  Select, aggregate selectors) by STORED element name. The serializer argument is ignored (can't be inspected at compile
  time), so a converter/non-default representation or a CLR-type mismatch declines.
- **A nested construction that is not all plain top-level fields is flattened** into the enclosing projection
  (`FlattenNestedConstructions`): each member becomes a leaf aliased by its LAST name; an all-plain nest stays a
  sub-document leaf. Two members sharing a last name collide in `seenAliases` and decline.
- **Mixed-projection alias agreement.** An array/owned-nav projection leaf's alias must equal the navigation's document
  path, or a fallback silently returns an empty collection. A member NAMED differently, or an element renamed with
  `HasElementName`, takes the path as alias plus a `DocumentPath` alias override. The same rule applies to a dotted scalar
  beside an array leaf; a path that equals/prefixes/extends another alias declines (`ProjectionPathsCollide`).
- **Owned-entity equality** (`p.Home == x`, `Visits.Contains(x)`) is a per-property conjunction over every CLR-mapped
  non-shadow property (driver-LINQ's rewrite), not a sub-document match; `ElementParam` (not `SelfParam`, which enables
  root-entity arms wrong inside `$elemMatch`) marks the element operand. Parameter comparands over all-nullable
  properties, owned == owned, TPH-owned and outer-scoped operands decline.

## Conventions and traps

- **Layering:** Query never calls `IMongoCollection<>.Aggregate` (Storage executes via `MongoClientWrapper.Execute`),
  never instantiates serializers (`BsonSerializerFactory`), and only reads metadata. The lowerer is BSON-free. Bulk writes
  cross to Storage as behavior (`MongoBulkPlan` delegates), not data.
- **Adding a native operator:** slot-style needs `NativeSlotPopulator.PopulateNativeSlots` and
  `IsNativeRepresentableSlotOperator`; a `Translate*`-override operator needs the override and
  `IsNativeRepresentableSlotOperator`. QMTEV's `TranslateWhere/OrderBy/ThenBy/Skip/Take` are deliberately inert
  (`=> null`); routing them through the switch double-populates slots.
- **Paging:** MongoDB rejects `{$limit: 0}`, so `Build` rewrites it to an always-false `$match`; negative values throw.
  Normalization covers top level, `$unionWith` and `$lookup` sub-pipelines (an element leaf's parameterized `Take`).
- **One discriminator test for `OfType<T>`, `x is T` and `x.GetType() ==/!= typeof(T)`**
  (`MongoDiscriminatorPredicate.TryBuild`): values go through the discriminator property's serializer, and an abstract type
  contributes no value (its `null` in `$in` would match a document with no discriminator). A `GetType()` test against a
  type outside the hierarchy folds to false (EF-202: driver-LINQ answers every row). Root-level, one-scope only. A member
  declared only on `TDerived` resolves by the lambda parameter's own type after `OfType<TDerived>`;
  `((Derived)e).Member` is not narrowed and declines.
- **TPH `$lookup` to a derived type must narrow by discriminator** (in `LookupExpression`'s constructor). The kind
  `LookupPipelineKind.DiscriminatorNarrowed` is write-once; native for a top-level collection Include only. Reference
  navigations, renamed-to-avoid-collision lookups and correlated reducers still decline.
- **Primitive collections, dictionaries and composite-key `Contains` are query-dialect only**, except `arrayField.Contains(item)`
  (`MongoArrayContainsExpression`), which also has an aggregation form `{$in: [item, {$ifNull: [<array>, []]}]}` (negation wraps `$not`).
  The `$ifNull` matters: `$in` over a missing array is a server error, and a NULL complex element reads every member as missing (#40).
  - The item is a constant or a query parameter of exactly the element type, serialized through the array's ELEMENT serializer. A
    converted or whole-collection-serialized array declines.
  - Outside a complex element scope the item must be unable to be null (a null constant, a string or nullable parameter decline),
    because the query dialect's `{f: null}` also matches a missing field.
  - Inside one (always the aggregation dialect, `ScopeField` NullSafe) `$in: [null, array]` is exactly `list.Contains(null)`, so
    null-capable items are translated.
  - `Any()`/`Count()`/`Length` over a primitive-collection PROPERTY reuse the owned size forms
    (`TryResolvePrimitiveCollectionPath`); a missing/null array counts as empty. `Any(pred)`/`All(pred)` stay declined.
  - A string-keyed dictionary entry is the sub-field `<dict>.<key>` (`MongoDictionaryEntryExpression`, serialized by the
    dictionary's VALUE serializer, hence `MongoParameterExpression.ExplicitSerializer`); `ContainsKey` is `$exists`. Only a
    constant, dotted-path-addressable key (not empty, no `.`, no leading `$`, no NUL). The aggregation dialect
    deliberately has no arm: a bare `$lt` over a possibly-missing key answers true there.
  - `entities.Contains(e)` over a composite key is a per-entity `$or` of `_id.<Key>` conjunctions
    (`MongoCompositeKeyInExpression`), never an order-sensitive `_id: {$in: [docs]}`; a parameter list is built per
    execution (`CompositeKeyClauses`). An empty list emits one always-false clause (`$or: []`/`$nor: []` are server errors).
- **Spec scalar functions.**
  - Integer `&`/`|`/`^`/`~` over int/long are `$bitAnd`/`$bitOr`/`$bitXor`/`$bitNot` inside `$expr` (server 6.3+).
    `MapBitwiseOperator` is separate from `MapArithmeticOperator` (shared with `SelectMany`). A bitwise Select/GroupBy LEAF
    is deliberately not admitted; boolean `^` declines.

## Testing

- **MQL shape cannot prove a query went native**; use `MongoQueryMode.NativeOnly` (`MONGODB_EF_QUERY_MODE=NativeOnly`
  flips every spec and functional context; `MONGODB_EF_NATIVE_ONLY=1` is an alias).
- For result-affecting native shapes, write a `[Theory]` comparing native results to an in-memory LINQ oracle over
  ragged/missing/null/empty data (`NativeOwnedCollectionAllTests`, `NativeModeAssert`).
- MQL baselines are regenerated (see SpecificationTests `AGENTS.md`). Tests: `tests/…UnitTests/Query/` (native in
  `NativeTranslation/`), `tests/…FunctionalTests/Query/`; filter `FullyQualifiedName~Query`.
