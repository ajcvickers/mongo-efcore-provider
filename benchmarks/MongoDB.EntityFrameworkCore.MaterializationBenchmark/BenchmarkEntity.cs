using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.EntityFrameworkCore.MaterializationBenchmark;

public enum Kind { A, B, C }

// Record => value equality for free; exercises the driver's constructor (creator) binding on read.
public record Address(string Street, string City, int Zip);

// Mutable class with parameterless ctor => exercises the property-set materialization path.
public sealed class BenchmarkEntity
{
    public ObjectId Id { get; set; }
    public int Count { get; set; }
    public long Big { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Quantity { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Ref { get; set; }

    public DateTime Created { get; set; }
    public decimal Price { get; set; }
    public double Rate { get; set; }
    public bool Active { get; set; }
    public Kind Kind { get; set; }

    public Address Address { get; set; } = new("", "", 0);
    public string[] Tags { get; set; } = Array.Empty<string>();

    public bool ValueEquals(BenchmarkEntity o) =>
        Id == o.Id && Count == o.Count && Big == o.Big && Name == o.Name
        && Description == o.Description && Quantity == o.Quantity && Ref == o.Ref
        && Created == o.Created && Price == o.Price && Rate.Equals(o.Rate)
        && Active == o.Active && Kind == o.Kind && Address == o.Address
        && Tags.SequenceEqual(o.Tags);
}
