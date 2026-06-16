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
