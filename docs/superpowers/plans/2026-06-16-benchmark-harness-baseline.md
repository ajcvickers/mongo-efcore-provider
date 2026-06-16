# Benchmark Harness + Baseline (Sub-project A) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up an in-repo BenchmarkDotNet project that measures the current MongoDB EF Core provider end-to-end on the query shapes the migration will touch, and commit baseline numbers.

**Architecture:** A standalone console project under `benchmarks/` (not in the solution), targeting `net10.0`, that references the provider via a plain `ProjectReference` and is **always built with `-c "Release EF10"`** so the global `Configuration` flows to NuGet restore *and* build (resolving the provider's config-conditional TFM to `net10.0`). Benchmarks run via the **InProcess toolchain** (BenchmarkDotNet's default toolchain spawns a child build forced to plain `Release`, which breaks the provider's config-conditional csproj). It seeds a `Customer` collection in a real MongoDB during `[GlobalSetup]` and benchmarks five representative EF queries with `MemoryDiagnoser`. No provider code changes.

**Tech Stack:** C#, `net10.0`, EF Core 10 (`$(EF10Version)` = 10.0.8), MongoDB.EntityFrameworkCore (project reference, built `Release EF10`), BenchmarkDotNet 0.15.8. Requires a running MongoDB.

---

## Build/run recipe (applies to every task)

- Build: `dotnet build "<path>" -c "Release EF10"`.
- Run: `dotnet run -c "Release EF10"` (from the project directory), `-- --smoke` for the validation mode.
- **Never** use plain `-c Release` / `-c Debug` — the project declares only `Debug EF10;Release EF10`, and plain configs break the provider reference.

## Prerequisite for run steps

Tasks 3 and 5 execute queries against a real MongoDB. Before running them, ensure one is reachable:
- Set `MONGODB_URI`, **or** have MongoDB on `mongodb://localhost:27017` (e.g. `docker run -d -p 27017:27017 --name ef-bench-mongo mongo:8`).
If no MongoDB can be started, report those run steps as BLOCKED — do not fake results. Build-only tasks (2, 4) do not need MongoDB.

## File structure

All under `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/` (plus `benchmarks/.gitignore`):
- `Directory.Build.props` — empty `<Project/>`; isolation from any future repo-root build props.
- `MongoDB.EntityFrameworkCore.Benchmarks.csproj` — `Exe`, `net10.0`, `Configurations=Debug EF10;Release EF10`, `Optimize` under `Release EF10`, BenchmarkDotNet + EF10 + plain provider reference.
- `Customer.cs` — `Customer` entity, owned `Address`, `CustomerKind` enum.
- `BenchmarkDbContext.cs` — `DbContext` with `DbSet<Customer>` and `OwnsOne(Address)`.
- `CustomerSeeder.cs` — deterministic generation of N customers.
- `BenchmarkConfig.cs` — `ManualConfig` selecting the InProcess toolchain + `MemoryDiagnoser` + capped job.
- `QueryBenchmarks.cs` — `[Config(typeof(BenchmarkConfig))]` harness, GlobalSetup/Cleanup, five `[Benchmark]`s.
- `Program.cs` — entry point: `--smoke` fast validation, else `BenchmarkRunner`.
- `../.gitignore` — excludes `BenchmarkDotNet.Artifacts/`.
- `results/2026-06-16-baseline.md` — committed baseline (created in Task 5).

---

## Task 1: Project scaffold + entity + context — ✅ DONE (commit b667a7b)

Already implemented and committed (the build-integration recipe was established by experiment). For reference, the final files are:

- [x] `benchmarks/.gitignore`:
```
BenchmarkDotNet.Artifacts/
```

- [x] `Directory.Build.props`:
```xml
<Project>
</Project>
```

- [x] `MongoDB.EntityFrameworkCore.Benchmarks.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Import Project="..\..\Versions.props" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <AssemblyName>MongoDB.EntityFrameworkCore.Benchmarks</AssemblyName>
    <Configurations>Debug EF10;Release EF10</Configurations>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)' == 'Release EF10'">
    <Optimize>true</Optimize>
    <DefineConstants>$(DefineConstants);RELEASE</DefineConstants>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.15.8" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="$(EF10Version)" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\MongoDB.EntityFrameworkCore\MongoDB.EntityFrameworkCore.csproj" />
  </ItemGroup>

</Project>
```

- [x] `Customer.cs` — `Customer` (`ObjectId Id`, `int Count`, `long Big`, `string Name`, `string Description`, `int Quantity`, `Guid Ref`, `DateTime Created`, `decimal Price`, `double Rate`, `bool Active`, `CustomerKind Kind`, `Address Address`), `Address` (`Street`/`City`/`Zip`), `enum CustomerKind { Standard, Premium, Vip }`. A bare `Guid` needs no attribute (provider maps it to `GuidSerializer.StandardInstance`); `Id` is left unset for the provider to generate.

- [x] `BenchmarkDbContext.cs` — `DbSet<Customer> Customers`; `OnModelCreating` calls `modelBuilder.Entity<Customer>().OwnsOne(c => c.Address)`.

- [x] `Program.cs` — stub: `Console.WriteLine("MongoDB.EntityFrameworkCore.Benchmarks");`.

Verified: `dotnet build ... -c "Release EF10"` succeeds (provider resolves to `bin/Release EF10/net10.0/...`).

---

## Task 2: Deterministic seeder

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/CustomerSeeder.cs`

- [ ] **Step 1: Seeder**

`CustomerSeeder.cs`:
```csharp
namespace MongoDB.EntityFrameworkCore.Benchmarks;

public static class CustomerSeeder
{
    // Whole-millisecond UTC base so BSON datetime round-trips exactly.
    private static readonly DateTime BaseUtc = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static List<Customer> Generate(int n)
    {
        var list = new List<Customer>(n);
        for (var i = 0; i < n; i++)
        {
            list.Add(new Customer
            {
                Count = i,
                Big = 1_000_000_000L + i,
                Name = "name-" + i,
                Description = "description text for customer " + i,
                Quantity = i % 500,
                Ref = new Guid(i, (short)(i % 100), (short)(i % 7), 1, 2, 3, 4, 5, 6, 7, 8),
                Created = BaseUtc.AddMilliseconds(i * 1000),
                Price = 1.23m + i,
                Rate = i * 0.5,
                Active = (i % 2) == 0,
                Kind = (CustomerKind)(i % 3),
                Address = new Address { Street = "street-" + i, City = "city-" + (i % 50), Zip = 10000 + i }
            });
        }
        return list;
    }
}
```

- [ ] **Step 2: Build**
```bash
cd /Users/arthur.vickers/code/provider2/benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
dotnet build -c "Release EF10"
```
Expected: build succeeds.

- [ ] **Step 3: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add benchmarks/
git commit -m "Benchmarks: deterministic Customer seeder"
```

---

## Task 3: Smoke mode (validates model + connectivity + queries)

**Files:**
- Modify: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/Program.cs`

- [ ] **Step 1: Replace `Program.cs` with the smoke harness**

```csharp
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Benchmarks;

if (args.Contains("--smoke"))
{
    var conn = Environment.GetEnvironmentVariable("MONGODB_URI") ?? "mongodb://localhost:27017";
    var dbName = "ef_bench_smoke_" + Guid.NewGuid().ToString("N");
    var options = new DbContextOptionsBuilder<BenchmarkDbContext>().UseMongoDB(conn, dbName).Options;

    try
    {
        using (var ctx = new BenchmarkDbContext(options))
        {
            ctx.Database.EnsureCreated();
            ctx.Customers.AddRange(CustomerSeeder.Generate(100));
            ctx.SaveChanges();
        }

        using (var ctx = new BenchmarkDbContext(options))
        {
            var where = ctx.Customers.Where(c => c.Active).ToList();
            var proj = ctx.Customers.Select(c => new { c.Name, c.Count }).ToList();
            var ordered = ctx.Customers.OrderBy(c => c.Count).Take(10).ToList();
            var tracked = ctx.Customers.ToList();
            var noTrack = ctx.Customers.AsNoTracking().ToList();

            // Order-independent: ToList() has no defined order, so don't index by position.
            var withCity = tracked.Count(c => !string.IsNullOrEmpty(c.Address.City));

            Console.WriteLine(
                $"SMOKE OK: where={where.Count}, proj={proj.Count}, ordered={ordered.Count}, " +
                $"tracked={tracked.Count}, noTrack={noTrack.Count}, withCity={withCity}");

            if (tracked.Count != 100)
                throw new InvalidOperationException($"Expected 100 tracked customers, got {tracked.Count}.");
            if (withCity != 100)
                throw new InvalidOperationException($"Owned Address.City did not round-trip for all rows ({withCity}/100).");
        }
    }
    finally
    {
        using var ctx = new BenchmarkDbContext(options);
        ctx.Database.EnsureDeleted();
    }
    return;
}

Console.WriteLine("Pass --smoke to validate, or run with no args to benchmark.");
```

- [ ] **Step 2: Run the smoke check (needs MongoDB — see Prerequisite)**
```bash
cd /Users/arthur.vickers/code/provider2/benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
dotnet run -c "Release EF10" -- --smoke
```
Expected: prints `SMOKE OK: where=50, proj=100, ordered=10, tracked=100, noTrack=100, withCity=100`. (`where=50` because half of 100 have `Active==true`; `withCity=100` confirms the owned `Address` round-trips for every row, order-independently.) If it throws on `SaveChanges` or a query, debug it and report what failed — do NOT alter the query shapes or the entity to force a pass. If no MongoDB is reachable, report BLOCKED.

- [ ] **Step 3: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add benchmarks/
git commit -m "Benchmarks: --smoke validation mode"
```

---

## Task 4: Benchmark harness (InProcess)

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/BenchmarkConfig.cs`
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/QueryBenchmarks.cs`
- Modify: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/Program.cs`

- [ ] **Step 1: InProcess config**

`BenchmarkConfig.cs` (the InProcess toolchain is required — see Architecture):
```csharp
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace MongoDB.EntityFrameworkCore.Benchmarks;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithWarmupCount(3)
            .WithIterationCount(10));
        AddDiagnoser(MemoryDiagnoser.Default);
    }
}
```

- [ ] **Step 2: Benchmark class**

`QueryBenchmarks.cs`:
```csharp
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;

namespace MongoDB.EntityFrameworkCore.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public class QueryBenchmarks
{
    private DbContextOptions<BenchmarkDbContext> _options = null!;

    [GlobalSetup]
    public void Setup()
    {
        var conn = Environment.GetEnvironmentVariable("MONGODB_URI") ?? "mongodb://localhost:27017";
        var dbName = "ef_bench_" + Guid.NewGuid().ToString("N");
        _options = new DbContextOptionsBuilder<BenchmarkDbContext>().UseMongoDB(conn, dbName).Options;

        using var ctx = new BenchmarkDbContext(_options);
        ctx.Database.EnsureCreated();
        ctx.Customers.AddRange(CustomerSeeder.Generate(10_000));
        ctx.SaveChanges();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        using var ctx = new BenchmarkDbContext(_options);
        ctx.Database.EnsureDeleted();
    }

    [Benchmark]
    public int Where_ToList()
    {
        using var ctx = new BenchmarkDbContext(_options);
        return ctx.Customers.Where(c => c.Active).ToList().Count;
    }

    [Benchmark]
    public int Projection_ToList()
    {
        using var ctx = new BenchmarkDbContext(_options);
        return ctx.Customers.Select(c => new { c.Name, c.Count }).ToList().Count;
    }

    [Benchmark]
    public int OrderBy_Take()
    {
        using var ctx = new BenchmarkDbContext(_options);
        return ctx.Customers.OrderBy(c => c.Count).Take(100).ToList().Count;
    }

    [Benchmark]
    public int Tracked_ToList()
    {
        using var ctx = new BenchmarkDbContext(_options);
        return ctx.Customers.ToList().Count;
    }

    [Benchmark]
    public int NoTracking_ToList()
    {
        using var ctx = new BenchmarkDbContext(_options);
        return ctx.Customers.AsNoTracking().ToList().Count;
    }
}
```

- [ ] **Step 3: Wire `BenchmarkRunner` into `Program.cs`**

Replace the final line `Console.WriteLine("Pass --smoke to validate, or run with no args to benchmark.");` with:
```csharp
BenchmarkDotNet.Running.BenchmarkRunner.Run<QueryBenchmarks>();
```
(Keep the `--smoke` block above it intact.)

- [ ] **Step 4: Build + re-run smoke**
```bash
cd /Users/arthur.vickers/code/provider2/benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
dotnet build -c "Release EF10"
dotnet run -c "Release EF10" -- --smoke
```
Expected: build succeeds; smoke still prints `SMOKE OK: ...`. (Do NOT run the full benchmark here — that's Task 5.)

- [ ] **Step 5: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add benchmarks/
git commit -m "Benchmarks: QueryBenchmarks harness (5 shapes, InProcess + MemoryDiagnoser)"
```

---

## Task 5: Run baseline + record results

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-16-baseline.md`

- [ ] **Step 1: Run the full benchmark (needs MongoDB — see Prerequisite)**
```bash
cd /Users/arthur.vickers/code/provider2/benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
dotnet run -c "Release EF10"
```
IMPORTANT: this takes several minutes and hits MongoDB many times. Set the Bash tool `timeout` to 600000 (10 min). The summary is printed to stdout and saved under `BenchmarkDotNet.Artifacts/results/*-report-github.md`. Capture both the environment header and the results table (Method, Mean, Error, StdDev, Gen0/1/2, Allocated). A benign warning `Failed to set up priority High ... Permission denied` may appear — ignore it. If the run aborts (e.g. GlobalSetup can't connect), report BLOCKED with the error.

- [ ] **Step 2: Record results**

Create `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-16-baseline.md`, filling every `<...>` from the actual run:
```markdown
# Provider query baseline — current driver-LINQ path

**Run on:** <date if known>, <CPU / OS / .NET SDK / runtime — from the BenchmarkDotNet header>
**EF config:** EF10 / net10.0 (InProcess toolchain). **Dataset:** 10,000 Customers (12 scalars + owned Address).
**Path:** current provider (driver-LINQ translation + BsonDocument-DOM materialization).

## Summary table

<paste the BenchmarkDotNet github-markdown results table verbatim>

## Notes

- This is the reference baseline for the low-level-provider migration (sub-projects B–E).
- Allocations are the portable signal; absolute times are only comparable on the same machine/server.
```

- [ ] **Step 3: Commit the results doc** (`BenchmarkDotNet.Artifacts/` is gitignored, so it won't be staged)
```bash
cd /Users/arthur.vickers/code/provider2
git add benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-16-baseline.md
git commit -m "Benchmarks: record current-path query baseline"
```

---

## Notes for the executor

- **No provider code changes in this sub-project.** If a benchmark won't run because of a provider bug, report it — do not patch the provider here.
- **Always use `-c "Release EF10"`.** Plain `Release`/`Debug` will not build (the provider's EF reference is config-conditional; only EF10 configs add EF10 + `net10.0`).
- **InProcess toolchain is required** (`BenchmarkConfig`). Do not switch to the default toolchain — its child build forces plain `Release` and breaks the provider reference.
- **`BenchmarkDotNet.Artifacts/` is gitignored** (`benchmarks/.gitignore`); only the `results/*.md` summary is committed.
- This harness intentionally measures end-to-end query latency + allocations against a live MongoDB. Materialization-only micro-benchmarks and Include/grouping shapes are deliberately out of scope (later sub-projects).
