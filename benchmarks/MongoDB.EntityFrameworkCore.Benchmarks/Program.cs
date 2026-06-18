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
            for (var i = 0; i < 100; i++)
            {
                ctx.FlatItems.Add(new FlatItem
                {
                    Count = i,
                    Big = 1_000_000_000L + i,
                    Name = "flat-" + i,
                    Active = (i % 2) == 0,
                    Rate = i * 0.5
                });
            }
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

            var flat = ctx.FlatItems.ToList();
            var flatActive = ctx.FlatItems.Where(f => f.Active).ToList();

            Console.WriteLine($"FLAT OK: flat={flat.Count}, flatActive={flatActive.Count}");

            if (flat.Count != 100)
                throw new InvalidOperationException($"Expected 100 flat items, got {flat.Count}.");

            // Verify the streaming materializer produced correct scalar values, not just counts.
            var f0 = flat.Single(f => f.Count == 0);
            if (f0.Name != "flat-0" || f0.Big != 1_000_000_000L || f0.Active != true || f0.Rate != 0.0)
                throw new InvalidOperationException($"FlatItem[0] scalars wrong: Name={f0.Name}, Big={f0.Big}, Active={f0.Active}, Rate={f0.Rate}.");
            var f3 = flat.Single(f => f.Count == 3);
            if (f3.Name != "flat-3" || f3.Big != 1_000_000_003L || f3.Active != false || f3.Rate != 1.5)
                throw new InvalidOperationException($"FlatItem[3] scalars wrong: Name={f3.Name}, Big={f3.Big}, Active={f3.Active}, Rate={f3.Rate}.");
        }
    }
    finally
    {
        using var ctx = new BenchmarkDbContext(options);
        ctx.Database.EnsureDeleted();
    }
    return;
}

BenchmarkDotNet.Running.BenchmarkRunner.Run<QueryBenchmarks>();
