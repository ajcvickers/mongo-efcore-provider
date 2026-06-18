# Native Reference Include (Includes slice 1) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Translate + materialize a single-level reference `Include` (a navigation to an entity in a separate collection) on the native path — emit `$lookup`+`$unwind` and stream the joined entity — instead of falling back.

**Architecture:** The structured `LookupExpression` IR + the raw `$lookup`/`$unwind` BSON already exist (driver path). Native pipeline: append those stages for reference lookups (replacing the `GetPendingLookups().Count==0` fallback gates), single-level reference only (collection/filtered/nested → fallback). Streaming materializer: materialize the joined non-owned entity from the `_lookup_<Nav>` field, modeled on C′'s owned-reference path but with the lookup-alias source, the joined entity's own PK (no owner-key resolution), and `IncludeReference` fixup.

**Tech Stack:** C#, EF Core 8/9/10 (build `Debug EF10`; validate EF8). MongoDB replica set at `mongodb://localhost:27017`.

---

## Conventions
- Build `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`. Validate EF8 where `#if`/tree shapes differ.
- Tests/benchmark use `MONGODB_URI=mongodb://localhost:27017`. Preserve BOMs.
- Switch: `MONGODB_EF_NATIVE_QUERY` (test-only override); product control is `UseNativeQuery` (default on).
- Existing machinery: `LookupExpression` (`From`/`LocalField`/`ForeignField`/`As`/`IsReference`/`ShouldUnwind`/`GetLookupAlias(nav)` = `"_lookup_"+nav.Name`); `MongoQueryExpression.GetPendingLookups()`; driver `$lookup`/`$unwind` BSON in `MongoEFToLinqTranslatingExpressionVisitor.LeftJoin.cs` `EmitLookupStages`; native gates in `MongoShapedQueryCompilingExpressionVisitor` (~line 195 streaming gate, ~line 316 native-pipeline gate, both `GetPendingLookups().Count == 0`); the native pipeline `BsonDocument[]` is assembled in the native branch of `TranslateQuery`; streaming rewriter throws for non-owned navs in `MongoStreamingEntityMaterializerRewriter` (~line 234).

---

## Task 1: Reference-Include benchmark entity + capture the materializer tree

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/Review.cs`
- Modify: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/BenchmarkDbContext.cs`, `Program.cs`
- Temporarily modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`
- Create: `docs/superpowers/specs/2026-06-18-reference-include-materializer-shape.md`

- [ ] **Step 1: Entity pair (separate collections → non-owned)**

`Review.cs`:
```csharp
using MongoDB.Bson;

namespace MongoDB.EntityFrameworkCore.Benchmarks;

public class Review
{
    public ObjectId Id { get; set; }
    public int Stars { get; set; }
    public ObjectId ProductId { get; set; }   // FK on the dependent (Review)
    public Product? Product { get; set; }      // reference navigation to the principal
}

public class Product
{
    public ObjectId Id { get; set; }
    public string Title { get; set; } = "";
}
```
In `BenchmarkDbContext.cs`: add `public DbSet<Review> Reviews => Set<Review>();` and `public DbSet<Product> Products => Set<Product>();` (BOTH have a DbSet ⇒ non-owned, separate collections; do NOT `OwnsOne`). EF discovers the `Review.Product` reference + `ProductId` FK by convention; if it doesn't, configure `modelBuilder.Entity<Review>().HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId);` in `OnModelCreating`.

- [ ] **Step 2: Seed + smoke check**

In `Program.cs` `--smoke` (same db/options), seed 20 `Product`s and 100 `Review`s (each review's `ProductId` = one of the 20 products' Ids; keep the product Ids to assign). Then:
```csharp
using (var ctx = new BenchmarkDbContext(options))
{
    var reviews = ctx.Reviews.Include(r => r.Product).ToList();
    var withProduct = reviews.Count(r => r.Product != null);
    Console.WriteLine($"REVIEW OK: reviews={reviews.Count}, withProduct={withProduct}");
    if (reviews.Count != 100) throw new InvalidOperationException($"expected 100 reviews, got {reviews.Count}");
    if (withProduct != 100) throw new InvalidOperationException($"expected 100 with Product, got {withProduct}");
}
```

- [ ] **Step 3: Instrument + capture the reference-Include tree**

In `CompileShapedQuery`, after the materializer-injection `#if/#else/#endif`, temporarily add:
```csharp
System.Console.Error.WriteLine("MAT-BLOCK " + rootEntityType.ShortName() + " >>>\n" + new Microsoft.EntityFrameworkCore.Query.ExpressionPrinter().PrintExpression(shaperBody) + "\n<<<MAT-BLOCK");
```
Run (auto — Review currently falls back to DOM since lookups force fallback; capture the tree):
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 dotnet run -c "Release EF10" -- --smoke 2>&1 | sed -n '/MAT-BLOCK Review >>>/,/<<<MAT-BLOCK/p'
```
Confirm `REVIEW OK: reviews=100, withProduct=100` (DOM fallback).

- [ ] **Step 4: Record findings** in `docs/superpowers/specs/2026-06-18-reference-include-materializer-shape.md`: the Review materializer block (verbatim), and notes on: (a) the `IncludeExpression` for `Product` — its `Navigation` (non-owned reference, `IsCollection=false`), and the SOURCE the navigation arm reads from (the `_lookup_Product` field? quote the exact access — `bsonDoc["_lookup_Product"]` or similar); (b) the Product construction block — how its own PK (`Product.Id`) is read (a normal `ValueBufferTryReadValue` from the joined sub-doc, NOT an owner-resolved key), and its `TryGetEntry`/`StartTracking`; (c) the null guard (post-`$unwind`, `_lookup_Product` may be Null); (d) whether the reference fixup node matches the owned-reference `IncludeExpression`/`IncludeReference` shape C′'s `SpliceReferenceInclude` handles, or differs for a non-owned relationship.

- [ ] **Step 5: Revert instrumentation; rebuild; confirm `git diff src/` empty**
```bash
cd /Users/arthur.vickers/code/provider2
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
git diff --stat src/   # expect empty
```

- [ ] **Step 6: Commit** (benchmark entity + findings doc)
```bash
git add benchmarks/ docs/superpowers/specs/2026-06-18-reference-include-materializer-shape.md
git commit -m "Native reference Include: Review/Product benchmark entity + capture materializer tree"
```

---

## Task 2: Eligibility — allow non-owned reference navigations

**Files:** Modify `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/StreamingEligibility.cs`

- [ ] **Step 1: Allow non-owned reference navs with eligible targets**

In the navigation loop, the current rule requires every navigation's target to be owned. Change so a navigation is allowed when its target type is recursively eligible AND it is either owned OR a non-owned **reference** (`!navigation.IsCollection`). Keep rejecting non-owned **collection** navigations (not in this slice) and the collection-of-collection rule. Concretely:
```csharp
        foreach (var navigation in entityType.GetNavigations())
        {
            if (!IsEligible(navigation.TargetEntityType, visiting))
            {
                return false;
            }
            // Owned (reference or collection) navigations are supported. Non-owned navigations are
            // supported only as single references (materialized via $lookup + $unwind); a non-owned
            // collection navigation is not yet streamable.
            if (!navigation.TargetEntityType.IsOwned() && navigation.IsCollection)
            {
                return false;
            }
            // (existing) owned collection whose element itself owns a collection is rejected:
            if (navigation.IsCollection
                && navigation.TargetEntityType.GetNavigations().Any(n => n.IsCollection))
            {
                return false;
            }
        }
```
(Adjust to merge cleanly with the existing checks; keep the cycle-guard, single-PK, TPH, and skip-navigation rules.)

- [ ] **Step 2: Build (EF10 + EF8) + commit**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
git add src/ && git commit -m "Native reference Include: allow non-owned reference navigations in eligibility"
```
Expected: clean. No behavior change yet — the rewriter still throws for non-owned navs (Task 4), so reference-Include queries still fall back in auto.

---

## Task 3: Native pipeline — emit reference `$lookup`/`$unwind`

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` (native gates + lookup-stage emission in the native branch of `TranslateQuery`)
- Possibly: a small helper for the lookup BSON (new file or a static in `NativeTranslation/`)

- [ ] **Step 1: Lookup-stage builder**

Add a helper that turns the query's reference pending lookups into pipeline stages. For each `LookupExpression l` in `queryExpression.GetPendingLookups()`:
- If `!l.IsReference` OR `l.PipelineStages.Count > 0` (filtered) OR it is transitive/nested (its `LocalField` references another lookup's `As`, i.e. `LocalField` starts with `"_lookup_"`) ⇒ `throw new NativeTranslationNotSupportedException(...)` (fall back).
- Else append:
```csharp
new BsonDocument("$lookup", new BsonDocument { { "from", l.From }, { "localField", l.LocalField }, { "foreignField", l.ForeignField }, { "as", l.As } });
new BsonDocument("$unwind", new BsonDocument { { "path", "$" + l.As }, { "preserveNullAndEmptyArrays", true } });
```
(Mirror `EmitLookupStages` in `MongoEFToLinqTranslatingExpressionVisitor.LeftJoin.cs` — reuse its simple-form BSON if you can extract a shared method cleanly; otherwise replicate the ~6 lines above.)

- [ ] **Step 2: Relax the native-pipeline gate to emit lookups**

In the native-pipeline gate (~line 316), replace the `&& queryExpression.GetPendingLookups().Count == 0` condition so the native pipeline is attempted even with pending lookups; inside the `try`, after `MongoPipelineTranslator` builds the base `$match`/`$sort`/`$skip`/`$limit` stages, append the lookup stages (Step 1) to the pipeline `BsonDocument[]` (the lookup-stage builder throws `NativeTranslationNotSupportedException` for unsupported lookups → caught → fallback). Keep the base-pipeline build and lookup-append inside the existing try so any NotSupported falls back.

- [ ] **Step 3: Build + smoke (still fallback-safe in auto)**
```bash
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 dotnet run -c "Release EF10" -- --smoke
```
Expected: build clean; smoke still prints all OK lines incl. `REVIEW OK: reviews=100, withProduct=100`. In AUTO mode the Review query still falls back: the streaming gate (~195) still bails on pending lookups, so the DOM path runs (correct). (Do NOT yet relax the streaming gate / run force — the materializer can't handle the joined entity until Task 4.)

- [ ] **Step 4: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add src/ && git commit -m "Native reference Include: emit \$lookup/\$unwind stages in the native pipeline (reference, single-level)"
```

---

## Task 4: Streaming materializer — materialize the joined reference entity + integrate

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoStreamingEntityMaterializerRewriter.cs`
- Modify: `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` (relax the streaming gate ~line 195)

Build against Task 1's captured tree. Reuse C′'s owned-reference machinery.

- [ ] **Step 1: Relax the streaming gate for reference lookups**

At the streaming gate (~line 195), allow pending lookups when they are all single-level reference lookups (same predicate as Task 3's builder: every pending lookup `IsReference`, not filtered, not transitive). If any lookup is unsupported, leave streaming off (DOM/driver fallback). (Eligibility from Task 2 already admits the root entity.)

- [ ] **Step 2: Handle a lookup-backed non-owned reference navigation in the rewriter**

In the rewriter, stop throwing for a non-owned navigation when it is a single **reference** backed by a lookup; instead materialize it like an owned reference (the `present`-flagged `ReadStartDocument` / element sub-fill-loop / `ReadEndDocument` path) with these differences (per Task 1's tree):
- **Source element name** = the lookup alias `LookupExpression.GetLookupAlias(navigation)` (`"_lookup_" + navigation.Name`), read post-`$unwind` as a single sub-document; BSON `Null` ⇒ `present = false`.
- **No owner-key resolution:** the joined entity reads its OWN primary key from the joined sub-document as a normal property read (the `ConstructionRewriter` rewrites its `ValueBufferTryReadValue`→locals as for any entity; do NOT route its key to an owner local). Its `TryGetEntry`/`StartTracking` (its own key) come from EF's construction block unchanged.
- **Fixup:** reuse `SpliceReferenceInclude` (EF's `IncludeReference`). If Task 1 showed the non-owned reference fixup differs from the owned-reference splice, add a minimal variant; otherwise reuse as-is.
- Still **throw** `NativeTranslationNotSupportedException` for a non-owned **collection** navigation, a filtered/nested include, or any shape not covered → fallback.

- [ ] **Step 3: Build (EF10 + EF8) + force-mode smoke (Review streams end-to-end)**
```bash
cd /Users/arthur.vickers/code/provider2
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"
cd benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 MONGODB_EF_NATIVE_QUERY=force dotnet run -c "Release EF10" -- --smoke
```
Expected (FORCE — Review must STREAM, not throw/fall back): all prior OK lines AND `REVIEW OK: reviews=100, withProduct=100`. `withProduct=100` proves the joined `Product` materialized via `$lookup`+`$unwind`+streaming for every review. Also run `auto` → same. If it throws or `withProduct` is wrong (e.g. nulls, wrong product, identity collisions), debug against Task 1's tree (temp `Console.Error.WriteLine(rewrittenBody.ToString())`). Do NOT weaken the smoke.

- [ ] **Step 4: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add src/ && git commit -m "Native reference Include: stream the joined non-owned reference entity from \$lookup result"
```

---

## Task 5: Validate suite + benchmark + record

**Files:**
- Modify: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/QueryBenchmarks.cs` (Review-Include shapes)
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-18-native-reference-include.md`

- [ ] **Step 1: Auto-mode regression check (THE gate), per assembly**
```bash
cd /Users/arthur.vickers/code/provider2
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"
MONGODB_URI=mongodb://localhost:27017 dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/*.csproj -c "Debug EF10" --filter "FullyQualifiedName~Query" 2>&1 | tail -8
MONGODB_URI=mongodb://localhost:27017 dotnet test tests/MongoDB.EntityFrameworkCore.FunctionalTests/*.csproj    -c "Debug EF10" --filter "FullyQualifiedName~Query" 2>&1 | tail -8
MONGODB_URI=mongodb://localhost:27017 dotnet test tests/MongoDB.EntityFrameworkCore.UnitTests/*.csproj          -c "Debug EF10" --filter "FullyQualifiedName~Query" 2>&1 | tail -6
```
(Timeout 600000 each.) Expected **0 failures** (pre-Include baseline: UnitTests 8/0, FunctionalTests 546/0/44, SpecificationTests 4345/0/18). The Northwind suite has many reference Includes (e.g. `Order.Customer`) that now stream natively. Any failure on a reference-Include test is a real bug (wrong join, wrong/duplicate/null related entity, identity/tracking error, MQL mismatch in an `AssertMql` test) — fix the lookup emission / materializer / eligibility (or tighten to fall back for the specific shape). Re-run to 0. NOTE: `AssertMql` spec tests assert the exact pipeline — reference-Include tests whose baseline MQL was the driver-LINQ `$lookup` should match the native `$lookup` (same shape); if a baseline differs, confirm the native MQL is correct/equivalent and update the test's expected MQL only if it's genuinely the same query (do not weaken — verify equivalence).

- [ ] **Step 2: Opt-out sanity** — the driver-LINQ path still works:
```bash
MONGODB_URI=mongodb://localhost:27017 MONGODB_EF_NATIVE_QUERY=off dotnet test tests/MongoDB.EntityFrameworkCore.SpecificationTests/*.csproj -c "Debug EF10" --filter "FullyQualifiedName~Query" 2>&1 | tail -6
```
Expected 0 failures.

- [ ] **Step 3: Benchmark Review-Include shapes** — in `QueryBenchmarks.cs` seed (10,000 reviews × 100 products) in `[GlobalSetup]`, add:
```csharp
    [Benchmark] public int Review_Include_NoTracking() { using var ctx = new BenchmarkDbContext(_options); return ctx.Reviews.AsNoTracking().Include(r => r.Product).ToList().Count; }
    [Benchmark] public int Review_Include_Tracked() { using var ctx = new BenchmarkDbContext(_options); return ctx.Reviews.Include(r => r.Product).ToList().Count; }
```
Run streaming vs DOM:
```bash
cd /Users/arthur.vickers/code/provider2/benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
MONGODB_URI=mongodb://localhost:27017 dotnet run -c "Release EF10" 2>&1 | tee /tmp/inc-streaming.txt
MONGODB_URI=mongodb://localhost:27017 MONGODB_EF_NATIVE_QUERY=off dotnet run -c "Release EF10" 2>&1 | tee /tmp/inc-dom.txt
```
(Timeout 600000 each.) Capture the `Review_Include_*` rows from both.

- [ ] **Step 4: Record results** — create `results/2026-06-18-native-reference-include.md`:
```markdown
# Native reference Include — results

**Run on:** <date/CPU/OS/SDK>
**Change:** single-level reference Include translated to native $lookup+$unwind and materialized by the streaming reader.

## Regression check (auto, per-assembly)
<counts> — <matches pre-Include 0 failures? list fixes / any AssertMql baseline updates (with equivalence note)>

## Opt-out (MONGODB_EF_NATIVE_QUERY=off): <passed/failed>

## Benchmark: Review.Include(Product) — streaming vs DOM
| Shape | DOM Mean | Streaming Mean | Δ Mean | DOM Alloc | Streaming Alloc | Δ Alloc % |
|---|---:|---:|---:|---:|---:|---:|
<Review_Include_NoTracking, Review_Include_Tracked — actual numbers + deltas>

## Reading & recommendation
- <Include allocation/time delta vs C'/D entity shapes. Next slice: collection Include, then filtered/ThenInclude.>
```
Fill every `<...>`.

- [ ] **Step 5: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add -A && git commit -m "Native reference Include: validate (regressions + benchmark) + Review-Include shapes"
```

---

## Notes for the executor
- **Task 1 governs Task 4.** Build the joined-entity materialization against the real `IncludeExpression` tree; the joined entity's key is its OWN field read (no owner resolution) — the key difference from owned references.
- **Both gates** (`~195` streaming, `~316` native-pipeline) must allow reference lookups; the lookup-stage builder + the materializer must agree on the supported predicate (reference, non-filtered, non-transitive) so a query either fully streams or fully falls back.
- **`force` mode drives development** (Task 4); a reference-Include query in force mode must stream end-to-end.
- **AssertMql spec tests:** native `$lookup` should equal the driver-LINQ `$lookup` for the same Include; verify equivalence before touching any expected-MQL baseline; never weaken.
- **Never disable streaming wholesale / never weaken a test.** Reuse C′'s `SpliceReferenceInclude` + EF's `IncludeReference` fixup; collection/filtered/nested includes throw → fallback.
- Leave `ef-bench-mongo` running.
