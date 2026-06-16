using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.EntityFrameworkCore.MaterializationBenchmark;

public static class ReaderMaterializer
{
    public static BenchmarkEntity Materialize(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new BsonBinaryReader(stream);
        var context = BsonDeserializationContext.CreateRoot(reader);

        var e = new BenchmarkEntity();
        reader.ReadStartDocument();
        // Forward-only, order-tolerant: dispatch each element by name; skip unmapped.
        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var name = reader.ReadName();
            if (MemberCache.ByElementName.TryGetValue(name, out var m))
            {
                // Reads the value straight off the reader stream (no BsonValue / no DOM).
                m.Set(e, m.Serializer.Deserialize(context));
            }
            else
            {
                reader.SkipValue();
            }
        }
        reader.ReadEndDocument();
        return e;
    }
}
