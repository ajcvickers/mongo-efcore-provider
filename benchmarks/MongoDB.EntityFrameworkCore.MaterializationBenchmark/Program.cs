using MongoDB.EntityFrameworkCore.MaterializationBenchmark;

if (args.Contains("--verify"))
{
    var entities = CorpusGenerator.GenerateEntities(100);
    var bytes = CorpusGenerator.SerializeToBytes(entities);

    for (var i = 0; i < entities.Length; i++)
    {
        var dom = DomMaterializer.Materialize(bytes[i]);
        if (!entities[i].ValueEquals(dom))
            throw new InvalidOperationException($"DOM path mismatch at index {i}.");
    }

    Console.WriteLine("VERIFY OK: DOM path matches originals.");
    return;
}

Console.WriteLine("Pass --verify to run the correctness check.");
