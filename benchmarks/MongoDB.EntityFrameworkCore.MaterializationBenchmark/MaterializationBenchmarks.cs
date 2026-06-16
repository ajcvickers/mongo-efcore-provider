using BenchmarkDotNet.Attributes;
using MongoDB.Bson.Serialization;

namespace MongoDB.EntityFrameworkCore.MaterializationBenchmark;

[MemoryDiagnoser]
public class MaterializationBenchmarks
{
    [Params(10_000)]
    public int N;

    private byte[][] _corpus = Array.Empty<byte[]>();

    [GlobalSetup]
    public void Setup()
    {
        var entities = CorpusGenerator.GenerateEntities(N);
        _corpus = CorpusGenerator.SerializeToBytes(entities);

        // Correctness gate (untimed): all three paths must reproduce the originals.
        for (var i = 0; i < entities.Length; i++)
        {
            if (!entities[i].ValueEquals(DomMaterializer.Materialize(_corpus[i])))
                throw new InvalidOperationException($"DOM path mismatch at index {i}.");
            if (!entities[i].ValueEquals(ReaderMaterializer.Materialize(_corpus[i])))
                throw new InvalidOperationException($"Reader path mismatch at index {i}.");
            if (!entities[i].ValueEquals(BsonSerializer.Deserialize<BenchmarkEntity>(_corpus[i])))
                throw new InvalidOperationException($"Typed class-map path mismatch at index {i}.");
        }
    }

    [Benchmark(Baseline = true)]
    public BenchmarkEntity? Dom_BsonDocument()
    {
        BenchmarkEntity? last = null;
        foreach (var bytes in _corpus)
            last = DomMaterializer.Materialize(bytes);
        return last;
    }

    [Benchmark]
    public BenchmarkEntity? Reader_RawBytes()
    {
        BenchmarkEntity? last = null;
        foreach (var bytes in _corpus)
            last = ReaderMaterializer.Materialize(bytes);
        return last;
    }

    [Benchmark]
    public BenchmarkEntity? Driver_TypedClassMap()
    {
        BenchmarkEntity? last = null;
        foreach (var bytes in _corpus)
            last = BsonSerializer.Deserialize<BenchmarkEntity>(bytes);
        return last;
    }
}
