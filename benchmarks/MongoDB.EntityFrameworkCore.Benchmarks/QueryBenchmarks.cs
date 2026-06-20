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

        for (var i = 0; i < 10_000; i++)
        {
            var basket = new Basket
            {
                Owner = "owner-" + i,
                Code = i
            };
            for (var j = 0; j < 3; j++)
            {
                basket.Items.Add(new BasketItem
                {
                    Sku = $"sku-{i}-{j}",
                    Qty = j + 1,
                    Price = 1.5m * (j + 1)
                });
            }
            ctx.Baskets.Add(basket);
        }

        ctx.SaveChanges();

        var products = new List<Product>();
        for (var i = 0; i < 100; i++)
        {
            var product = new Product { Title = "product-" + i };
            products.Add(product);
            ctx.Products.Add(product);
        }
        ctx.SaveChanges();

        for (var i = 0; i < 10_000; i++)
        {
            ctx.Reviews.Add(new Review
            {
                Stars = (i % 5) + 1,
                ProductId = products[i % products.Count].Id
            });
        }
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

    [Benchmark] public int Basket_NoTracking_ToList() { using var ctx = new BenchmarkDbContext(_options); return ctx.Baskets.AsNoTracking().ToList().Count; }
    [Benchmark] public int Basket_Tracked_ToList() { using var ctx = new BenchmarkDbContext(_options); return ctx.Baskets.ToList().Count; }

    [Benchmark] public int Review_Include_NoTracking() { using var ctx = new BenchmarkDbContext(_options); return ctx.Reviews.AsNoTracking().Include(r => r.Product).ToList().Count; }
    [Benchmark] public int Review_Include_Tracked() { using var ctx = new BenchmarkDbContext(_options); return ctx.Reviews.Include(r => r.Product).ToList().Count; }
}
