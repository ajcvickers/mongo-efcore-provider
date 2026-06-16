using MongoDB.Bson;

namespace MongoDB.EntityFrameworkCore.MaterializationBenchmark;

public static class CorpusGenerator
{
    // Fixed base date aligned to whole milliseconds so BSON UTC-datetime round-trips exactly.
    private static readonly DateTime BaseUtc = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static BenchmarkEntity[] GenerateEntities(int n)
    {
        var result = new BenchmarkEntity[n];
        for (var i = 0; i < n; i++)
        {
            result[i] = new BenchmarkEntity
            {
                Id = ObjectId.GenerateNewId(),
                Count = i,
                Big = 1_000_000_000L + i,
                Name = "name-" + i,
                Description = "description text for entity number " + i,
                Quantity = i % 500,
                Ref = new Guid(i, (short)(i % 100), (short)(i % 7), 1, 2, 3, 4, 5, 6, 7, 8),
                Created = BaseUtc.AddMilliseconds(i * 1000),
                Price = 1.23m + i,
                Rate = i * 0.5,
                Active = (i % 2) == 0,
                Kind = (Kind)(i % 3),
                Address = new Address("street-" + i, "city-" + (i % 50), 10000 + i),
                Tags = new[] { "t" + (i % 3), "t" + (i % 5), "t" + (i % 7) }
            };
        }
        return result;
    }

    public static byte[][] SerializeToBytes(BenchmarkEntity[] entities)
    {
        var result = new byte[entities.Length][];
        for (var i = 0; i < entities.Length; i++)
        {
            result[i] = entities[i].ToBson(); // uses the driver's registered class-map serializer
        }
        return result;
    }
}
