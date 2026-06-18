using MongoDB.Bson;

namespace MongoDB.EntityFrameworkCore.Benchmarks;

public class Review
{
    public ObjectId Id { get; set; }
    public int Stars { get; set; }
    public ObjectId ProductId { get; set; }   // FK on the dependent (Review)
    public Product? Product { get; set; }      // reference navigation to the principal
}

public class Product
{
    public ObjectId Id { get; set; }
    public string Title { get; set; } = "";
}
