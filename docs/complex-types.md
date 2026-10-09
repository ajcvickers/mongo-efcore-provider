# Complex types

EF Core complex types map value objects that have no identity of their own (an address, a geo point, a money amount)
as part of an entity. The MongoDB provider stores a complex property as an embedded subdocument, and a complex
collection as an array of subdocuments, inside the entity's document.

Complex types are different from owned entity types (`OwnsOne`/`OwnsMany`), which the provider also stores as
embedded documents: owned types are entity types with a (hidden) key and identity; complex types are compared and
tracked as values.

## Mapping

```c#
public class Customer
{
    public ObjectId Id { get; set; }
    public string Name { get; set; } = null!;
    public Address Home { get; set; } = null!;
    public GeoPoint Pin { get; set; }               // struct complex type
    public Address? Billing { get; set; }           // optional: EF10 only
    public List<Address> PastAddresses { get; set; } = []; // complex collection: EF10 only
}

public class Address
{
    public string City { get; set; } = null!;
    public string? Street { get; set; }
    public GeoPoint Location { get; set; }          // nested complex type
}

public struct GeoPoint
{
    public double Lat { get; set; }
    public double Lon { get; set; }
}

modelBuilder.Entity<Customer>(e =>
{
    e.ComplexProperty(c => c.Home, a => a.ComplexProperty(x => x.Location));
    e.ComplexProperty(c => c.Pin);
    e.ComplexProperty(c => c.Billing, a => a.ComplexProperty(x => x.Location)); // EF10
    e.ComplexCollection(c => c.PastAddresses, a => a.ComplexProperty(x => x.Location)); // EF10
});
```

- `[ComplexType]` on the CLR type maps it by convention wherever it is used.
- Nested complex properties must be configured explicitly (`a.ComplexProperty(x => x.Location)`) when the outer one is
  configured with `ComplexProperty`; they are not discovered by convention.
- Element names: the CLR name by default; `CamelCaseElementNameConvention` camel-cases complex properties and their
  members; `[BsonElement("...")]` and `[Column("...")]` on a complex property or a member override it, as does the
  `Mongo:ElementName` annotation (`HasPropertyAnnotation(MongoAnnotationNames.ElementName, "...")` on the complex property
  builder, `HasElementName("...")` on a member's property builder). `[BsonIgnore]` and `[BsonRequired]` are honored.
  `BsonRepresentation` and value converters on members work as they do for entity properties.
- The same CLR type may be used at several places with different element names; each place is configured independently.

### Per-version support

| Feature | EF8 | EF9 | EF10 |
|---|---|---|---|
| Class complex property | yes | yes | yes |
| Struct complex property | yes | yes | yes |
| Nested complex properties | yes | yes | yes |
| Optional (nullable) complex property | no (EF rejects it: call `IsRequired()`) | no (same) | yes |
| Complex collection (`ComplexCollection`) | no (API absent) | no (API absent) | yes |
| `ExecuteUpdate`/`ExecuteDelete` | no bulk operations on EF8 | yes | yes |

### Model validation

The model validator rejects, with an `InvalidOperationException` or `NotSupportedException` naming the member:

- two members of one document level mapped to the same element name, including a complex property and a scalar or owned
  navigation, and a collision produced by `CamelCaseElementNameConvention` (`Home` and a property `home`);
- an element name that starts with `$` or contains `.`;
- **encryption** (Queryable Encryption or client-side field level encryption) annotations on a complex property or on any
  member of a complex type: encryption is not supported inside complex types. Map the value as a property of an entity
  type or an owned entity type to encrypt it;
- a **concurrency token or row version** on a member of a complex type. Put the token on the entity: it guards the whole
  document, including its complex properties.

## Stored shape

```json
{
  "_id": ObjectId("..."),
  "Name": "Ann",
  "Home": { "City": "Oslo", "Street": null, "Location": { "Lat": 59.9, "Lon": 10.7 } },
  "Pin": { "Lat": 1.0, "Lon": 2.0 },
  "Billing": null,
  "PastAddresses": [ { "City": "Bergen", "Street": "Bryggen", "Location": { "Lat": 60.4, "Lon": 5.3 } } ]
}
```

- Members are read by element name: element order does not matter and unmapped elements are ignored.
- A null optional complex property is stored as BSON `null`. When reading, `null` and a missing element both read as
  `null`; `{}` reads as an instance whose members follow the rules below.
- A **required** complex property whose element is missing or BSON `null` throws when the entity (or the value) is
  read ("... is missing/null for required complex property ..."); it never reads as `null` or a default struct.
- A missing or `null` **required complex collection** reads as an empty collection. EF10 can store a `null` element in a
  complex collection; it reads back as `null`.
- Members inside a complex value follow the same rules as entity properties: a missing required member throws, a
  missing nullable member reads `null`.

Documents written by provider versions before complex type support do not contain the complex property's element
(those versions silently did not write it; see `BREAKING-CHANGES.md`). Backfill the element before reading them, or make
the property optional (EF10).

## Saving and change tracking

- Inserts write every complex property.
- When any member of a complex property changes (at any depth), the **whole top-level complex property** is rewritten
  with `$set`; unchanged complex properties are not written. Concurrent changes to different members of the **same**
  complex property from two contexts are therefore last-writer-wins for that property; changes to different complex
  properties, or to scalar properties, do not overwrite each other.
- A complex collection is rewritten as a whole when any element or member changes.
- EF rejects `null` for a required complex property (and, on EF10, a required complex collection) at `SaveChanges`.
- Replacing a complex value with an equal value is not a change.

## Queries

Complex-type queries run on the provider's native translator. With `MongoQueryMode.NativeOnly` a query that cannot be
translated natively throws instead of falling back to the driver's LINQ provider.

Supported natively:

- predicates, ordering and grouping keys over members at any depth (`c.Home.City == "Oslo"`, `OrderBy(c => c.Home.Location.Lat)`,
  `GroupBy(c => c.Home.City)`), aggregates over members (`Max(c => c.Pin.Lat)`), and set operations over member projections;
- projections of members and of whole complex values (`Select(c => c.Home)`, `new { c.Name, c.Home }`, nested values,
  complex collections), and materialization of entities holding complex properties, tracked or not;
- `== null` / `!= null` on an optional complex property (EF10), and equality between a complex value and `null`, an instance
  (constant, captured or constructed inline) or another stored complex value of the same CLR type. Equality is
  **member-wise** (it compares every mapped member, recursing into nested values) and follows C# null propagation;
- joins, `Include` and TPH queries over entities that have complex properties;
- complex collections (EF10): `Any`, `All`, `Count`, `Count(predicate)`, `LongCount`, ordering by a count, count projections,
  `Contains`/`!Contains` of a complex value (member-wise), nested and correlated element predicates. Inside an element
  predicate a `null` element reads every member as `null` (and a non-nullable `bool` member as `false`).

Not supported (each fails with a clear exception in every mode, never wrong rows):

- A whole complex value as the operand of `Distinct`, `Union`, `Concat`, `Intersect`, `Except`, `Contains`, `Cast`, `Join`,
  `Min`/`Max`/`Sum`/`Average` without a selector, or of `Select`/`Where`/`OrderBy` written after a paging operator over such
  a projection ("... cannot be the operand of '<Op>' ..."). Projections holding whole complex values allow `Take`, `Skip`,
  `First`/`Single`/`Last`(`OrDefault`) without a predicate, `Count`, `LongCount` and `Any`. Use member projections instead.
- `GroupBy` in which a whole complex value takes part (as the key, part of the key, or read off the grouped elements).
  Grouping by a member is supported.
- A constructor or record projection that takes a whole complex value as an argument.
- `Sum`/`Max`/`Min`/`Average` over the members of complex collection elements, and a count projected beside an element-list
  projection (`new { N = c.Lines.Count, Xs = c.Lines.Select(l => l.X) }`).

Translated by the driver-LINQ fallback (correct results; slower; refused under `NativeOnly`):

- element-member projections, indexers, `ElementAt`, `First(predicate).Member` and `Select(l => l.X).Contains(v)` over
  complex collections;
- members reached through a join scope or a reference navigation, and join keys through a complex property (an anonymous
  join key with a complex member fails with an exception);
- members declared only on a derived type, after `OfType`;
- on **EF8/EF9 only**, projections of complex members from an entity that also has an owned navigation. A whole complex value
  in such a projection fails with an exception on EF8/EF9 (EF10 translates all of these natively).

### Null elements in complex collections (EF10)

The driver-LINQ path evaluates predicates over a `null` element of a complex collection differently from C# (it treats
the element's members as missing, which sorts below every value and is not equal to `null`). When a query is not
translatable natively and its element predicate contains a comparison that depends on this (a relational comparison,
a comparison with `null`, a local-list `Contains` of a member, or an equality on a non-nullable `bool` member), the
default `Native` mode refuses the query with a `NativeTranslationNotSupportedException` instead of falling back to wrong
rows. The check is structural: it refuses such a query even if no element is null.

This check is best effort. It does not see element predicates reached through a navigation or join
(`s.Store.Tags.Any(t => t.Rank < 1)`), through an element-member `Select` (`Select(s => s.Verified).Contains(false)`),
null tests not written as a comparison with a `null` constant (`HasValue`, `== nullVariable`, `string.IsNullOrEmpty`), or an
element read inside a nested lambda whose result is compared. For those, the default mode returns the driver's rows.
Explicit `MongoQueryMode.DriverLinq` always runs the driver.

## Bulk operations (`ExecuteUpdate` / `ExecuteDelete`, EF9+)

Bulk operations run on the driver-LINQ path.

- Setters: a member of a complex property at any depth through required complex properties (`SetProperty(c => c.Home.City, "x")`),
  with a constant or a value computed from the row; a whole complex property or complex collection set to a client value
  (written as `SaveChanges` would write it).
- Refused setters: a member under an optional complex property (set the whole optional property instead); a whole complex
  value read from the row; `null` for a required complex property; a member of a complex collection element; a value
  computed from the row for a member stored with a `BsonRepresentation`; setting both a complex property and one of its
  members in the same operation.
- Filters over complex members are supported (relational comparisons over converted or represented members are refused,
  as for entity properties). Whole-value equality filters are refused.
- Filters over complex collection elements are restricted to forms the driver evaluates like a query: `==`/`!=` between a
  member and a non-null value, a non-nullable `bool` member and its negation, `list.Contains(member)` over a list that
  cannot hold `null`, and `&&`/`||`/`!` over those, under `Any`/`All`/`Count`/`LongCount`/`Where`/`Skip`/`Take` over a
  required collection typed `List<T>`/`IList<T>`. Anything else is refused before anything is written.
- Bulk operations bypass concurrency tokens, as for every entity.

> **Warning.** With `MongoQueryMode.DriverLinq` (set on the context or the query), the restriction above on complex
> collection filters is lifted: the operation runs the driver's evaluation, which treats the members of a `null` element as
> missing. Such an `ExecuteDelete` can delete rows whose `null` elements the equivalent default-mode query excludes
> (for example `Where(r => r.Stops.Any(s => s.Floor < 1))` deletes routes that contain a `null` stop). Check the rows with a
> query first, or delete by key.

## Known differences on the driver-LINQ path

These are pinned in the provider's tests; the native path answers as C# does.

- Projecting a complex-value equality (`new { Same = c.Home == other }`) always answers `false`.
- `c.Opt == null ? ... : ...` in a projection treats a missing optional complex value as present.
- `Any(i => i.Pos == null)`/`Count(i => i.Pos == null)` miss a missing complex value inside an owned collection.
- Predicates over `null` complex collection elements (see above).

## Design time

The compiled model (`dotnet ef dbcontext optimize`) supports complex properties: nested, struct, renamed, and (EF10)
complex collections. Element names are carried in the generated model. Migrations are not supported by the provider.
