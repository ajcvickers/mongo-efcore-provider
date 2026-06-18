# Streaming owned collections (sub-project D) — results

> **Final review: SOLID-FOR-SPIKE.** Ordinal correctness (construct-then-increment, `counter+1`, scoped by
> `DeclaringType`), array-bracket integrity, and null/empty-collection semantics all verified to match the DOM
> path; all unhandled shapes fail safe via throw→fallback. Caveat & follow-ups:
> - **Scope reality: "full recursion" = one owned-collection level + nested owned references.**
>   Collection-of-owned-collection is over-admitted by `StreamingEligibility` but always falls back (a
>   grandchild owner-FK referencing the parent element's ordinal has no resolving local → throws → DOM path).
>   Safe (no wrong results). Follow-up: either tighten eligibility to reject a collection element that itself
>   owns a collection (state intent), or thread parent-ordinal resolution to actually stream it. Add a
>   collection-of-collection test pinning the fallback.
> - Nits: comment that per-element BSON `Null` mirrors the DOM throw; drop/assert the `Name`-match fallback in
>   `FindCollectionPlan`/`FindChildPlan`; note the in-loop counter/list reset is authoritative.

**Run on:** 2026-06-18 / Apple M4 Max, 1 CPU, 14 logical and 14 physical cores / macOS Tahoe 26.5.1 (25F80) [Darwin 25.5.0] / .NET SDK 10.0.301, .NET 10.0.9 Arm64 RyuJIT (BenchmarkDotNet v0.15.8)
**Change:** streaming materializer extended to owned collections (array loop, per-element construct, ordinal=counter+1, PopulateCollection). Eligibility relaxed for owned-collection composite keys.

## Regression check (auto, per-assembly)
| Assembly | Passed | Failed | Skipped |
|---|---:|---:|---:|
| UnitTests (~Query) | 8 | 0 | 0 |
| FunctionalTests (~Query) | 544 | 0 | 44 |
| SpecificationTests (~Query) | 4345 | 0 | 18 |

Matches the pre-D baseline exactly (UnitTests 8/0, FunctionalTests 544/0/44, SpecificationTests 4345/0/18). **0 failures, no fixes required** — owned-collection entities now stream through EF tracking with correct item counts, ordinals, and owner-FKs; ineligible shapes fall back cleanly. End-to-end smoke (`MONGODB_EF_NATIVE_QUERY=force`): `SMOKE OK`, `FLAT OK`, `BASKET OK: baskets=100, items=300`.

## Benchmark: Basket — streaming (auto) vs DOM (MONGODB_EF_NATIVE_QUERY=off)
10,000 Baskets × 3 BasketItems each (owned collection).

| Shape | DOM Mean | Streaming Mean | Δ Mean | DOM Alloc | Streaming Alloc | Δ Alloc % |
|---|---:|---:|---:|---:|---:|---:|
| Basket_NoTracking_ToList | 78.840 ms | 36.589 ms | -42.251 ms (-53.6%) | 114.79 MB | 34.99 MB (35830.23 KB) | -69.5% |
| Basket_Tracked_ToList | 139.328 ms | 84.773 ms | -54.555 ms (-39.2%) | 162.53 MB | 70.14 MB (71823.55 KB) | -56.8% |

## Reading & recommendation
- **The owned-collection win is comparable to, and on allocation exceeds, the flat/owned-ref shapes.** NoTracking allocation drops 69.5% (vs the ~60-68% C' saw on flat/owned-ref shapes) and time drops 53.6% — the per-element array loop does not erode the streaming advantage; eliminating the intermediate BSON DOM for the whole document (owner + nested item array) is the dominant saving.
- The tracked path shows a smaller (but still large) win: alloc -56.8%, time -39.2%. The narrower margin is expected — change-tracker entries, identity-map insertions, and collection fixup for the synthetic-ordinal-keyed owned entities are paid on both paths, so they dilute the relative streaming gain. The per-element construct + PopulateCollection overhead is real but well below the cost of building and walking the DOM it replaces.
- **Recommendation: proceed to sub-project E — make streaming the default for covered shapes and begin retiring the DOM path for them.** With four shape classes now validated at 0 regressions and a consistent 40-70% saving, the eligibility surface (flat, owned-ref, owned-collection) is broad enough to flip the default to `auto`-on and start deprecating the DOM materializer for the covered set, keeping DOM only as the fallback for not-yet-eligible shapes. Cross-collection Includes / TPH remain the next eligibility extension, but they can land after the default flip rather than gating it.
