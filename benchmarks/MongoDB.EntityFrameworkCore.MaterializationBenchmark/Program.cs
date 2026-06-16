using MongoDB.EntityFrameworkCore.MaterializationBenchmark;

if (args.Contains("--verify"))
{
    var entities = CorpusGenerator.GenerateEntities(100);
    var bytes = CorpusGenerator.SerializeToBytes(entities);

    for (var i = 0; i < entities.Length; i++)
    {
        var dom = DomMaterializer.Materialize(bytes[i]);
        var reader = ReaderMaterializer.Materialize(bytes[i]);

        if (!entities[i].ValueEquals(dom))
            throw new InvalidOperationException($"DOM path mismatch at index {i}.");
        if (!entities[i].ValueEquals(reader))
            throw new InvalidOperationException($"Reader path mismatch at index {i}.");
    }

    Console.WriteLine("VERIFY OK: DOM and Reader paths both match originals.");
    return;
}

BenchmarkDotNet.Running.BenchmarkRunner.Run<MaterializationBenchmarks>();
