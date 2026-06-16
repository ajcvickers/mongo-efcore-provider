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
