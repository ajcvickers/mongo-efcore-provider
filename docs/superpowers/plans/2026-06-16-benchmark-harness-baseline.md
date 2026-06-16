# Benchmark Harness + Baseline (Sub-project A) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up an in-repo BenchmarkDotNet project that measures the current MongoDB EF Core provider end-to-end on the query shapes the migration will touch, and commit baseline numbers.

**Architecture:** A standalone console project under `benchmarks/` (not in the solution) referencing the provider pinned to the `Release EF10` configuration via `SetConfiguration`, targeting `net10.0`. It seeds a `Customer` collection in a real MongoDB during `[GlobalSetup]` and benchmarks five representative EF queries with `[MemoryDiagnoser]`. No provider code changes.

**Tech Stack:** C#, `net10.0`, EF Core 10 (`$(EF10Version)` = 10.0.8), MongoDB.EntityFrameworkCore (project reference), BenchmarkDotNet 0.15.8. Requires a running MongoDB.

---

## Prerequisite for run steps

Tasks 3 and 5 execute queries against a real MongoDB. Before running them, ensure one is reachable:
- Set `MONGODB_URI`, **or** have MongoDB on `mongodb://localhost:27017` (e.g. `docker run -d -p 27017:27017 --name ef-bench-mongo mongo:8`).
If no MongoDB can be started, report those run steps as BLOCKED — do not fake results. Build-only tasks (1, 2, 4) do not need MongoDB.

## File structure

All under `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/`:
- `Directory.Build.props` — empty `<Project/>`; isolation from any future repo-root build props.
- `MongoDB.EntityFrameworkCore.Benchmarks.csproj` — `Exe`, `net10.0`, BenchmarkDotNet + EF10 + provider (pinned `Release EF10`).
- `Customer.cs` — `Customer` entity, owned `Address`, `CustomerKind` enum.
- `BenchmarkDbContext.cs` — `DbContext` with `DbSet<Customer>` and `OwnsOne(Address)`.
- `CustomerSeeder.cs` — deterministic generation of N customers.
- `QueryBenchmarks.cs` — `[MemoryDiagnoser]` harness, GlobalSetup/Cleanup, five `[Benchmark]`s.
- `Program.cs` — entry point: `--smoke` fast validation, else `BenchmarkRunner`.
- `results/2026-06-16-baseline.md` — committed baseline (created in Task 5).

---

## Task 1: Project scaffold + entity + context

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/Directory.Build.props`
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/MongoDB.EntityFrameworkCore.Benchmarks.csproj`
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/Customer.cs`
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/BenchmarkDbContext.cs`
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/Program.cs`

- [ ] **Step 1: Isolation props**

`Directory.Build.props`:
```xml
<Project>
</Project>
```

- [ ] **Step 2: csproj (pins the provider to Release EF10)**

`MongoDB.EntityFrameworkCore.Benchmarks.csproj`:
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
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.15.8" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="$(EF10Version)" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\MongoDB.EntityFrameworkCore\MongoDB.EntityFrameworkCore.csproj"
                      SetConfiguration="Configuration=Release EF10" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Entity, owned type, enum**

`Customer.cs` (a bare `Guid` needs no attribute — the provider maps it to `GuidSerializer.StandardInstance` by default; `Id` is left unset so the provider generates the `ObjectId`):
```csharp
using MongoDB.Bson;

namespace MongoDB.EntityFrameworkCore.Benchmarks;

public enum CustomerKind { Standard, Premium, Vip }

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public int Zip { get; set; }
}

public class Customer
{
    public ObjectId Id { get; set; }
    public int Count { get; set; }
    public long Big { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Quantity { get; set; }
    public Guid Ref { get; set; }
    public DateTime Created { get; set; }
    public decimal Price { get; set; }
    public double Rate { get; set; }
    public bool Active { get; set; }
    public CustomerKind Kind { get; set; }
    public Address Address { get; set; } = new();
}
```

- [ ] **Step 4: DbContext**

`BenchmarkDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;

namespace MongoDB.EntityFrameworkCore.Benchmarks;

public class BenchmarkDbContext : DbContext
{
    public BenchmarkDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().OwnsOne(c => c.Address);
    }
}
```

- [ ] **Step 5: Program stub**

`Program.cs`:
```csharp
Console.WriteLine("MongoDB.EntityFrameworkCore.Benchmarks");
```

- [ ] **Step 6: Restore + build (validates the Release EF10 pin)**

Run:
```bash
cd /Users/arthur.vickers/code/provider2/benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
dotnet build -c Release
```
Expected: build succeeds. This is the critical integration check — it proves `SetConfiguration="Configuration=Release EF10"` makes the `net10.0` benchmark compile against the provider. If it fails with a config error (e.g. the provider building under plain `Release`) or a TFM mismatch, report BLOCKED with the exact error — do NOT change the pin string or TFM without flagging it.

- [ ] **Step 7: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add benchmarks/
git commit -m "Benchmarks: project scaffold + Customer entity + DbContext (Release EF10 pin)"
```

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
dotnet build -c Release
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
dotnet run -c Release -- --smoke
```
Expected: prints `SMOKE OK: where=50, proj=100, ordered=10, tracked=100, noTrack=100, withCity=100`. (`where=50` because half of 100 have `Active==true`; `withCity=100` confirms the owned `Address` round-trips for every row, order-independently.) If it throws on `SaveChanges` or a query, debug it and report what failed — do NOT alter the query shapes or the entity to force a pass. If no MongoDB is reachable, report BLOCKED.

- [ ] **Step 3: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add benchmarks/
git commit -m "Benchmarks: --smoke validation mode"
```

---

## Task 4: Benchmark harness

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/QueryBenchmarks.cs`
- Modify: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/Program.cs`

- [ ] **Step 1: Create the benchmark class**

`QueryBenchmarks.cs`:
```csharp
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;

namespace MongoDB.EntityFrameworkCore.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
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

- [ ] **Step 2: Wire `BenchmarkRunner` into `Program.cs`**

Replace the final line `Console.WriteLine("Pass --smoke to validate, or run with no args to benchmark.");` with:
```csharp
BenchmarkDotNet.Running.BenchmarkRunner.Run<QueryBenchmarks>();
```
(Keep the `--smoke` block above it intact.)

- [ ] **Step 3: Build + re-run smoke**
```bash
cd /Users/arthur.vickers/code/provider2/benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
dotnet build -c Release
dotnet run -c Release -- --smoke
```
Expected: build succeeds; smoke still prints `SMOKE OK: ...`. (Do NOT run the full benchmark here — that's Task 5.)

- [ ] **Step 4: Commit**
```bash
cd /Users/arthur.vickers/code/provider2
git add benchmarks/
git commit -m "Benchmarks: QueryBenchmarks harness (5 shapes, MemoryDiagnoser)"
```

---

## Task 5: Run baseline + record results

**Files:**
- Create: `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-16-baseline.md`

- [ ] **Step 1: Run the full benchmark (needs MongoDB — see Prerequisite)**
```bash
cd /Users/arthur.vickers/code/provider2/benchmarks/MongoDB.EntityFrameworkCore.Benchmarks
dotnet run -c Release
```
IMPORTANT: this takes several minutes and hits MongoDB many times. Set the Bash tool `timeout` to 600000 (10 min). The summary is printed to stdout and saved under `BenchmarkDotNet.Artifacts/results/*-report-github.md`. Capture both the environment header and the results table (Method, Mean, Error, StdDev, Gen0/1/2, Allocated). If the run aborts (e.g. GlobalSetup can't connect), report BLOCKED with the error.

- [ ] **Step 2: Record results**

Create `benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-16-baseline.md`, filling every `<...>` from the actual run:
```markdown
# Provider query baseline — current driver-LINQ path

**Run on:** <date if known>, <CPU / OS / .NET SDK / runtime — from the BenchmarkDotNet header>
**EF config:** EF10 / net10.0. **Dataset:** 10,000 Customers (12 scalars + owned Address).
**Path:** current provider (driver-LINQ translation + BsonDocument-DOM materialization).

## Summary table

<paste the BenchmarkDotNet github-markdown results table verbatim>

## Notes

- This is the reference baseline for the low-level-provider migration (sub-projects B–E).
- Allocations are the portable signal; absolute times are only comparable on the same machine/server.
```

- [ ] **Step 3: Commit ONLY the results doc (NOT `BenchmarkDotNet.Artifacts/`)**
```bash
cd /Users/arthur.vickers/code/provider2
git add benchmarks/MongoDB.EntityFrameworkCore.Benchmarks/results/2026-06-16-baseline.md
git commit -m "Benchmarks: record current-path query baseline"
```
If `git status` shows `BenchmarkDotNet.Artifacts/` as untracked, leave it untracked — do not commit it.

---

## Notes for the executor

- **No provider code changes in this sub-project.** If a benchmark won't run because of a provider bug, report it — do not patch the provider here.
- **The `SetConfiguration` pin (Task 1) is the linchpin.** If the build fails there, stop and flag it; everything else depends on it.
- **`BenchmarkDotNet.Artifacts/` is throwaway** run output — never commit it; only the `results/*.md` summary is committed.
- This harness intentionally measures end-to-end query latency + allocations against a live MongoDB. Materialization-only micro-benchmarks and Include/grouping shapes are deliberately out of scope (later sub-projects).
