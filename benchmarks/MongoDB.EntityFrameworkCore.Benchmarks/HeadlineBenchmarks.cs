using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoDB.EntityFrameworkCore.Benchmarks;

// Three-config headline benchmarks:
//   DriverOnly      - raw MongoDB C# driver LINQ / aggregation, no EF Core at all (perf floor).
//   EF-DriverLinq   - EF provider with UseNativeQuery(false): driver-LINQ + BsonDocument DOM path (== current main).
//   EF-Native       - EF provider with UseNativeQuery(true) (default): native MQL + streaming materialization.
//
// All three read the SAME documents seeded once in [GlobalSetup]. EF stores entities in collections named by
// the CLR short name (Customer, FlatItem, Review, Product) — the driver's default conventions deserialize the
// same documents into the same POCOs, so DriverOnly reads them directly.
[Config(typeof(BenchmarkConfig))]
public class HeadlineBenchmarks
{
    private const int N = 10_000;

    private DbContextOptions<BenchmarkDbContext> _domOptions = null!;     // EF-DriverLinq  (UseNativeQuery(false))
    private DbContextOptions<BenchmarkDbContext> _nativeOptions = null!;  // EF-Native      (UseNativeQuery(true))

    private IMongoCollection<FlatItem> _flatColl = null!;
    private IMongoCollection<Review> _reviewColl = null!;

    [GlobalSetup]
    public void Setup()
    {
        var conn = Environment.GetEnvironmentVariable("MONGODB_URI") ?? "mongodb://localhost:27017";
        var dbName = "ef_bench_headline_" + Guid.NewGuid().ToString("N");

        // Single shared MongoClient for ALL three configs. The driver-only config reuses one client
        // across invocations, so the EF configs must too (via the IMongoClient UseMongoDB overload) —
        // otherwise each `new BenchmarkDbContext` would spin up a fresh MongoClient (connection pool +
        // SDAM topology monitoring), charging client-startup to the EF numbers per invocation and making
        // the comparison unfair. MongoClient is thread-safe and designed to be shared.
        var client = new MongoClient(conn);

        _domOptions = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseMongoDB(client, dbName, o => o.UseNativeQuery(false)).Options;
        _nativeOptions = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseMongoDB(client, dbName, o => o.UseNativeQuery(true)).Options;

        // Seed once (config-agnostic: documents are identical).
        using (var ctx = new BenchmarkDbContext(_nativeOptions))
        {
            ctx.Database.EnsureCreated();

            for (var i = 0; i < N; i++)
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

            var products = new List<Product>();
            for (var i = 0; i < 100; i++)
            {
                var product = new Product { Title = "product-" + i };
                products.Add(product);
                ctx.Products.Add(product);
            }
            ctx.SaveChanges();

            for (var i = 0; i < N; i++)
            {
                ctx.Reviews.Add(new Review
                {
                    Stars = (i % 5) + 1,
                    ProductId = products[i % products.Count].Id
                });
            }
            ctx.SaveChanges();
        }

        // Raw driver collections against the same db / EF collection names — same shared client as the EF configs.
        var db = client.GetDatabase(dbName);
        // EF names collections after the DbSet (pluralized): FlatItems / Reviews / Products.
        _flatColl = db.GetCollection<FlatItem>("FlatItems");
        _reviewColl = db.GetCollection<Review>("Reviews");

        // Round-trip / mapping validation so we never report numbers from a divergent read.
        Validate();
    }

    private void Validate()
    {
        var driverWhere = _flatColl.AsQueryable().Where(f => f.Active).ToList().Count;
        var driverAll = _flatColl.AsQueryable().ToList().Count;
        var driverReview = DriverReviewInclude();

        using var ef = new BenchmarkDbContext(_nativeOptions);
        var efWhere = ef.FlatItems.AsNoTracking().Where(f => f.Active).ToList().Count;
        var efAll = ef.FlatItems.AsNoTracking().ToList().Count;
        var efReview = ef.Reviews.AsNoTracking().Include(r => r.Product).Count(r => r.Product != null);

        // Verify driver actually materialized scalar values, not just counts.
        var f0 = _flatColl.AsQueryable().First(f => f.Count == 0);
        var f3 = _flatColl.AsQueryable().First(f => f.Count == 3);

        if (driverAll != N || efAll != N)
            throw new InvalidOperationException($"FlatItem count mismatch: driver={driverAll}, ef={efAll}, expected {N}.");
        if (driverWhere != efWhere)
            throw new InvalidOperationException($"Where count mismatch: driver={driverWhere}, ef={efWhere}.");
        if (driverReview != N || efReview != N)
            throw new InvalidOperationException($"Review+Product count mismatch: driver={driverReview}, ef={efReview}, expected {N}.");
        if (f0.Name != "flat-0" || f0.Big != 1_000_000_000L || f0.Active != true || f0.Rate != 0.0)
            throw new InvalidOperationException($"DriverOnly FlatItem[0] scalars wrong: Name={f0.Name}, Big={f0.Big}, Active={f0.Active}, Rate={f0.Rate}.");
        if (f3.Name != "flat-3" || f3.Big != 1_000_000_003L || f3.Active != false || f3.Rate != 1.5)
            throw new InvalidOperationException($"DriverOnly FlatItem[3] scalars wrong: Name={f3.Name}, Big={f3.Big}, Active={f3.Active}, Rate={f3.Rate}.");
    }

    // Hand-written $lookup + $unwind equivalent of Reviews.Include(r => r.Product).
    private int DriverReviewInclude()
    {
        var reviews = _reviewColl
            .Aggregate()
            .Lookup<Review, Product, ReviewWithProduct>(
                foreignCollection: _reviewColl.Database.GetCollection<Product>("Products"),
                localField: r => r.ProductId,
                foreignField: p => p.Id,
                @as: rp => rp.Products)
            .ToList();

        var count = 0;
        foreach (var rp in reviews)
        {
            var review = new Review
            {
                Id = rp.Id,
                Stars = rp.Stars,
                ProductId = rp.ProductId,
                Product = rp.Products.FirstOrDefault()
            };
            if (review.Product != null) count++;
        }
        return count;
    }

    private sealed class ReviewWithProduct
    {
        public ObjectId Id { get; set; }
        public int Stars { get; set; }
        public ObjectId ProductId { get; set; }
        public List<Product> Products { get; set; } = new();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        using var ctx = new BenchmarkDbContext(_nativeOptions);
        ctx.Database.EnsureDeleted();
    }

    // ----- WhereToList: Where(x => x.Active).ToList() -----

    [Benchmark] public int WhereToList_DriverOnly()
        => _flatColl.AsQueryable().Where(f => f.Active).ToList().Count;

    [Benchmark] public int WhereToList_EF_DriverLinq()
    { using var ctx = new BenchmarkDbContext(_domOptions); return ctx.FlatItems.AsNoTracking().Where(f => f.Active).ToList().Count; }

    [Benchmark] public int WhereToList_EF_Native()
    { using var ctx = new BenchmarkDbContext(_nativeOptions); return ctx.FlatItems.AsNoTracking().Where(f => f.Active).ToList().Count; }

    // ----- WholeEntityToList: ToList() of a whole entity -----
    // 3-way uses AsNoTracking (DriverOnly has no change tracker). EF tracked variants reported separately.

    [Benchmark] public int WholeEntityToList_DriverOnly()
        => _flatColl.AsQueryable().ToList().Count;

    [Benchmark] public int WholeEntityToList_EF_DriverLinq_NoTracking()
    { using var ctx = new BenchmarkDbContext(_domOptions); return ctx.FlatItems.AsNoTracking().ToList().Count; }

    [Benchmark] public int WholeEntityToList_EF_Native_NoTracking()
    { using var ctx = new BenchmarkDbContext(_nativeOptions); return ctx.FlatItems.AsNoTracking().ToList().Count; }

    [Benchmark] public int WholeEntityToList_EF_DriverLinq_Tracked()
    { using var ctx = new BenchmarkDbContext(_domOptions); return ctx.FlatItems.ToList().Count; }

    [Benchmark] public int WholeEntityToList_EF_Native_Tracked()
    { using var ctx = new BenchmarkDbContext(_nativeOptions); return ctx.FlatItems.ToList().Count; }

    // ----- OrderByTake: OrderBy(x => x.Count).Take(100).ToList() -----

    [Benchmark] public int OrderByTake_DriverOnly()
        => _flatColl.AsQueryable().OrderBy(f => f.Count).Take(100).ToList().Count;

    [Benchmark] public int OrderByTake_EF_DriverLinq()
    { using var ctx = new BenchmarkDbContext(_domOptions); return ctx.FlatItems.AsNoTracking().OrderBy(f => f.Count).Take(100).ToList().Count; }

    [Benchmark] public int OrderByTake_EF_Native()
    { using var ctx = new BenchmarkDbContext(_nativeOptions); return ctx.FlatItems.AsNoTracking().OrderBy(f => f.Count).Take(100).ToList().Count; }

    // ----- ReferenceInclude: Reviews.Include(r => r.Product).ToList() -----

    [Benchmark] public int ReferenceInclude_DriverOnly()
        => DriverReviewInclude();

    [Benchmark] public int ReferenceInclude_EF_DriverLinq()
    { using var ctx = new BenchmarkDbContext(_domOptions); return ctx.Reviews.AsNoTracking().Include(r => r.Product).ToList().Count; }

    [Benchmark] public int ReferenceInclude_EF_Native()
    { using var ctx = new BenchmarkDbContext(_nativeOptions); return ctx.Reviews.AsNoTracking().Include(r => r.Product).ToList().Count; }
}
