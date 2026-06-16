using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.EntityFrameworkCore.MaterializationBenchmark;

public static class DomMaterializer
{
    public static BenchmarkEntity Materialize(byte[] bytes)
    {
        // wire bytes -> BsonDocument DOM (the cost the provider pays today)
        var doc = BsonSerializer.Deserialize<BsonDocument>(bytes);
        var e = new BenchmarkEntity();
        foreach (var m in MemberCache.Members)
        {
            if (doc.TryGetValue(m.ElementName, out var value))
            {
                m.Set(e, m.SerializationInfo.DeserializeValue(value));
            }
        }
        return e;
    }
}
