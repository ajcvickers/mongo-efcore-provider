# Shaper Layout Rework — Design

**Date:** 2026-06-16
**Status:** Phase 0 → most of Phase 1b implemented on branch `shaper-layout-rework` (verified-green checkpoint `40faee3`). Full heuristic elimination deferred to a follow-up — see `2026-06-16-shaper-layout-rework-STATUS.md` for what landed, what is heuristic-served, and the FK-join entry point.
**Scope of this spec:** Phase 0 → Phase 1b, EF10 only. EF8/EF9 backport and Phase 2 (path normalization, shaper-path unification, new query-feature shapes) are explicit follow-on specs.

## Background

The MongoDB EF Core provider sits on top of the C# driver's LINQ v3 provider. EF Core hands the provider an expression tree; the provider decides what to push to the driver versus shape client-side, and compiles a **shaper** that turns each returned `BsonDocument` into the requested CLR shape.

The query pipeline currently has three shaper paths (`MongoShapedQueryCompilingExpressionVisitor.VisitShapedQuery`, lines 74–117):

- **Push-down** — no entity references in the projection; the driver does everything and the client shaper is the identity function.
- **Mixed** — the projection mixes entity references and scalars; the `Select` is stripped, the driver returns whole `BsonDocument`s, and `MongoMixedProjectionBindingRemovingExpressionVisitor` re-projects client-side.
- **Entity** — the result is an entity type; whole documents are shaped into tracked/untracked entities by `MongoProjectionBindingRemovingExpressionVisitor`.

### The problem

In the mixed and entity paths, the client shaper **re-derives where each value physically lives in the returned document** using heuristics rather than being told. The relevant code:

- `MongoProjectionBindingRemovingExpressionVisitor.GetCrossCollectionFieldName` (line 505): returns `"_inner"` or `accessExpression.Name` depending on the `UsesDriverJoinFields` flag.
- `GetCrossCollectionRootDocument` (line 516): redirects a collection-element nested reference to the element document.
- The `UsesDriverJoinFields` branches that read `"_outer"`/`"_inner"`/`"_lookup_<Navigation>"` (lines 287–310, 574–584).

`UsesDriverJoinFields` is described in-code (line 497) as "the single source of truth," but it is a **computed flag the shaper re-derives** — not an authored record of the layout the translator actually produced. Because the layout is reconstructed rather than recorded, a mismatch between what the translator emitted and what the shaper expects produces **silent wrong data**, not a crash.

This is the dominant, recurring failure class in the cross-collection Include work (EF-117). The working notes for the ComplexNav port catalogue it repeatedly: phantom references with `Id=0`, off-by-one nested references, `_inner._lookup_<Nav>` document synthesis defeating the null-guard, and self-ref includes that "need architectural rework." Each was a layout reconstruction the shaper got wrong.

### Why this is the highest-leverage change

A provider-wide analysis (query architecture, unsupported-operation census, spec-test failure census, non-query areas) found the two largest blocked themes are GroupBy (292 baselined spec failures) and cross-collection queries (~636 across several tickets). Both are blocked at root by the same thing: the rigid projection-binding/shaper contract cannot express novel output shapes, and the layout reconstruction is fragile. Fixing the shaper's relationship to layout is the substrate beneath both — it de-risks the existing cross-collection work and is the precondition for the new-feature work, rather than competing with either.

## Goals and constraints

Driven by the brainstorming decisions:

- **Primary goal:** *de-risk first, then enable* — establish a single source of truth for where values live (killing the silent-wrong-data class), and treat that as the foundation new features build on. Two phases, one architecture.
- **MQL stability:** the emitted pipeline **stays frozen in Phase 1** (this spec). Pipeline normalization that changes MQL is deliberately deferred to Phase 2, where the ~1,100 baselines may be rebaselined via `EF_TEST_REWRITE_BASELINES`. Freezing MQL in Phase 1 is the behavior-preservation proof: any green→red is a real regression, not churn.
- **EF-version scope:** develop and prove on EF10 (the most-complete line); EF8/EF9 backport behind `#if` guards is a follow-on. EF-X020 (the EF8/EF9-only cross-collection gap) may clear as a side effect of the backport but is not a goal here.

## The core abstraction: `DocumentLayout`

A single new concept — an immutable descriptor tree, **authored by the translator** at the moment it builds each pipeline stage, recording where every value physically lands in the returned document. One node per shaped slot:

```
DocumentLayout
 ├─ kind: Entity | Navigation | Scalar | Collection | (Grouping, … added in Phase 2)
 ├─ path: BSON path to read from, RELATIVE to the parent node
 │        ("" = root; "_inner"; "_lookup_Orders"; …)
 ├─ absolutePathOverride: optional, for the _outer/_inner sibling case (see Data flow)
 ├─ entityType / property / navigation: the metadata the shaper needs
 └─ children: DocumentLayout[]  (mirrors the include/projection tree)
```

**The inversion.** Today: translator builds pipeline → discards layout knowledge → shaper guesses it back. After: translator builds pipeline **and emits the matching `DocumentLayout`** → shaper reads it. The path is written exactly once, by the component that decided it.

**Relationship to EF's machinery.** `DocumentLayout` does **not** replace EF Core's `ProjectionBindingExpression` / `ProjectionMember` / `_projectionMapping`. EF's base classes still need those for change tracking and fixup. The layout sits *underneath* them, answering only "given this binding, what is the document path?" — the question the heuristics answer today. During the transition `_projectionMapping`/`_projection` and the layout coexist; the layout is additive.

## Component boundaries

Strict producer → carrier → consumer flow. The safety contract: **only the producer writes paths; the consumer may only read them.**

1. **`DocumentLayout` (new)** — the descriptor type. Depends only on EF metadata (`IEntityType`, `IProperty`, `INavigation`) and primitives — no visitor, no `BsonDocument`, no driver types. Lives in a new `Query/Layout/` folder. Dependency-light by design, so it is unit-testable with no database.

2. **Producer — `MongoProjectionBindingExpressionVisitor` (+ `.Lookup.cs`) (changed)** — already the component that decides `_outer` vs `_inner` vs `_lookup_<Nav>` and registers lookups on `MongoQueryExpression`. Change: at each placement decision it **also emits the matching `DocumentLayout` node**. No new decisions are introduced — existing decisions are captured at the one site they are made. This is the only code permitted to author a path.

3. **Carrier — `MongoQueryExpression` (changed)** — gains a `DocumentLayout? ResultLayout` property next to `CapturedExpression`. A dumb container: it neither computes nor interprets the layout. `_projectionMapping`/`_projection` remain during transition.

4. **Consumer — `MongoProjectionBindingRemovingExpressionVisitor` (changed); `Mixed` subclass folded in (Phase 2)** — the shaper. Change: `GetCrossCollectionFieldName`, `GetCrossCollectionRootDocument`, and the `UsesDriverJoinFields` branches (lines 505–516, 574–584, 287–310) are **deleted** and replaced by "look up the path for this binding in `ResultLayout`." The consumer loses all path-computing code. In Phase 2, because the entity and mixed paths now resolve paths identically, `MongoMixedProjectionBindingRemovingExpressionVisitor` collapses into the base and the three-way split in `VisitShapedQuery` becomes one path.

**Untouched in Phase 1:** `MongoEFToLinqTranslatingExpressionVisitor` (pipeline/MQL stays stable — the layout must *match* what it already emits) and the push-down path (no client shaper to fix there).

**Assertable invariant after Phase 1:** the consumer contains zero string literals for `_inner`/`_outer`/`_lookup_` and zero reads of `UsesDriverJoinFields`. This is a grep-able regression guard enforced by a test.

## Data flow

Tracing `ctx.Orders.Include(o => o.Customer).ThenInclude(c => c.Region)` with a top-level reference Include (driver-join mode — the case where `_outer`/`_inner` placement bites today):

**Translation (producer side):**

1. `MongoQueryableMethodTranslatingExpressionVisitor` builds the `MongoQueryExpression` and captures the LINQ chain into `CapturedExpression` — unchanged.
2. `MongoProjectionBindingExpressionVisitor` walks the include tree and registers lookups. **New:** as it places the root under `_outer`, the `Customer` join under `_inner`, and `Region` under `_inner._lookup_Region`, it emits the parallel layout:
   ```
   Entity(Order, path="_outer")
     └─ Navigation(Customer, path="_inner")
          └─ Navigation(Region, path="_lookup_Region")   // relative to parent
   ```
   Paths are stored **relative to the parent**; a node's absolute path is the join of its ancestors, composed by the shaper walking down. This also makes Phase-2 path normalization a localized change.
3. `MongoQueryExpression.ResultLayout` now holds the tree. MQL emitted by the EF-to-LINQ translator is **byte-identical to today**.

**The `_outer`/`_inner` subtlety the layout makes explicit:** the root is `_outer` but `Customer` is `_inner`, a *sibling* of `_outer`, not nested under it. The naive "compose down the tree" would produce `_outer._inner`. The layout encodes the sibling relationship via `absolutePathOverride` on the `Customer` node, so the shaper reads `_inner._lookup_Region` — no inference. This sibling-vs-nested confusion is precisely what the heuristic got wrong in the EF-117 cases.

**Shaper compilation (consumer side):**

4. `MongoShapedQueryCompilingExpressionVisitor` compiles the shaper as today, but hands `ResultLayout` to `MongoProjectionBindingRemovingExpressionVisitor`.
5. When the shaper needs `Order.Customer.Region`, instead of calling `GetCrossCollectionFieldName`, it walks the layout to resolve the path. The answer is recorded, not inferred.

**Runtime:** unchanged — `QueryingEnumerable` streams `BsonDocument`s through the compiled shaper.

**Key property:** every place the old shaper asked "*which* field is this?", the new shaper asks the layout "*what did the producer record for this node?*" — and the two can be diffed in a test.

## Phasing

Each step lands green on the full EF10 suite before the next begins.

- **Phase 0 — Scaffold + characterize.** Add the `DocumentLayout` type and `MongoQueryExpression.ResultLayout` (unused). Add unit tests asserting the *current* heuristic's output for a matrix of include shapes (embedded; single reference; reference + collection; self-ref; the EF-117 problem cases). Captures today's behavior as an oracle before anything changes.
- **Phase 1a — Produce.** Emit layout nodes from the projection-binding visitor alongside the existing placement decisions. Layout is built but **not yet consumed**. Add a debug-only assertion that the layout's computed path equals what `GetCrossCollectionFieldName`/`GetCrossCollectionRootDocument` would return — runs both derivations side by side and fails loudly on divergence. Suite stays green (layout is inert).
- **Phase 1b — Consume + delete.** Switch the shaper to read `ResultLayout`; delete `GetCrossCollectionFieldName`, `GetCrossCollectionRootDocument`, and the `UsesDriverJoinFields` reads. The grep-guard test (zero `_inner`/`_outer`/`_lookup_` literals in the consumer) goes green. MQL frozen ⇒ any spec/functional failure is a real regression. **This is the de-risk payoff and the natural landing point for this spec.**
- **Phase 2 — Normalize + unify (separate spec).** Collapse the mixed path into the base shaper, normalize layout paths, and add GroupBy / complex-type / set-op output shapes as new layout kinds. Out of scope here.

## Error handling

- A binding with **no matching layout node is a hard fail** (`InvalidOperationException`, e.g. "no layout recorded for binding X") — never a silent fallback to guessing. A missing node is a translator bug we want loud.
- The Phase 1a debug-only side-by-side assertion exists specifically to surface divergences between the new layout and the old heuristic *before* Phase 1b removes the old path.

## Validation strategy

- **Unit (no DB):** layout-shape assertions per query pattern — the new regression surface that does not exist today. Plus the Phase-0 oracle tests.
- **Spec / functional:** the existing ~1,100 baselines are the behavior-preservation proof. **MQL frozen in Phase 1 means the baselines must not move.** Any green→red is real.
- **The EF-117 case file:** every wrong-data / missing-element case in the ComplexNav port notes (`Multiple_complex_includes_self_ref`, `Include_collection_multiple`, the X023/X024 cluster) becomes a targeted layout unit test — if the layout tree is right, those cannot regress silently again.

## Out of scope

- Changing the emitted MQL (Phase 2).
- Unifying the three shaper paths (Phase 2).
- New query features — GroupBy, set operations, Distinct, complex-type projections (Phase 2 and beyond).
- EF8/EF9 backport (follow-on spec).
