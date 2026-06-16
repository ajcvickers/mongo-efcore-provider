using MongoDB.EntityFrameworkCore.MaterializationBenchmark;

var entities = CorpusGenerator.GenerateEntities(10);
var bytes = CorpusGenerator.SerializeToBytes(entities);
Console.WriteLine($"Generated {entities.Length} entities, first doc is {bytes[0].Length} bytes.");
