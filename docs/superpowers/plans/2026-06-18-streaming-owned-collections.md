# Streaming Owned Collections (Sub-project D) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the forward-only streaming materializer (`MongoStreamingEntityMaterializerRewriter`) to owned collections (a BSON array of owned sub-documents), so entities with owned collections materialize via the `IBsonReader` pass instead of falling back to the DOM. Full recursion; keep the throw→fallback contract.

**Architecture:** Mirror C′'s single-owned-reference path as an array loop (EF's relational `MaterializeJsonEntityCollection` analog): on the collection's element name, `ReadStartArray` → counter-driven loop, per element `ReadStartDocument` → fill element locals → `ReadEndDocument` → construct the element (EF's per-element block, rewritten) → add to a `List<TElement>`, `counter++` → `ReadEndArray`; then populate the parent navigation via `IClrCollectionAccessor`. The synthetic `IsOwnedTypeOrdinalKey` resolves to `counter + 1`. Unlike a reference (fill-once/construct-once), a collection re-fills + re-constructs per element inside the loop.

**Tech Stack:** C#, EF Core 8/9/10 (build `Debug EF10`; validate EF8 too), MongoDB.Driver 3.9.0. Reference: EF `MaterializeJsonEntityCollection`; the provider's DOM `CollectionShaperExpression` handling in `MongoProjectionBindingRemovingExpressionVisitor` (lines ~133–189) + `PopulateCollection` (~927–939).

---

## Conventions
- Build `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`. The rewriter is EF-version-agnostic but the injected tree differs (`#if`), so validate EF8 + EF10.
- MongoDB replica set at `mongodb://localhost:27017`; tests/benchmark use `MONGODB_URI=mongodb://localhost:27017`.
- Switch `MONGODB_EF_NATIVE_QUERY` (auto/force/off). Preserve BOMs.
- The rewriter file: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoStreamingEntityMaterializerRewriter.cs` — key members: `EntityPlan` (EntityType/Locals/Present/OwnedNavigations), `BuildPlan` (collection rejection ~line 204), `CollectLocals`, `BuildFillLoop` (owned-reference descent), `RewriteMaterializer` (IncludeExpression; collection rejection ~line 332), `SpliceReferenceInclude`, `RewriteOwnedNavigation` (nested-collection rejection ~line 433), and a `ConstructionRewriter` (`ResolveLocal`).

---

## Task 1: Add a `Basket` owned-collection benchmark entity + capture its materializer tree

**Files:**
- Modify: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/` — new `Basket.cs`, `BenchmarkDbContext.cs`, `Program.cs`
- Temporarily modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Create: `docs/superpowers/specs/2026-06-18-owned-collection-materializer-shape.md`

- [ ] **Step 1: Add the entity**

`benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/Basket.cs`:
```csharp
using MongoDB.Bson;

namespace MongoDB.EntityFrameworkCore.Benchmarks;

public class Basket
{
    public ObjectId Id { get; set; }
    public string Owner { get; set; } = "";
    public int Code { get; set; }
    public List<BasketItem> Items { get; set; } = new();
}

public class BasketItem
{
    public string Sku { get; set; } = "";
    public int Qty { get; set; }
    public decimal Price { get; set; }
}
```

- [ ] **Step 2: Map it** — in `BenchmarkDbContext.cs`: add `public DbSet<Basket> Baskets => Set<Basket>();` and in `OnModelCreating` `modelBuilder.Entity<Basket>().OwnsMany(b => b.Items);`.

- [ ] **Step 3: Seed + smoke check** — in `Program.cs` `--smoke`, after the existing checks: seed 100 `Basket`s (each with 3 `BasketItem`s, deterministic), then:
```csharp
using (var ctx = new BenchmarkDbContext(options))
{
    var baskets = ctx.Baskets.ToList();
    var totalItems = baskets.Sum(b => b.Items.Count);
    Console.WriteLine($"BASKET OK: baskets={baskets.Count}, items={totalItems}");
    if (baskets.Count != 100) throw new InvalidOperationException($"expected 100 baskets, got {baskets.Count}");
    if (totalItems != 300) throw new InvalidOperationException($"expected 300 items, got {totalItems}");
}
```
(Seed Baskets alongside the Customer/FlatItem seeding, same db/options.)

- [ ] **Step 4: Instrument + capture the materializer tree for a Basket read**

In `MongoShapedQueryCompilingExpressionVisitor.CompileShapedQuery`, after the materializer-injection `#if/#else/#endif`, temporarily add:
```csharp
System.Console.Error.WriteLine("MAT-BLOCK " + rootEntityType.ShortName() + " >>>\n" + new Microsoft.EntityFrameworkCore.Query.ExpressionPrinter().PrintExpression(shaperBody) + "\n<<<MAT-BLOCK");
```
Run (auto mode; Basket currently falls back to DOM since collections aren't streamed yet — that's fine, we just want the tree):
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 dotnet run -c "Release EF10" -- --smoke 2>&1 | sed -n '/MAT-BLOCK Basket >>>/,/<<<MAT-BLOCK/p'
```

- [ ] **Step 5: Record findings** in `docs/superpowers/specs/2026-06-18-owned-collection-materializer-shape.md`: the Basket materializer block (verbatim), and notes on: (a) how the owned collection appears — a `CollectionShaperExpression`, an `ObjectArrayProjectionExpression`, and/or an `IncludeExpression` whose `Navigation.IsCollection`? what element name is the array under? (b) the inner per-element shaper block (its `ValueBufferTryReadValue` reads, its `IsOwnedTypeOrdinalKey` property and how the ordinal is referenced, its owner-FK property); (c) how the collection is populated (`PopulateCollection`/`IClrCollectionAccessor`) and how that node is structured; (d) anything that differs from the single-owned-reference `IncludeExpression` shape C′ already handles.

- [ ] **Step 6: Revert instrumentation; rebuild; confirm `git diff src/` empty**
```bash
# remove the Console.Error.WriteLine line
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git diff --stat src/   # expect empty
```

- [ ] **Step 7: Commit** (benchmark entity + findings doc; src reverted)
```bash
git add benchmarks/ docs/superpowers/specs/2026-06-18-owned-collection-materializer-shape.md
git commit -m "Streaming owned collections: add Basket benchmark entity + capture materializer tree"
```
(Smoke should print `BASKET OK: baskets=100, items=300` via the DOM fallback — confirm before committing.)

---

## Task 2: Allow owned collections in eligibility

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/StreamingEligibility.cs`

- [ ] **Step 1: Allow recursively-eligible owned collections**

In the navigation loop, replace the blanket `navigation.IsCollection` rejection so a collection navigation is allowed when owned and its element type is recursively eligible. The loop body becomes:
```csharp
        foreach (var navigation in entityType.GetNavigations())
        {
            if (!navigation.TargetEntityType.IsOwned()
                || !IsEligible(navigation.TargetEntityType, visiting))
            {
                return false;
            }
            // Both single (reference) and collection owned navigations are allowed, provided the
            // target owned type is itself eligible (checked above, recursively).
        }
```
(Keep the cycle-guard `visiting` set, the simple-single-PK check, the TPH/derived-types check, and the `GetSkipNavigations().Any()` rejection unchanged.)

- [ ] **Step 2: Build (EF10 + EF8)**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
```
Expected: clean. (No behavior change yet for collections — the rewriter still throws for them until Task 3, so eligible-but-not-yet-rewritable collection entities fall back in auto. That's fine and safe.)

- [ ] **Step 3: Commit**
```bash
git add src/ && git commit -m "Streaming owned collections: allow recursively-eligible owned collections in eligibility"
```

---

## Task 3: Extend the rewriter to owned collections (the core)

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoStreamingEntityMaterializerRewriter.cs`

Build against Task 1's captured tree. Mirror C′'s owned-reference handling as an array loop. Keep "throw `NativeTranslationNotSupportedException` on anything not handled" so unsupported shapes fall back.

- [ ] **Step 1: Plan model — represent collection navigations**

Extend `EntityPlan` (or add a parallel structure) so a navigation entry records whether it's a collection, plus, for collections: the element `EntityPlan`, a loop-counter `ParameterExpression`, and a `List<TElement>` accumulator `ParameterExpression`. In `BuildPlan`, stop rejecting `navigation.IsCollection`: for an owned collection navigation, build the element type's plan (`BuildPlan(target, present: null)` — collection elements use the array loop, not a present-flag) and allocate the counter + list locals. (Single owned references keep their existing `present`-flagged plan.)

- [ ] **Step 2: `CollectLocals`** — also declare the collection counter locals and list-accumulator locals, and the element plan's locals (element locals are shared across iterations and reassigned each element). Initialize counters to 0 and lists to `new List<TElement>()` (or defer list creation to the loop entry).

- [ ] **Step 3: `BuildFillLoop` — array loop for a collection navigation**

For a collection navigation, the parent fill loop's name-dispatch for the array's element name emits:
```
reader.ReadStartArray();
counter = 0;
list = new List<TElement>();
while (reader.ReadBsonType() != BsonType.EndOfDocument)   // EndOfDocument is the end-of-array sentinel
{
    reader.ReadStartDocument();
    <element sub-fill-loop: fill the element plan's locals>
    reader.ReadEndDocument();
    <constructed element>   // EF's per-element construction block, rewritten (Step 4)
    list.Add(<constructed element>);
    counter = counter + 1;
}
reader.ReadEndArray();
```
The element construction must happen INSIDE the loop (per element), so the rewritten element-construction expression (from Task 1's inner element shaper, rewritten in Step 4) is inlined here. Handle a BSON `Null` array value (absent collection) by leaving the list empty (or per EF semantics — match the DOM path; confirm via Task 1).

- [ ] **Step 4: Rewrite the element construction + ordinal key + owner FK**

When `RewriteMaterializer` encounters the collection node (a `CollectionShaperExpression`/`ObjectArrayProjectionExpression`, or an `IncludeExpression` whose `Navigation.IsCollection` — per Task 1): extract the inner per-element shaper block, rewrite it (reuse the `ConstructionRewriter`): `ValueBufferTryReadValue(property)`→element local (box at `<object>`), `MaterializationContext`→`ValueBuffer.Empty`, the element's `IsOwnedTypeOrdinalKey` property →`counter + 1`, the element's owner-FK →the owner's key local (reuse `ResolveLocal`). This rewritten element block is the `<constructed element>` inlined in Step 3's loop.

- [ ] **Step 5: Populate the parent navigation**

After the loop builds `list`, assign it to the parent's collection navigation. Mirror the DOM path's `PopulateCollection<TEntity,TCollection>(navigation.GetCollectionAccessor(), list)` (or call the same `PopulateCollection` helper / `IClrCollectionAccessor.AddRange`-equivalent), and wire it through EF's collection-Include fixup so the parent's navigation property is set and (for tracking) the owned entries are tracked. Keep EF's `IncludeExpression` collection structure (analogous to `SpliceReferenceInclude` for references — add a `SpliceCollectionInclude` if the include-fixup call differs for collections; check EF's `IncludeCollection`/collection-fixup helper).

- [ ] **Step 6: Full recursion** — a collection element type may itself own references and/or collections; `BuildPlan`/`BuildFillLoop`/`RewriteMaterializer` already recurse for references; ensure the collection path recurses too (an element's nested owned collection uses the same array-loop logic). If a nested case can't be generated correctly, `throw NativeTranslationNotSupportedException` (→ fallback) and note it.

- [ ] **Step 7: Build (EF10 + EF8) + force-mode smoke (Basket streams end-to-end)**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 MONGODB_EF_NATIVE_QUERY=force dotnet run -c "Release EF10" -- --smoke
```
Expected (FORCE — Basket must stream, not throw): all prior smoke lines (`SMOKE OK ...`, `FLAT OK ...`) AND `BASKET OK: baskets=100, items=300`. `items=300` proves each basket's 3-item owned collection materialized via the array loop. If it throws or `items` is wrong (e.g. items duplicated/dropped, wrong ordinal → identity collisions), debug against Task 1's tree (temp `Console.Error.WriteLine(rewrittenBody.ToString())`). Do NOT weaken the smoke.

- [ ] **Step 8: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add src/ && git commit -m "Streaming owned collections: array-loop materialization in the rewriter"
```

---

## Task 4: Validate suite + benchmark + record

**Files:**
- Modify: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/` — add `Basket` benchmark shapes
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-18-streaming-owned-collections-D.md`

- [ ] **Step 1: Auto-mode regression check (the gate)** — per assembly (avoid combined-run shared-DB pollution):
```bash
cd /Users/arthur.vickers/code/provider2
MONGODB_URI=mongodb://localhost:27017 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/*.csproj -c "Debug EF10" --filter "FullyQualifiedName~Query" 2>&1 | tail -8
MONGODB_URI=mongodb://localhost:27017 dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/*.csproj -c "Debug EF10" --filter "FullyQualifiedName~Query" 2>&1 | tail -8
MONGODB_URI=mongodb://localhost:27017 dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/*.csproj -c "Debug EF10" --filter "FullyQualifiedName~Query" 2>&1 | tail -8
```
(Timeout 600000 each.) Expected **0 failures** (pre-D: UnitTests 8/0, FunctionalTests 544/0/44, SpecificationTests 4345/0/18). Entities with owned collections across the suites now stream. Any failure on an eligible owned-collection entity is a real rewriter bug — fix it (or tighten eligibility to fall back for the specific shape; never weaken a test, never disable streaming wholesale). Re-run to 0.

- [ ] **Step 2: Add Basket benchmark shapes** — in `QueryBenchmarks.cs` (seed Baskets in `GlobalSetup`, 10,000 baskets × 3 items), add:
```csharp
    [Benchmark] public int Basket_NoTracking_ToList() { using var ctx = new BenchmarkDbContext(_options); return ctx.Baskets.AsNoTracking().ToList().Count; }
    [Benchmark] public int Basket_Tracked_ToList() { using var ctx = new BenchmarkDbContext(_options); return ctx.Baskets.ToList().Count; }
```

- [ ] **Step 3: Benchmark streaming vs DOM** — run twice and compare (a fresh entity has no A-baseline, so on/off is the control):
```bash
cd /Users/arthur.vickers/code/provider2/benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 dotnet run -c "Release EF10" 2>&1 | tee /tmp/d-streaming.txt          # auto = streaming
MONGODB_URI=mongodb://localhost:27017 MONGODB_EF_NATIVE_QUERY=off dotnet run -c "Release EF10" 2>&1 | tee /tmp/d-dom.txt   # DOM
```
(Timeout 600000 each.) Capture the `Basket_*` rows from both.

- [ ] **Step 4: Record results** — create `results/2026-06-18-streaming-owned-collections-D.md`:
```markdown
# Streaming owned collections (sub-project D) — results

**Run on:** <date/CPU/OS/SDK>
**Change:** streaming materializer extended to owned collections (array loop, per-element construct, ordinal=counter+1, PopulateCollection).

## Regression check (auto, per-assembly)
<counts> — <matches pre-D 0 failures? any fixes made>

## Benchmark: Basket — streaming vs DOM (MONGODB_EF_NATIVE_QUERY=off)
| Shape | DOM Mean | Streaming Mean | Δ Mean | DOM Alloc | Streaming Alloc | Δ Alloc % |
|---|---:|---:|---:|---:|---:|---:|
<Basket_NoTracking_ToList, Basket_Tracked_ToList — actual numbers + deltas>

## Reading & recommendation
- <Allocation/time delta for owned-collection materialization vs the ~60-68% C' saw on flat/owned-ref shapes.>
- <Recommendation: next eligibility extension (cross-collection Includes / TPH), or make streaming the default + start retiring DOM for covered shapes (sub-project E)?>
```
Fill every `<...>` from actual runs.

- [ ] **Step 5: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add -A && git commit -m "Streaming owned collections: validate D (regressions + benchmark) + Basket shapes"
```

---

## Notes for the executor
- **Task 1 governs Task 3.** Build the collection rewriter against the real captured tree; mirror `MongoProjectionBindingRemovingExpressionVisitor`'s `CollectionShaperExpression` handling (array-by-name, `SelectWithOrdinal`, `PopulateCollection`) and EF's `MaterializeJsonEntityCollection`. Keep throw→fallback for anything unhandled.
- **`force` mode is the development driver** — iterate Task 3 against the Basket smoke in force mode.
- **Reuse EF's per-element construction + tracking + collection fixup** — only the value-source (DOM array → forward array loop) and ordinal-from-counter change.
- **Never weaken a test / never disable streaming wholesale.** A failure on an eligible owned-collection entity is a real bug to fix; tightening eligibility to fall back for a specific unsupported shape is acceptable.
- Leave `ef-bench-mongo` running.
