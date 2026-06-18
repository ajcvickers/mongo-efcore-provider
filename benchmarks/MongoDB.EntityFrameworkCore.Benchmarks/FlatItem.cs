using MongoDB.Bson;

namespace MongoDB.EntityFrameworkCore.Benchmarks;

public class FlatItem
{
    public ObjectId Id { get; set; }
    public int Count { get; set; }
    public long Big { get; set; }
    public string Name { get; set; } = "";
    public bool Active { get; set; }
    public double Rate { get; set; }
}
