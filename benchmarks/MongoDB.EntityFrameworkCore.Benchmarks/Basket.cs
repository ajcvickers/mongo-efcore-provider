using MongoDB.Bson;

namespace MongoDB.EntityFrameworkCore.Benchmarks;

public class Basket
{
    public ObjectId Id { get; set; }
    public string Owner { get; set; } = "";
    public int Code { get; set; }
    public List<BasketItem> Items { get; set; } = new();
}

public class BasketItem
{
    public string Sku { get; set; } = "";
    public int Qty { get; set; }
    public decimal Price { get; set; }
}
