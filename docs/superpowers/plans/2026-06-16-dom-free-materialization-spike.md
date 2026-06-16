# DOM-free Materialization Spike — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Measure the time and allocation difference between the provider's current DOM-based materialization and a reader-based (DOM-free) materialization, to decide whether the reader-shaper codegen rewrite is worth building.

**Architecture:** A standalone BenchmarkDotNet console project (no EF, no live MongoDB) generates an in-memory corpus of BSON document bytes, then materializes them into `BenchmarkEntity` three ways: (A) `BsonDocument` DOM + per-member `DeserializeValue` (mimics today's provider), (B) a hand-written order-tolerant `BsonBinaryReader` shaper (the candidate), and (C) the driver's own typed `BsonClassMapSerializer` (reference ceiling). A driver `BsonClassMap` is the single source of truth for element names and per-member serializers, so all three paths stay consistent.

**Tech Stack:** C#, `net8.0`, `MongoDB.Driver` 3.9.0, `BenchmarkDotNet` 0.15.8.

---

## File structure

All under a new directory `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/`, **not** added to `MongoDB.EFCoreProvider.sln`:

- `Directory.Build.props` — empty `<Project/>`; stops any future repo-root build props from leaking into this standalone project.
- `MongoDB.EntityFrameworkCore.MaterializationBenchmark.csproj` — `Exe`, `net8.0`, references BenchmarkDotNet + MongoDB.Driver.
- `BenchmarkEntity.cs` — the POCO (`BenchmarkEntity` mutable class, `Address` record, `Kind` enum) + value equality.
- `CorpusGenerator.cs` — deterministic entity generation and serialization to `byte[][]`.
- `MemberAccessor.cs` — the shared per-member metadata cache (element name, serializer, setter) built from the class map.
- `DomMaterializer.cs` — Path A.
- `ReaderMaterializer.cs` — Path B.
- `MaterializationBenchmarks.cs` — BenchmarkDotNet harness (`[MemoryDiagnoser]`, 3 `[Benchmark]` methods, `[GlobalSetup]` correctness gate).
- `Program.cs` — entry point: `--verify` runs the correctness check fast; otherwise runs BenchmarkRunner.

---

## Task 1: Project scaffold + entity + corpus generator

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/Directory.Build.props`
- Create: `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/MongoDB.EntityFrameworkCore.MaterializationBenchmark.csproj`
- Create: `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/BenchmarkEntity.cs`
- Create: `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/CorpusGenerator.cs`
- Create: `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/Program.cs`

- [ ] **Step 1: Create the isolation props file**

`Directory.Build.props`:

```xml
<Project>
</Project>
```

- [ ] **Step 2: Create the csproj**

`MongoDB.EntityFrameworkCore.MaterializationBenchmark.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <AssemblyName>MongoDB.EntityFrameworkCore.MaterializationBenchmark</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.15.8" />
    <PackageReference Include="MongoDB.Driver" Version="3.9.0" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create the entity, nested record, and enum with value equality**

`BenchmarkEntity.cs`:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.EntityFrameworkCore.MaterializationBenchmark;

public enum Kind { A, B, C }

// Record => value equality for free; exercises the driver's constructor (creator) binding on read.
public record Address(string Street, string City, int Zip);

// Mutable class with parameterless ctor => exercises the property-set materialization path.
public sealed class BenchmarkEntity
{
    public ObjectId Id { get; set; }
    public int Count { get; set; }
    public long Big { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Quantity { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Ref { get; set; }

    public DateTime Created { get; set; }
    public decimal Price { get; set; }
    public double Rate { get; set; }
    public bool Active { get; set; }
    public Kind Kind { get; set; }

    public Address Address { get; set; } = new("", "", 0);
    public string[] Tags { get; set; } = Array.Empty<string>();

    public bool ValueEquals(BenchmarkEntity o) =>
        Id == o.Id && Count == o.Count && Big == o.Big && Name == o.Name
        && Description == o.Description && Quantity == o.Quantity && Ref == o.Ref
        && Created == o.Created && Price == o.Price && Rate.Equals(o.Rate)
        && Active == o.Active && Kind == o.Kind && Address == o.Address
        && Tags.SequenceEqual(o.Tags);
}
```

- [ ] **Step 4: Create the deterministic corpus generator**

`CorpusGenerator.cs`:

```csharp
using MongoDB.Bson;

namespace MongoDB.EntityFrameworkCore.MaterializationBenchmark;

public static class CorpusGenerator
{
    // Fixed base date aligned to whole milliseconds so BSON UTC-datetime round-trips exactly.
    private static readonly DateTime BaseUtc = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static BenchmarkEntity[] GenerateEntities(int n)
    {
        var result = new BenchmarkEntity[n];
        for (var i = 0; i < n; i++)
        {
            result[i] = new BenchmarkEntity
            {
                Id = ObjectId.GenerateNewId(),
                Count = i,
                Big = 1_000_000_000L + i,
                Name = "name-" + i,
                Description = "description text for entity number " + i,
                Quantity = i % 500,
                Ref = new Guid(i, (short)(i % 100), (short)(i % 7), 1, 2, 3, 4, 5, 6, 7, 8),
                Created = BaseUtc.AddMilliseconds(i * 1000),
                Price = 1.23m + i,
                Rate = i * 0.5,
                Active = (i % 2) == 0,
                Kind = (Kind)(i % 3),
                Address = new Address("street-" + i, "city-" + (i % 50), 10000 + i),
                Tags = new[] { "t" + (i % 3), "t" + (i % 5), "t" + (i % 7) }
            };
        }
        return result;
    }

    public static byte[][] SerializeToBytes(BenchmarkEntity[] entities)
    {
        var result = new byte[entities.Length][];
        for (var i = 0; i < entities.Length; i++)
        {
            result[i] = entities[i].ToBson(); // uses the driver's registered class-map serializer
        }
        return result;
    }
}
```

- [ ] **Step 5: Create a minimal Program that proves generation works**

`Program.cs`:

```csharp
using MongoDB.EntityFrameworkCore.MaterializationBenchmark;

var entities = CorpusGenerator.GenerateEntities(10);
var bytes = CorpusGenerator.SerializeToBytes(entities);
Console.WriteLine($"Generated {entities.Length} entities, first doc is {bytes[0].Length} bytes.");
```

- [ ] **Step 6: Restore, build, and run**

Run:
```bash
cd benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark
dotnet run -c Release
```
Expected: builds, prints `Generated 10 entities, first doc is <N> bytes.` with N > 0. (If `ToBson()` throws on the Guid member, the representation attribute is missing — re-check Step 3.)

- [ ] **Step 7: Commit**

```bash
git add benchmarks/
git commit -m "Spike: benchmark project scaffold + entity + corpus generator"
```

---

## Task 2: Shared member accessor cache (single source of truth)

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/MemberAccessor.cs`

The class map drives element names and per-member serializers so all three paths agree. Each accessor carries the element name, the driver serializer, the nominal type (for the DOM path's `BsonSerializationInfo`), and a hand-written setter.

- [ ] **Step 1: Create the accessor type and the cache builder**

`MemberAccessor.cs`:

```csharp
using MongoDB.Bson.Serialization;

namespace MongoDB.EntityFrameworkCore.MaterializationBenchmark;

public sealed class MemberAccessor
{
    public required string ElementName { get; init; }
    public required IBsonSerializer Serializer { get; init; }
    public required Action<BenchmarkEntity, object?> Set { get; init; }

    // Reusable for the DOM path (BsonSerializationInfo.DeserializeValue(BsonValue)).
    public BsonSerializationInfo SerializationInfo =>
        new(ElementName, Serializer, Serializer.ValueType);
}

public static class MemberCache
{
    // Built once from the driver's class map for BenchmarkEntity.
    public static readonly IReadOnlyList<MemberAccessor> Members = Build();

    public static readonly IReadOnlyDictionary<string, MemberAccessor> ByElementName =
        Members.ToDictionary(m => m.ElementName);

    private static MemberAccessor[] Build()
    {
        var cm = BsonClassMap.LookupClassMap(typeof(BenchmarkEntity));

        MemberAccessor For(string memberName, Action<BenchmarkEntity, object?> set)
        {
            var mm = cm.GetMemberMap(memberName)
                     ?? throw new InvalidOperationException($"No member map for '{memberName}'.");
            return new MemberAccessor
            {
                ElementName = mm.ElementName,
                Serializer = mm.GetSerializer(),
                Set = set
            };
        }

        return new[]
        {
            For(nameof(BenchmarkEntity.Id),          (e, v) => e.Id = (MongoDB.Bson.ObjectId)v!),
            For(nameof(BenchmarkEntity.Count),       (e, v) => e.Count = (int)v!),
            For(nameof(BenchmarkEntity.Big),         (e, v) => e.Big = (long)v!),
            For(nameof(BenchmarkEntity.Name),        (e, v) => e.Name = (string)v!),
            For(nameof(BenchmarkEntity.Description), (e, v) => e.Description = (string)v!),
            For(nameof(BenchmarkEntity.Quantity),    (e, v) => e.Quantity = (int)v!),
            For(nameof(BenchmarkEntity.Ref),         (e, v) => e.Ref = (Guid)v!),
            For(nameof(BenchmarkEntity.Created),     (e, v) => e.Created = (DateTime)v!),
            For(nameof(BenchmarkEntity.Price),       (e, v) => e.Price = (decimal)v!),
            For(nameof(BenchmarkEntity.Rate),        (e, v) => e.Rate = (double)v!),
            For(nameof(BenchmarkEntity.Active),      (e, v) => e.Active = (bool)v!),
            For(nameof(BenchmarkEntity.Kind),        (e, v) => e.Kind = (Kind)v!),
            For(nameof(BenchmarkEntity.Address),     (e, v) => e.Address = (Address)v!),
            For(nameof(BenchmarkEntity.Tags),        (e, v) => e.Tags = (string[])v!)
        };
    }
}
```

- [ ] **Step 2: Build to verify it compiles**

Run:
```bash
cd benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark
dotnet build -c Release
```
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add benchmarks/
git commit -m "Spike: shared member-accessor cache from class map"
```

---

## Task 3: DOM materializer (Path A) + correctness verify

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/DomMaterializer.cs`
- Modify: `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/Program.cs`

- [ ] **Step 1: Write the DOM materializer (mimics today's provider)**

`DomMaterializer.cs`:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.EntityFrameworkCore.MaterializationBenchmark;

public static class DomMaterializer
{
    public static BenchmarkEntity Materialize(byte[] bytes)
    {
        // wire bytes -> BsonDocument DOM (the cost the provider pays today)
        var doc = BsonSerializer.Deserialize<BsonDocument>(bytes);
        var e = new BenchmarkEntity();
        foreach (var m in MemberCache.Members)
        {
            if (doc.TryGetValue(m.ElementName, out var value))
            {
                m.Set(e, m.SerializationInfo.DeserializeValue(value));
            }
        }
        return e;
    }
}
```

- [ ] **Step 2: Add a `--verify` path to Program that checks Path A reproduces originals**

Replace `Program.cs` with:

```csharp
using MongoDB.EntityFrameworkCore.MaterializationBenchmark;

if (args.Contains("--verify"))
{
    var entities = CorpusGenerator.GenerateEntities(100);
    var bytes = CorpusGenerator.SerializeToBytes(entities);

    for (var i = 0; i < entities.Length; i++)
    {
        var dom = DomMaterializer.Materialize(bytes[i]);
        if (!entities[i].ValueEquals(dom))
            throw new InvalidOperationException($"DOM path mismatch at index {i}.");
    }

    Console.WriteLine("VERIFY OK: DOM path matches originals.");
    return;
}

Console.WriteLine("Pass --verify to run the correctness check.");
```

- [ ] **Step 3: Run verify and confirm it passes**

Run:
```bash
cd benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark
dotnet run -c Release -- --verify
```
Expected: prints `VERIFY OK: DOM path matches originals.` (If a scalar mismatches — most likely `Created` or `Ref` — confirm Step 3 of Task 1 used UTC/ms-aligned dates and the Guid representation attribute.)

- [ ] **Step 4: Commit**

```bash
git add benchmarks/
git commit -m "Spike: DOM materializer (Path A) + correctness verify"
```

---

## Task 4: Reader materializer (Path B) + extend verify

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/ReaderMaterializer.cs`
- Modify: `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/Program.cs`

- [ ] **Step 1: Write the order-tolerant reader materializer (no DOM)**

`ReaderMaterializer.cs`:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.EntityFrameworkCore.MaterializationBenchmark;

public static class ReaderMaterializer
{
    public static BenchmarkEntity Materialize(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new BsonBinaryReader(stream);
        var context = BsonDeserializationContext.CreateRoot(reader);

        var e = new BenchmarkEntity();
        reader.ReadStartDocument();
        // Forward-only, order-tolerant: dispatch each element by name; skip unmapped.
        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var name = reader.ReadName();
            if (MemberCache.ByElementName.TryGetValue(name, out var m))
            {
                // Reads the value straight off the reader stream (no BsonValue / no DOM).
                m.Set(e, m.Serializer.Deserialize(context));
            }
            else
            {
                reader.SkipValue();
            }
        }
        reader.ReadEndDocument();
        return e;
    }
}
```

- [ ] **Step 2: Extend `--verify` to also check Path B and that A == B**

Replace the `if (args.Contains("--verify")) { ... }` block in `Program.cs` with:

```csharp
if (args.Contains("--verify"))
{
    var entities = CorpusGenerator.GenerateEntities(100);
    var bytes = CorpusGenerator.SerializeToBytes(entities);

    for (var i = 0; i < entities.Length; i++)
    {
        var dom = DomMaterializer.Materialize(bytes[i]);
        var reader = ReaderMaterializer.Materialize(bytes[i]);

        if (!entities[i].ValueEquals(dom))
            throw new InvalidOperationException($"DOM path mismatch at index {i}.");
        if (!entities[i].ValueEquals(reader))
            throw new InvalidOperationException($"Reader path mismatch at index {i}.");
    }

    Console.WriteLine("VERIFY OK: DOM and Reader paths both match originals.");
    return;
}
```

- [ ] **Step 3: Run verify and confirm both paths pass**

Run:
```bash
cd benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark
dotnet run -c Release -- --verify
```
Expected: prints `VERIFY OK: DOM and Reader paths both match originals.` (If the reader path throws "Element ... not valid" or mismatches, the most likely cause is reading the value before `ReadName()` or not skipping unmapped elements — re-check Step 1.)

- [ ] **Step 4: Commit**

```bash
git add benchmarks/
git commit -m "Spike: order-tolerant reader materializer (Path B) + verify A==B"
```

---

## Task 5: BenchmarkDotNet harness

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/MaterializationBenchmarks.cs`
- Modify: `benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark/Program.cs`

- [ ] **Step 1: Write the benchmark class with the correctness gate in GlobalSetup**

`MaterializationBenchmarks.cs`:

```csharp
using BenchmarkDotNet.Attributes;
using MongoDB.Bson.Serialization;

namespace MongoDB.EntityFrameworkCore.MaterializationBenchmark;

[MemoryDiagnoser]
public class MaterializationBenchmarks
{
    [Params(10_000)]
    public int N;

    private byte[][] _corpus = Array.Empty<byte[]>();

    [GlobalSetup]
    public void Setup()
    {
        var entities = CorpusGenerator.GenerateEntities(N);
        _corpus = CorpusGenerator.SerializeToBytes(entities);

        // Correctness gate (untimed): all three paths must reproduce the originals.
        for (var i = 0; i < entities.Length; i++)
        {
            if (!entities[i].ValueEquals(DomMaterializer.Materialize(_corpus[i])))
                throw new InvalidOperationException($"DOM path mismatch at index {i}.");
            if (!entities[i].ValueEquals(ReaderMaterializer.Materialize(_corpus[i])))
                throw new InvalidOperationException($"Reader path mismatch at index {i}.");
            if (!entities[i].ValueEquals(BsonSerializer.Deserialize<BenchmarkEntity>(_corpus[i])))
                throw new InvalidOperationException($"Typed class-map path mismatch at index {i}.");
        }
    }

    [Benchmark(Baseline = true)]
    public BenchmarkEntity? Dom_BsonDocument()
    {
        BenchmarkEntity? last = null;
        foreach (var bytes in _corpus)
            last = DomMaterializer.Materialize(bytes);
        return last;
    }

    [Benchmark]
    public BenchmarkEntity? Reader_RawBytes()
    {
        BenchmarkEntity? last = null;
        foreach (var bytes in _corpus)
            last = ReaderMaterializer.Materialize(bytes);
        return last;
    }

    [Benchmark]
    public BenchmarkEntity? Driver_TypedClassMap()
    {
        BenchmarkEntity? last = null;
        foreach (var bytes in _corpus)
            last = BsonSerializer.Deserialize<BenchmarkEntity>(bytes);
        return last;
    }
}
```

- [ ] **Step 2: Wire BenchmarkRunner into Program for the non-verify path**

Replace the final line of `Program.cs` (`Console.WriteLine("Pass --verify ...");`) with:

```csharp
BenchmarkDotNet.Running.BenchmarkRunner.Run<MaterializationBenchmarks>();
```

- [ ] **Step 3: Build to confirm the harness compiles**

Run:
```bash
cd benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark
dotnet build -c Release
```
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add benchmarks/
git commit -m "Spike: BenchmarkDotNet harness with correctness gate"
```

---

## Task 6: Run the spike and record results

**Files:**
- Create: `docs/superpowers/specs/2026-06-16-dom-free-materialization-results.md`

- [ ] **Step 1: Run the benchmark**

Run:
```bash
cd benchmarks/MongoDB.EntityFrameworkCore.MaterializationBenchmark
dotnet run -c Release
```
Expected: BenchmarkDotNet runs (the GlobalSetup correctness gate passes silently — if it throws, the run aborts) and prints a summary table with Mean, Ratio, Allocated, and Gen0/1/2 for `Dom_BsonDocument` (baseline), `Reader_RawBytes`, and `Driver_TypedClassMap`.

- [ ] **Step 2: Record results and the decision**

Create `docs/superpowers/specs/2026-06-16-dom-free-materialization-results.md`. Paste the BenchmarkDotNet summary table verbatim, then fill in:

```markdown
# DOM-free materialization spike — results

**Run on:** <date>, <CPU / OS / .NET SDK from the BenchmarkDotNet header>
**Corpus:** N = 10,000 documents, entity = 12 scalars + 1 nested doc + 1 array.

## Summary table

<paste BenchmarkDotNet table here>

## Reading

- Reader vs DOM — time: <Ratio>x. Allocations: <DOM Allocated> -> <Reader Allocated> (<reduction>%).
- Reader vs Driver typed class-map (ceiling): <gap in time and allocations>.

## Decision

<One of:>
- GO: Reader path shows materially lower allocations and comparable-or-better time. Proceed to design the order-tolerant reader-shaper codegen (random-access -> forward-dispatch, constructor binding, includes).
- NO-GO: <reason>.
```

- [ ] **Step 3: Commit**

```bash
git add docs/superpowers/specs/2026-06-16-dom-free-materialization-results.md
git commit -m "Spike: record DOM-free materialization benchmark results"
```

---

## Notes for the executor

- **Fairness:** all three paths consume the identical `byte[]` corpus; only result type and materialization differ. The reader path uses a `Dictionary` name→accessor dispatch, which is slightly *pessimistic* versus a compiled switch — so any reader win is conservative.
- **No live MongoDB / no EF:** intentional. This isolates materialization cost. Do not add an `IMongoCollection` round trip or an EF reference.
- **This is throwaway code.** The reader materializer is hand-written for one entity shape. It proves the cost; it is not the general codegen. The follow-on (if GO) is restructuring EF's shaper from random-access `BsonDocument` reads to a forward-only reader pass — that is a separate spec.
```
