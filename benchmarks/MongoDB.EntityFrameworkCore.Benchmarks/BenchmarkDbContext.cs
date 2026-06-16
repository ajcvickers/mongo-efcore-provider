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
