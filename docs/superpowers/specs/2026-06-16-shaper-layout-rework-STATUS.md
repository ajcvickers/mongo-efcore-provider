# Shaper Layout Rework — Implementation Status

**Date:** 2026-06-16
**Branch:** `shaper-layout-rework` (off `main` @ `efb5f25`)
**Verified checkpoint:** commit `40faee3` — full EF10 suite green (UnitTests 270 / FunctionalTests 1518 +51 skip / SpecificationTests 4408 +19 skip, **0 failures**), independently re-run by the controller.

This documents what the rework landed, what is deliberately deferred, and the exact entry point for completing full heuristic elimination in a follow-up. It supplements the design (`2026-06-16-shaper-layout-rework-design.md`) and plan (`../plans/2026-06-16-shaper-layout-rework.md`).

## What landed (verified green at `40faee3`)

The translator now authors a `DocumentLayout` descriptor (`Query/Layout/DocumentLayout.cs`) on `MongoQueryExpression.ResultLayout`, finalized once (`MongoQueryExpression.FinalizeLayout`), and the shaper reads from it. The layout is the **source of truth** for these field locations:

- **Include cross-collection reads** — reference and collection Includes, ThenInclude chains, including the driver-native LeftJoin `_outer`/`_inner` shape. This is the area where the EF-117 cross-collection silent-wrong-data bugs lived (the X023/X024/self-ref clusters); it is now resolved from the authored layout rather than the re-derivation heuristic.
- **Join `_inner` reads** — explicit `Join`/`LeftJoin` inner entities are authored as `_inner`-pinned layout nodes keyed by entity type (`FinalizeLayout` Case B).
- **`_outer` root reads** — the query-root entity in driver-join mode reads its `_outer` path from the layout root (pinned by `FinalizeLayout`), not from the `UsesDriverJoinFields` flag.

A bug fixed along the way: **sibling-include mis-parenting** (`Include(o => o.Customer).Include(o => o.OrderDetails)` parented the second include under the first). Fixed in `FindLayoutParent` by matching the enclosing navigation whose `TargetEntityType == nav.DeclaringEntityType` (commit `12511d0`).

## What is still heuristic-served (deferred)

The legacy heuristic (`GetCrossCollectionFieldName` / `GetCrossCollectionRootDocument` / the `UsesDriverJoinFields`-driven `_inner` decision) is **retained as a fallback** for cross-collection navigations the layout does not yet author a node for:

- **Inline projected navigations** without an `Include` (`select new { c.Orders }`, projection-to-object-array, entity-compared-to-null).
- **Multi-level ThenInclude intermediate references** registered inside a collection pipeline (`ExtractNestedIncludePipeline`, `AddReferenceLookupStages`).
- **Self-referential navigations** (`DirectReports.ThenInclude(d => d.DirectReports)`).

These paths are **correct today** (the heuristic serves them and the suite is green); they simply block *deleting* the heuristic.

## Why full elimination was deferred

Two findings made full elimination a focused follow-up rather than an in-line continuation:

1. **It is a structural-resolution change, not more authoring.** The clean way to cover the deferred shapes is to resolve cross-collection fields *structurally* — recurse the `ObjectAccessExpression` parent chain (`access.AccessExpression` + `access.Name`), which the consumer already does for general object access and which is inherently per-instance (self-ref safe). The layout then only needs to supply the driver-join `_inner`/`_outer` overlay (which it already pins). This avoids authoring at ~5 more sites and the self-ref keying problem entirely.

2. **The structural switch exposed an FK-join keying mismatch — the follow-up's entry point.** A first attempt at the structural switch (stashed, see below) broke **56 tests** (Join / Navigation / AsNoTracking / a left-join-include `NullReferenceException` in `BsonBinding.GetBsonDocument`). Root cause: FK-derived join inner accesses are `NavigationObjectAccessExpression`s (they *have* a navigation), but `FinalizeLayout` Case B authors their nodes keyed by **entity type**, and `FindLayoutNode`'s entity-type fallback only fires when `navigation == null`. So the node is not found → not recognized as pinned → structural recursion yields `_lookup_<Nav>` instead of `_inner`. The fix is to reconcile the keying (e.g. author FK-join inner nodes with their navigation, or let the entity-type fallback fire even when a navigation is present but unmatched), then re-run.

## Follow-up plan (own session)

1. Fix the FK-join keying mismatch (above).
2. Switch cross-collection resolution to **pinned-node → layout path; else → structural recursion** (the stashed edits are the starting shape — they add `DocumentLayout.IsAbsolutePinned` and rewrite the two cross-collection branches in `MongoProjectionBindingRemovingExpressionVisitor`).
3. Remove the heuristic fallback; verify the full suite green (this is the real coverage gate).
4. Delete `GetCrossCollectionFieldName`, `GetCrossCollectionRootDocument`, `RecordLayoutDivergence`, `LayoutHeuristicDivergence`, and the `UsesDriverJoinFields` reads in the consumer/mixed visitor (original plan T9).
5. Add the grep-guard test (no `_inner`/`_outer`/`_lookup_` literals, no `UsesDriverJoinFields` reads in the consumer) (T10).
6. Backport behind `#if` to EF8/EF9 (originally out of scope).

**Verify every step by independently re-running the full EF10 suite.** During this work two subagents reported green / "pre-existing failures" that were false (they had not tested clean `HEAD`); do not trust a green report without re-running.

## Stash

`stash@{0}` ("T8c-structural-wip") holds the first structural-switch attempt (`DocumentLayout.IsAbsolutePinned` + the two rewritten cross-collection branches). It is **incomplete** — it causes the 56 FK-join failures above. Useful as a starting point for the follow-up; not safe to apply as-is. May be dropped and re-derived from this doc.

## DEBUG-only scaffolding still present at `40faee3`

`Query/Layout/LayoutHeuristicDivergence.cs` and `RecordLayoutDivergence` (a `#if DEBUG` divergence collector comparing layout-leaf vs heuristic field) remain. They are harmless (DEBUG-only, observational) and are removed in the follow-up when the heuristic is deleted.
