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
