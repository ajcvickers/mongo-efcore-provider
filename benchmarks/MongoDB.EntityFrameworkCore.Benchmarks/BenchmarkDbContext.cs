using Microsoft.EntityFrameworkCore;

namespace MongoDB.EntityFrameworkCore.Benchmarks;

public class BenchmarkDbContext : DbContext
{
    public BenchmarkDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<FlatItem> FlatItems => Set<FlatItem>();

    public DbSet<Basket> Baskets => Set<Basket>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().OwnsOne(c => c.Address);
        modelBuilder.Entity<FlatItem>();
        modelBuilder.Entity<Basket>().OwnsMany(b => b.Items);
        modelBuilder.Entity<Review>().HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId);
    }
}
