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
