using MongoDB.Bson.Serialization;

namespace MongoDB.EntityFrameworkCore.MaterializationBenchmark;

public sealed class MemberAccessor
{
    public required string ElementName { get; init; }
    public required IBsonSerializer Serializer { get; init; }
    public required Action<BenchmarkEntity, object?> Set { get; init; }

    // Reusable for the DOM path (BsonSerializationInfo.DeserializeValue(BsonValue)).
    public BsonSerializationInfo SerializationInfo =>
        new(ElementName, Serializer, Serializer.ValueType);
}

public static class MemberCache
{
    // Built once from the driver's class map for BenchmarkEntity.
    public static readonly IReadOnlyList<MemberAccessor> Members = Build();

    public static readonly IReadOnlyDictionary<string, MemberAccessor> ByElementName =
        Members.ToDictionary(m => m.ElementName);

    private static MemberAccessor[] Build()
    {
        var cm = BsonClassMap.LookupClassMap(typeof(BenchmarkEntity));

        MemberAccessor For(string memberName, Action<BenchmarkEntity, object?> set)
        {
            var mm = cm.GetMemberMap(memberName)
                     ?? throw new InvalidOperationException($"No member map for '{memberName}'.");
            return new MemberAccessor
            {
                ElementName = mm.ElementName,
                Serializer = mm.GetSerializer(),
                Set = set
            };
        }

        return new[]
        {
            For(nameof(BenchmarkEntity.Id),          (e, v) => e.Id = (MongoDB.Bson.ObjectId)v!),
            For(nameof(BenchmarkEntity.Count),       (e, v) => e.Count = (int)v!),
            For(nameof(BenchmarkEntity.Big),         (e, v) => e.Big = (long)v!),
            For(nameof(BenchmarkEntity.Name),        (e, v) => e.Name = (string)v!),
            For(nameof(BenchmarkEntity.Description), (e, v) => e.Description = (string)v!),
            For(nameof(BenchmarkEntity.Quantity),    (e, v) => e.Quantity = (int)v!),
            For(nameof(BenchmarkEntity.Ref),         (e, v) => e.Ref = (Guid)v!),
            For(nameof(BenchmarkEntity.Created),     (e, v) => e.Created = (DateTime)v!),
            For(nameof(BenchmarkEntity.Price),       (e, v) => e.Price = (decimal)v!),
            For(nameof(BenchmarkEntity.Rate),        (e, v) => e.Rate = (double)v!),
            For(nameof(BenchmarkEntity.Active),      (e, v) => e.Active = (bool)v!),
            For(nameof(BenchmarkEntity.Kind),        (e, v) => e.Kind = (Kind)v!),
            For(nameof(BenchmarkEntity.Address),     (e, v) => e.Address = (Address)v!),
            For(nameof(BenchmarkEntity.Tags),        (e, v) => e.Tags = (string[])v!)
        };
    }
}
