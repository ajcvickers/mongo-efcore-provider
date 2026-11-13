# Failing specification tests — Jira inventory

EF Core ships a "specification tests" suite that providers consume from the
`Microsoft.EntityFrameworkCore.Specification.Tests` NuGet package and override
to assert the provider's actual behavior. When the MongoDB provider does not
yet support the functionality exercised by a test, the override is updated to
**assert the current failure** rather than be skipped — so the test stays green,
and any change in behavior (different exception type, different error message,
or the feature beginning to work) is detected immediately.

Each such override is annotated with a `// Fails: <description> <ticket-id>`
comment. This document enumerates every ticket referenced by such comments
together with a one-line description, plus a section listing failure modes
that currently lack a ticket. Counts are sourced from `tests/MongoDB.EntityFrameworkCore.SpecificationTests/**/*.cs`.

## Summary

Regenerate every figure below (and the `Count` column in the tables) with
`python tests/tools/count-spec-fails.py` (add `--by-file <ticket>` to see which files carry a ticket).

| | Before the spec port (`main` @ `cc5151f9`) | Now (rebuilt on `release`) |
| --- | --- | --- |
| `// Fails:` tag lines | 1,206 | 5,011 |
| Distinct tagged test methods, any EF version | 1,175 | 3,530 |
| ... active in the EF8 build | 1,064 | 2,646 |
| ... active in the EF9 build | 1,141 | 3,169 |
| ... active in the EF10 build | 882 | 3,313 |
| ... active in the EF11 build | not measured | 3,339 |
| Test bases still ignored by `MongoComplianceTest` (distinct entries) | 142 | 60 |

Full `SpecificationTests` run (Docker, `MONGODB_URI`/`ATLAS_URI` unset, one EF version at a time): EF8 18,159 passed / 73 skipped,
EF9 20,060 passed / 80 skipped, EF10 19,700 passed / 84 skipped, no failures; EF11 (xUnit v3) 19,840 passed / 87 skipped, no failures. These
counts are xunit test cases (theory rows included), unlike the method counts in this file.

The port was first written against `main` and then rebuilt on `release`. `release`'s native query pipeline already fixed a lot: about
700 version-cases that asserted a failure on `main` now pass, and the tag count dropped from 4,261 to 3,467 methods as a result. The other direction
matters more: a few tests that passed on `main` fail on `release`; see "Regressions on `release`" below.

The spec port (#35) took 85 test bases off the ignore list. The jump in tags is the point of the exercise: the newly
ported suites were never exercised before, so their failures were invisible. A tagged method asserts today's failure and
stays green; it is a measure of the provider's gaps, not of broken tests. Methods that pass as the upstream base are not
counted anywhere in this file. The bases still ignored are the complex type suites (separate work), the Associations suites,
the Translations bases for Math, String, the Operators and Miscellaneous groups, `TypeTestBase`, the bulk-update suites for
shapes the provider does not support; the reason
sits next to each entry in `MongoComplianceTest.cs`.

Ten largest tickets (distinct overridden methods, all EF versions):

| Ticket | Methods | Subject |
| --- | --- | --- |
| EF-X526 | 711 | Query shape (navigation/projection/set operation) not translated |
| EF-X101 | 8 | Entities added without an explicit key get Id 0 (no int/long key generation), so tracking/saving them ... |
| EF-X530 | 238 | Wrong results compared with the reference (client) evaluation |
| EF-216 | 245 | Navigations that cross collection boundaries cannot be translated; surfaces as Unsupported cross-DbSet... |
| EF-X102 | 8 | Relationship discovery between root entity types fails at model build (Unable to determine the relatio... |
| EF-X527 | 185 | Expression not supported by the driver LINQ translator |
| EF-X600 | 203 | Query translates natively but returns different data than the expected result  |
| EF-149 | 147 | GroupBy translation is severely limited; most non-trivial group-by shapes fail to translate. |
| EF-X001 | 137 | Sub-query selection across DbSets is not translated |
| EF-X316 | 121 | Collections of exotic element types / geometry types cannot be mapped (JsonTypes model) |

`EF-X526`, `EF-X527` and `EF-X530` are deliberately coarse buckets (the failure message was grouped by shape, not triaged
individually); expect them to split once someone looks at the individual failures.

> **Counting basis.** Every figure in the `Count` column is the number of distinct overridden test methods carrying a
> `// Fails: ... <ticket>` tag in `tests/MongoDB.EntityFrameworkCore.SpecificationTests/**/*.cs`, computed by
> `tests/tools/count-spec-fails.py`. A method split across `#if` arms counts once; a method that cites two tickets counts
> once for each; a tag belongs to the method whose body encloses it, or to the next method when it sits between members. A
> theory counts once however many data rows it has, so the numbers are methods, not xunit test cases (a ported
> `Generic`/`NonGeneric` pair of classes shares one set of methods and so counts once per class). Tickets cited only in
> prose, such as `post-EF-117`, are not counted. Earlier figures in this file used an unknown mixture of rules and have all
> been replaced. A count of 0 means no test is tagged: either the bug is fixed (the row is kept for history), or the
> ticket describes a workaround that the ported fixtures apply in their model (`EF-X201`, `X207`, `X211`, `X214`, `X215`,
> `X304`) or a base that could not be ported.

> If you fix one of these bugs, search for the ticket id in the spec-tests
> project — the corresponding overrides need to be updated (drop the
> `AssertTranslationFailed` wrapper or the throws-asserting `Assert.Contains`,
> assert the new MQL baseline, and remove the `// Fails:` line).

---

**Query mode.** CI also runs every suite with the driver fallback disabled (`MONGODB_EF_QUERY_MODE=NativeOnly`, the `native-only` job). The suites
ported in #35 were baselined in the default query mode only, so their stores are pinned to it (`MongoTestStoreFactory(server, pinDefaultQueryMode: true)`;
`MongoPinnedBasicTypesQueryFixture` for the Translations suites). Under NativeOnly the same tests would fail with
`NativeTranslationNotSupportedException` and different messages. Baselining them in NativeOnly too is a follow-up; until then these suites give no
native-only coverage.

**Test pollution warning.** The databases of the ported fixtures are shared by every test in a class and there is no transaction to roll back, unlike a
relational provider. A test whose `SaveChanges` succeeds where the relational provider throws (no foreign keys, no unique constraint) leaves rows behind and
changes the results of later tests, in an order dependent way. EF-X700 was this. Before treating a "wrong results" ticket as a provider bug, run the test on its own.

## Regressions on `release` (passed on `main`, fail on `release`)

The spec port was first written against `main` (the upstream mirror). Re-baselining it against `release` (the fork's trunk, which has
the native query pipeline) turned up tests that passed on `main` as a plain `base` call and now fail. They are asserted as today's
failure, tagged `// Fails: REGRESSION vs main: ...`, so any change is noticed. Wrong data and broken common scenarios matter most.

| Ticket | Test(s) | What changed | EF versions |
| --- | --- | --- | --- |
| EF-X703 | **Fixed.** `CompositeKeyEndToEnd.Can_use_two_non_generated_integers_as_composite_key_end_to_end` could not query back a saved row. The discriminator is part of the composite key, so it is stored in `_id._t`, but the discriminator filter of a derived-type query targeted a top-level `_t` (`MongoDiscriminatorPredicate`). The test now calls `base`; regression test `CompositeKeyDiscriminatorTests` | 0 |
| EF-X702 | **Fixed.** `DateTimeOffsetTranslations.TimeOfDay` lost the sub-millisecond ticks (and rounded a pre-1970 value up). The native leaf now computes the time of day from the stored `Ticks` (`MongoDatePart.TimeOfDayTicks`) and reads it back as ticks; regression test `DateTimeOffsetTimeOfDayTests`. The explicit `DriverLinq` bridge still truncates to milliseconds | 0 |
| EF-X701 | **Fixed.** `AdHocQueryFilters.Query_filter_with_context_accessor_with_constant` threw `NullReferenceException` while building the pipeline for an unset list member of the context (`Foo == 1 ? Baz.Contains(e.Bar) : ...`), even though the list's branch was not taken. `MongoPipelineFactory` now serializes an unset list parameter as an empty list, as EF Core treats a null collection (`Contains` matches nothing). This also fixes EF-X503 (`PrimitiveCollections.Parameter_collection_null_Contains`) and EF-X524 (`QueryFilterFuncletization.DbContext_list_is_parameterized`), which asserted the same `NullReferenceException`; regression test `QueryFilterContextMemberTests` | 0 |
| EF-X704 | **Deliberate, not a bug.** `GuidTranslations.NewGuid`: `Guid.NewGuid()` in a query is refused ("cannot be evaluated once per row on the server"), as for the other non-deterministic calls (EF-255). The test asserts the refusal without a `// Fails:` tag. No `$rand`-based translation is planned | 0 |
| EF-X705 | `GearsOfWar`: `Group_by_with_aggregate_max_on_entity_type`, `Nav_rewrite_Distinct_with_convert`, `_anonymous`; `ComplexNavigationsSharedType`: one GroupJoin test | **Not a regression.** Only the exception type of an unsupported query changed (`FormatException` / `ArgumentException` instead of `InvalidOperationException`). `AGENTS.md` does not count this as a break, so the tests pin the type each one throws (`FormatException` for `Group_by_with_aggregate_max_on_entity_type`, `ArgumentException` for the rest) | 8, 9, 10 |

Changes that are *not* regressions: baseline-only MQL differences where the results are still correct (`$count` field name; the
`Today`/`Now`/`UtcNow` tests no longer assert MQL because it embeds the current date), and tests that failed on `main` and now fail differently
or pass. About 700 version-cases moved from "asserts a failure" to passing, including the `Find` discriminator bug (EF-X200).

## MongoDB EF Core provider tickets — `EF-NNN`

| Ticket | Comment subject | Description | Count |
| --- | --- | --- | --- |
| [EF-117](https://jira.mongodb.org/browse/EF-117) | _(no remaining `// Fails:` tags)_ | Cross-collection **Include**/`ThenInclude` is now implemented for the tested shapes. The five tests formerly tagged here were re-investigated: `Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join` (tracking + no-tracking) now **passes**; `Collection_include_over_result_of_single_non_scalar` and `Do_not_erase_projection_mapping_when_adding_single_projection` actually fail on cross-`DbSet` subquery translation (re-tagged **EF-X001**); `Included_one_to_many_query_with_client_eval` fails on driver client-evaluation (re-tagged **EF-X003**); and Include on a keyless entity (incl. multi-level) is a genuine PK-less `$lookup` gap (re-tagged **EF-X019**). EF-117 no longer has any active `// Fails:` tags. (Join/GroupJoin/SelectMany/RightJoin/subquery failures formerly tagged here were re-categorized — see EF-X001/EF-216/EF-220/EF-X016/EF-X017/EF-X018.) | 0 |
| [EF-149](https://jira.mongodb.org/browse/EF-149) | `GroupBy issue EF-149` | `GroupBy` translation is severely limited; most non-trivial group-by shapes fail to translate. | 147 |
| [EF-153](https://jira.mongodb.org/browse/EF-153) | `TagWith EF-153` | `TagWith(...)` content is silently dropped — does not appear in the emitted MQL. | 8 |
| [EF-164](https://jira.mongodb.org/browse/EF-164) | `Missing property values issue EF-164` / `Projections issue EF-164` | BSON documents that omit a required scalar (or required navigation) throw on materialization — `Project_root_with_missing_scalars`, `Project_root_entity_with_missing_required_navigation`, etc. | 33 |
| [EF-202](https://jira.mongodb.org/browse/EF-202) | `Entity equality issue EF-202` | Comparing two entities (`entity1 == entity2` / `Contains(entity)`) is not lowered to a key-equality comparison. | 3 |
| [EF-216](https://jira.mongodb.org/browse/EF-216) | `Cross-document navigation access issue EF-216` / `Navigations issue EF-216` | Navigations that cross collection boundaries cannot be translated; surfaces as `Unsupported cross-DbSet query between ...`. Documented at the helper `AssertNoMultiCollectionQuerySupport`. | 207 |
| [EF-217](https://jira.mongodb.org/browse/EF-217) | `Call ToString on DateTimeOffset EF-217` | `DateTimeOffset.ToString()` cannot be translated. | 1 |
| [EF-220](https://jira.mongodb.org/browse/EF-220) | `Multiple query roots issue EF-220` | Queries that reference more than one `DbSet<>` (Cartesian product / cross-join) are not translatable. Includes `SelectMany` across DbSets and tautology-predicate cross-joins. | 64 |
| [EF-221](https://jira.mongodb.org/browse/EF-221) | _(no remaining `// Fails:` tags)_ | `Equals` calls with genuinely mismatched, non-convertible simple types (e.g. `int?.Equals((ulong)x)`) are now folded to the correct constant result in `MongoEFToLinqTranslatingExpressionVisitor` instead of reaching the driver's `Equals` translator, which threw `InvalidCastException`. The four tests formerly tagged here now pass. | 0 |
| [EF-222](https://jira.mongodb.org/browse/EF-222) | `translation of Like issue EF-222` | `EF.Functions.Like(...)` is not translated. | 5 |
| [EF-227](https://jira.mongodb.org/browse/EF-227) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: `Min` / `Max` over an empty nullable sequence does not produce the EF-expected `null`. | 0 |
| [EF-228](https://jira.mongodb.org/browse/EF-228) | `Truncation data loss issue EF-228` | `Sum`/`Average` over `float` columns suffers precision/truncation loss when accumulated server-side. | 1 |
| [EF-232](https://jira.mongodb.org/browse/EF-232) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: `Sum_with_no_data_cast_to_nullable` does not produce the EF-expected `null`. (The `Compiled_query_when_does_not_end_in_query_operator` failure that previously also cited EF-232 has been re-tagged as `EF-X011`.) | 0 |
| [EF-234](https://jira.mongodb.org/browse/EF-234) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: `EF.Functions.Random()` is not translated. | 0 |
| [EF-235](https://jira.mongodb.org/browse/EF-235) | `Translate Convert methods issue EF-235` | `Convert.ToBoolean/Byte/Int*/Decimal/Double/String/...` calls are not translated. | 8 |
| [EF-237](https://jira.mongodb.org/browse/EF-237) | `MathF mapping issue EF-237` | `MathF.*` overloads (the `float` Math API) are not translated. | 5 |
| [EF-238](https://jira.mongodb.org/browse/EF-238) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: `Math.Min` / `Math.Max` are not translated. | 0 |
| [EF-239](https://jira.mongodb.org/browse/EF-239) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: `Math.Sign` is not translated. | 0 |
| [EF-240](https://jira.mongodb.org/browse/EF-240) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: `Double.RadiansToDegrees` / `Double.DegreesToRadians` are not translated. | 0 |
| [EF-241](https://jira.mongodb.org/browse/EF-241) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: `String.TrimStart(...)` / `String.TrimEnd(...)` with a `char[]` argument are not translated. | 0 |
| [EF-242](https://jira.mongodb.org/browse/EF-242) | `DateOnly support issue EF-242` | `DateOnly.FromDateTime(...)` (and related `DateOnly` conversions) are not translated. | 1 |
| [EF-243](https://jira.mongodb.org/browse/EF-243) | `StartsWith/Contains/EndsWith Ordinal/OrdinalIgnoreCase issue EF-243` | `string.StartsWith/Contains/EndsWith` with `StringComparison.Ordinal` or `OrdinalIgnoreCase` is not translated. | 3 |
| [EF-245](https://jira.mongodb.org/browse/EF-245) | `String.Join issue EF-245` | `String.Join(separator, source.Select(...))` is not translated. | 4 |
| [EF-246](https://jira.mongodb.org/browse/EF-246) | `DateTime subtraction issue EF-246` | `(dateA - dateB).TotalDays / TotalHours / TotalSeconds / TotalMilliseconds` is not translated. | 1 |
| [EF-247](https://jira.mongodb.org/browse/EF-247) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: `Regex.IsMatch` with a non-constant pattern is not translated. | 0 |
| [EF-248](https://jira.mongodb.org/browse/EF-248) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: `String.FirstOrDefault()` / `LastOrDefault()` (LINQ-on-string) is not translated. | 0 |
| [EF-249](https://jira.mongodb.org/browse/EF-249) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: `checked { ... }` arithmetic is not honored — the `Checked_context_with_arithmetic_does_not_fail` test sees a different exception than EF expects. | 0 |
| [EF-252](https://jira.mongodb.org/browse/EF-252) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: `Throws_on_concurrent_query_first/list` — the concurrency detector does not fire as the EF base test expects. | 0 |
| [EF-253](https://jira.mongodb.org/browse/EF-253) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: `OrderBy(x).ThenBy(x)` on the same column with different directions does not emit the expected MQL. | 0 |
| [EF-254](https://jira.mongodb.org/browse/EF-254) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: `.Skip(0).Take(0)` with a parameter does not produce the expected empty result. | 0 |
| [EF-371](https://jira.mongodb.org/browse/EF-371) | _(no remaining `// Fails:` tags)_ | **Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: A self-referencing two-hop reference navigation (`e.Manager.Manager`) collapses to a single join and hop 2 degrades to an inner `$unwind`, so the query returns 0 rows instead of 6. Baselined green on EF10 by asserting the wrong-data failure; the EF8/EF9 arm is a translation failure tagged EF-X020. | 0 |

## MongoDB C# Driver tickets — `CSHARP-NNNN`

| Ticket | Comment subject | Description | Count |
| --- | --- | --- | --- |
| [CSHARP-5296](https://jira.mongodb.org/browse/CSHARP-5296) | `DateTimeOffset issue CSHARP-5296` | Driver-level: component access (`.Year`, `.Hour`, etc.) on the static `DateTimeOffset.Now` / `UtcNow` is not translated by the LINQ provider. Members of a stored `DateTimeOffset` property are handled by the provider itself (EF-218). | 2 |
| [CSHARP-5836](https://jira.mongodb.org/browse/CSHARP-5836) | `Reverse not supported CSHARP-5836` | Driver-level: `Queryable.Reverse()` is not implemented in the driver's LINQ provider. | 10 |

## Upstream EF Core tickets — `dotnet/efcore#NNNNN`

| Ticket | Comment subject | Description | Count |
| --- | --- | --- | --- |
| [dotnet/efcore#36412](https://github.com/dotnet/efcore/issues/36412) | `EF upstream issue--see https://github.com/dotnet/efcore/issues/36412` | Upstream EF Core test bug — provider-side override compensates while the upstream is fixed. | 1 |

Deliberate refusals referenced in test comments but not `// Fails:`-tagged: **EF-255** — functions with no per-row server equivalent (`EF.Functions.Random()`, `Guid.NewGuid()`, `Random.Next`) are refused rather than folded into one constant. This supersedes [EF-234](https://jira.mongodb.org/browse/EF-234), whose tag is gone. Also referenced: EF-375 (two joins onto one target flatten to one `$lookup` each), EF-417 (retire the driver-LINQ oracle in test infrastructure), EF-457, EF-461 (nullable date-part group keys decline native).

Two further GitHub references appear in the codebase but are not `// Fails:`-tagged (they are TODOs / notes, not failure annotations): [dotnet/efcore#36488](https://github.com/dotnet/efcore/issues/36488) (`NorthwindSetOperationsQueryMongoTest.cs:149`, upstream test has a bug), [dotnet/efcore#36521](https://github.com/dotnet/efcore/issues/36521) (`MongoApiConsistencyTest.cs:91`), [dotnet/efcore#36413](https://github.com/dotnet/efcore/issues/36413) (`Utilities/TestMqlLoggerFactory.cs:6`).

---

## Failure modes lacking a Jira ticket — proposed new tickets

These entries appear in `// Fails:` comments without an `EF-` or `CSHARP-` reference, or in test bodies as un-commented failure assertions. Each entry is assigned a **temporary** ticket id of the form `EF-X###` to be replaced with a real Jira number once filed; the `X` makes it obvious in `grep` results that the id is a placeholder.

| Temp ticket | Subject | Count |
| --- | --- | --- |
| EF-X001 | Sub-query selection across DbSets is not translated | 137 |
| EF-X002 | Provider throws a different exception than the EF translation-failure message | 29 |
| EF-X003 | Driver-level feature gaps surfaced as test failures | 9 |
| EF-X004 | ~~Float `Sum`/`Average` truncation (likely duplicate of EF-228)~~ — resolved, no remaining tags | 0 |
| EF-X005 | BSON document missing nested required reference (AdHoc JSON) | 2 |
| EF-X006 | MongoDB `DateTimeKind` round-trip handling | 3 |
| EF-X007 | Views / `HasDefiningQuery` semantics for MongoDB collections | 2 |
| EF-X008 | No support for nested JSON in AdHoc JSON tests | 3 |
| EF-X009 | ~~Single uncategorized failure — needs triage~~ — resolved, no remaining tags | 0 |
| EF-X010 | Provider-specific Include error message differs from EF baseline | 4 |
| EF-X011 | ~~Compiled query with non-query operator — wrong exception~~ — fixed by EF-233 | 0 |
| EF-X012 | ~~`OfType` translation unsupported~~ — resolved, no remaining tags | 0 |
| EF-X013 | MongoDB has no `$xor` operator (`Where_bitwise_xor`) | 1 |
| EF-X014 | Server-side projection conflict with cast-to-nullable | 1 |
| EF-X015 | Sub-second `DateTime` component translation (nanosecond/microsecond) | 1 |
| EF-X023 | GroupJoin shapes not translated | 6 |
| EF-X017 | Join shapes not translated | 7 |
| EF-X018 | RightJoin not supported | 5 |
| EF-X019 | Include on keyless entity not supported (no primary key for $lookup join) | 2 |
| EF-X020 | ~~Cross-collection Include/join/navigation not translated on EF8/EF9 (works on EF10)~~ — resolved, no remaining tags | 2 |
| EF-X021 | Filtered Include / query filter on cross-collection target not translated | 0 |
| EF-X022 | Join/GroupJoin whose inner source is a filtered or ordered sub-query is rejected | 14 |
| EF-X024 | `Where`/entity materialization over a flattened multi-join chain onto ambiguous same-navigation or navigation-less siblings is not translated | 2 |
| EF-X016 | Bulk `ExecuteUpdate`/`ExecuteDelete` source restricted to a single collection scoped by `Where` | 61 |
| EF-X400 | `DbUpdateConcurrencyException` is thrown without invoking `ISaveChangesInterceptor.ThrowingConcurrencyException` | 2 |
| EF-X401 | Ambiguous foreign key discovery between a join/owned type and its owner (`Both relationships ... could use {'PostId'}`) | 4 |
| EF-X402 | Projecting an owned collection navigation directly (`Select(e => e.OwnedCollection)`) is not translated | 1 |
| EF-X403 | Entity types reachable only through a navigation are made owned, so ModelBuilder relationship tests see a different model (NRE in the base assertions) | 82 |
| EF-X404 | Collection-typed properties (`List<T>`, `T[]`, `ObservableCollection<T>`) in ModelBuilder tests fail model validation as unmapped | 34 |
| EF-X405 | Primary key must be mapped to `_id`: composite, shadow and field-backed keys are rejected in model validation | 18 |
| EF-X406 | `HasData` on owned types fails seed validation (required key property has no value) | 1 |
| EF-X407 | Owned type used from multiple navigations with different element names is rejected | 6 |
| EF-X408 | EF convention/validation exceptions the base tests expect are not thrown | 4 |
| EF-X409 | Many-to-many navigations (`[ForeignKey]` attribute shapes) cannot be determined | 2 |
| EF-X410 | Multiple row-version (concurrency token) properties on one entity are rejected | 1 |
| EF-X411 | Model metadata differs from the relational baseline (owned-type keys/names, `ValueGenerated`) | 12 |
| EF-X412 | `CompiledModelTestBase`: properties of types with no Mongo mapping (`IPAddress`, ...) are discovered as owned entity types and fail to build | 3 |
| EF-X300 | Value converter built from inline conversion expressions on a nullable property is not supported | 1 |
| EF-X301 | Query comparing a converted property to a parameter/literal fails casting `ValueConverterSerializer` | 0 |
| EF-X302 | `ValueConverterSerializer` / enum-conversion serializers lack `IHasRepresentationSerializer` | 1 |
| EF-X303 | Query over converted property fails binding `ValueConverterSerializer` | 3 |
| EF-X304 | No built-in integer key value generation (spec fixtures use a test-only generator) | 0 |
| EF-X305 | Query comparing a struct-typed (value-converted) key does not translate | 0 |
| EF-X306 | Inserting entities with value-converted keys raises a duplicate `_id` error | 35 |
| EF-X307 | Model validation requires every key property to be mapped to `_id` | 6 |
| EF-X308 | Relationship/navigation discovery differs (navigations become owned by convention) | 26 |
| EF-X309 | `[Required]`/`[MaxLength]`/`[StringLength]` are not enforced by the store | 4 |
| EF-X310 | `Single`/`First` over an empty result in the test body (not triaged) | 2 |
| EF-X311 | Music store query fails with `InvalidOperationException` (not triaged) | 1 |
| EF-X312 | `[Column(TypeName)]` is reported as an error by default | 5 |
| EF-X314 | Driver LINQ translation rejects expression over converted property, indexer or subquery | 6 |
| EF-X315 | Value-converter behavior differs from the provider-agnostic expectation (not triaged) | 5 |
| EF-X316 | Collections of exotic element types / geometry types cannot be mapped (JsonTypes model) | 121 |
| EF-X317 | `JsonTypesTestBase` round-trips values through the type mapping's `JsonValueReaderWriter`, which Mongo type mappings do not have (it is `null`), so the base test throws `NullReferenceException`. Confirmed by decompiling the base and running `Can_read_write_DateOnly_JSON_values`; the other wrappers share that code path | 60 |
| EF-X318 | Bad JSON value does not throw on materialization | 0 |
| EF-X319 | Concurrency token on an owned entity is rejected (Serialization). The fixture drops the owned token (`FixExternalModel`) and seeds the F1 data itself, and `SerializationMongoTest` passes as the base | 0 |
| EF-X320 | NetTopologySuite spatial types are not supported: the provider would own them. `SpatialQueryMongoFixture` stores them as well-known binary through a value converter so the data seeds and loads, but every spatial operation (area, distance, union, ...) is then rejected as not translatable | 88 |
| EF-X321 | `DateOnly`/`TimeOnly`/`TimeSpan`/`DateTimeOffset` members and methods are not translated by the driver | 54 |
| EF-X322 | `byte[]` is not supported as an array in LINQ | 4 |
| EF-X323 | Translated `byte[]` pipeline is rejected by the server | 2 |
| EF-X324 | `byte[].Length` query returns no rows | 1 |
| EF-X200 | `Find`/derived-type `Set<T>` over a type hierarchy does not filter by discriminator (a base-type document is materialized as the derived type) | 0 |
| EF-X201 | Single-property primary key set by explicit `HasKey`/constructor binding is not mapped to element `_id` (model validation fails); ported fixtures work around it with a test convention | 0 |
| EF-X202 | ~~Explicit transaction cannot be shared by a second context (session disposed with the first context)~~ — resolved by `UseTransaction`, no remaining tags | 0 |
| EF-X203 | Property hiding (`new Id` on a derived type) conflicts in the BSON class map | 9 |
| EF-X205 | `int` keys are not store-generated (upstream models expect identity-style keys); ported fixtures work around it with a counter generator | 1 |
| EF-X206 | Lazy-load/`Single` query over a type hierarchy returns more than one element | 0 |
| EF-X207 | Unique index over a nullable one-to-one foreign key collides on `null` in MongoDB; ported fixtures drop the uniqueness | 1 |
| EF-X208 | `Include` of a many-to-many skip navigation is not supported | 87 |
| EF-X209 | Many-to-many skip navigation `Load`/`Query` (`SelectMany` + `NotQuiteInclude`) not translated | 44 |
| EF-X211 | Owned-collection ordinal keys are not generated for some owned shapes (duplicate `{Id: 0}` on `Add`); ported fixtures work around it with a counter generator | 0 |
| EF-X212 | **Mostly withdrawn.** Reading a document with nested owned types reported "Document element is missing for required non-nullable property" in 59 of 61 lazy-load tests. That was reseed pollution, not a provider bug: before release made `MongoTestStore.Clean` drop the database, `Fixture.Reseed()` stacked a second copy of the seed data on the previous test's rows. After release's isolation change the 59 no longer fail; 2 tests still carry the tag | 3 |
| EF-X213 | A type reached only through a navigation is auto-owned, so `Set<T>()` for it is rejected | 14 |
| EF-X214 | Concurrency token on an owned type is rejected by model validation (F1 test model); ported fixture drops them | 0 |
| EF-X215 | `UseSeeding`/`UseAsyncSeeding` delegates are not run by `EnsureCreated`; ported fixture seeds explicitly | 0 |
| EF-X216 | Materialization interception creates the base proxy type for derived-type documents (`DriverProxy` instead of `TestDriver`) | 1 |
| EF-X217 | Keyless query type maps to its own (empty) collection; relational tests map it to a view/SQL query over another table | 1 |
| EF-X101 | Entities added without an explicit key get `Id` 0 (no `int`/`long` key generation), so tracking/saving them collides on the same key | 8 |
| EF-X102 | Relationship discovery between root entity types fails at model build (`Unable to determine the relationship represented by navigation`) | 8 |
| EF-X103 | No value generator for `int`/`long`/enum/value-converted keys (`NotSupportedException` on `Add`) | 31 |
| EF-X104 | Dependent key is null because the principal key (`string`/`Uri`/`Guid` wrapper) was not generated | 4 |
| EF-X105 | Bulk write fails with a duplicate `_id` when saving entities that rely on default `int` keys | 25 |
| EF-X106 | Unique index build fails on null/missing values (`createIndexes` duplicate `null`). `NullKeysMongoFixture` relaxes the indexes (`RelaxNullableUniqueIndexes`, `RelaxUniqueForeignKeyIndexes`), so the `NullKeys` tests run | 0 |
| EF-X107 | Entity types used by the test are missing from the model (`entity type '...' was not found`) | 8 |
| EF-X108 | Store-generated/computed value and update semantics differ (no database defaults, computed columns or generated keys) | 6 |
| EF-X110 | `Include` of a skip (many-to-many) navigation is not supported | 27 |
| EF-X111 | Concurrency token on an owned-entity property is rejected at model validation | 12 |
| EF-X112 | Many-to-many join entity is modelled as an owned type and cannot be saved or queried on its own | 4 |
| EF-X113 | Indexer (shared-type entity) property access `get_Item(...)` is not translated by the driver | 2 |
| EF-X114 | Partial update of a tracked entity reports a `DbUpdateConcurrencyException` | 0 |
| EF-X115 | Concurrency exception message differs from the relational wording asserted by `UpdatesTestBase` | 4 |
| EF-X116 | A conflicting write between two contexts surfaces as a raw `MongoCommandException` (server write conflict) instead of `DbUpdateConcurrencyException` | 15 |
| EF-X117 | Inserting a duplicate key surfaces as a raw `MongoBulkWriteException` instead of `DbUpdateException` | 1 |
| EF-X118 | **Skipped, not failing.** The upstream test's model (the `Cruiser` / `CruiserWithSentinel` entity types with a default-value foreign key) is registered only by the relational fixtures, so it does not exist in the Mongo `GraphUpdates` fixture and the test cannot run. `Can_insert_when_FK_has_default_value`, `Can_insert_when_FK_has_sentinel_value`, `Can_insert_when_composite_FK_has_default_value_for_one_part`. Whether the provider handles a foreign key left at its CLR default is therefore untested | 0 |
| EF-X500 | Primitive-collection projection/SelectMany/Union/Concat/ordering not translated | 10 |
| EF-X501 | Inline List<T> collection (Min/Max/Contains/index) not translated or serialized | 6 |
| EF-X502 | Single/SingleOrDefault over a primitive collection not translated | 2 |
| EF-X504 | Contains over collection of structs with value converter not translated or serializer mismatch | 2 |
| EF-X505 | Wrong result: collection index compared with nullable column | 1 |
| EF-X506 | Server error: input to $map must be an array (enum collection cast) | 1 |
| EF-X507 | EF8 only: inline empty-collection Count query returns no rows | 1 |
| EF-X508 | Microsecond DateTime values are truncated to milliseconds so distinct elements collapse/duplicate | 1 |
| EF-X509 | Query over a primitive collection with a custom converter returns no rows instead of failing as the base expects | 1 |
| EF-X510 | byte[] element access / element collection: ByteArraySerializer is not an IBsonArraySerializer | 5 |
| EF-X511 | EF8 only: array of arrays does not throw the expected InvalidOperationException | 1 |
| EF-X512 | Materializing an owned collection from a BsonDocument throws ArgumentNullException (derivedType) | 21 |
| EF-X513 | Owned-collection operators (SelectMany/GroupBy/equality/...) not translated | 20 |
| EF-X514 | Owned indexer properties / owned-collection element expressions not supported by driver LINQ | 16 |
| EF-X515 | Wrong results for owned-collection operators (ElementAt/Union/...) | 5 |
| EF-X516 | Server aggregate error for owned-collection element access | 2 |
| EF-X517 | Projecting an owned entity without its owner in a tracking query | 6 |
| EF-X518 | Provider throws a different exception type than the base test expects | 3 |
| EF-X519 | Hierarchy materialization fails: discriminator value does not match the requested type | 1 |
| EF-X520 | ElementAt/indexer call is rebuilt with the wrong number of arguments | 4 |
| EF-X521 | Left join on entity with owned navigations throws ArgumentNullException (key) | 1 |
| EF-X522 | Generated int key not assigned (no value generation), so tracking conflicts | 45 |
| EF-X523 | Model validation rejects the relationship (owned/nested owned or ambiguous FK) | 2 |
| EF-X525 | EF8/EF9: filtered query with multiple filtered entities returns duplicate rows | 1 |
| EF-X526 | Query shape (navigation/projection/set operation) not translated | 711 |
| EF-X527 | Expression not supported by the driver LINQ translator | 185 |
| EF-X528 | Server error executing the generated pipeline | 6 |
| EF-X529 | NullReferenceException during query execution/materialization | 10 |
| EF-X530 | Wrong results compared with the reference (client) evaluation | 238 |
| EF-X531 | ArgumentNullException (null string/source argument) during translation or materialization | 10 |
| EF-X532 | GroupBy over all columns builds an invalid materialization expression (QueryingEnumerable not assignable) | 9 |
| EF-X533 | Manual left join / transparent identifier shape throws ArgumentException (LeftJoinResult / _inner / _outer) | 38 |
| EF-X534 | Join key selector that is a sub-query: serializer cannot be created for MongoQuery | 7 |
| EF-X535 | Skip/Take/Distinct between two cross-collection joins (Include/ThenInclude) not supported | 13 |
| EF-X537 | Single/First over query results finds the wrong number of rows (duplicate or missing rows) | 0 |
| EF-X539 | Many-to-many join entity types (`JoinOneToTwo` and the like) have no `DbSet` and are reached through navigations, so the provider owns them by convention and `Set<JoinOneToTwo>()` throws. `ManyToManyQueryMongoFixture` and `ManyToManyNoTrackingQueryMongoFixture` replace the convention (`MongoSpecModelHelpers.DoNotOwnTypes`) so the join types are independent entity types, and relax the unique indexes (EF-X207). The bases are now ported on EF8 to EF11 | 1 |
| EF-X540 | Result mismatch whose assertion failure surfaces as TypeLoadException (xunit.assert version skew), hiding the wrong-result cause | 3 |
| EF-X541 | Average/aggregate over a navigation without selector throws 'Operation is not valid due to the current state of the object' | 3 |
| EF-X542 | Enum flags bitwise/closure typed as a different type: ConvertIntegralTypeToEnumSerializer lacks IHasRepresentationSerializer | 2 |
| EF-X543 | Property converted to nullable/other type in a projection: no coercion operator defined (int to string) | 1 |
| EF-X544 | Model validation: an owned entity type used in multiple navigations is rejected | 4 |
| EF-X545 | Model validation: cannot determine the relationship represented by a navigation | 4 |
| EF-X546 | Filter on a nested DTO via an interface: serializer value type mismatch | 1 |
| EF-X547 | Single-property primary key with a name other than Id must be mapped to '_id' (test model does not) | 2 |
| EF-X538 | A type that derives from an entity type, and is first reached through a navigation before it is in the model (the base type's navigation to its derived type, with no `DbSet` for the derived type), is owned by convention. That breaks the explicit one-to-one between the two ("the dependent side could not be determined"). `InheritanceRelationshipsQueryMongoFixture` replaces the convention so such a type is not owned; the default is unchanged, because making it the provider default also changes the model of `GraphUpdates` | 0 |
| EF-X548 | `JsonQueryTestBase<>`: saving a `uint[]`, `List<uint>` or `ulong[]` property holding a value above `Int32.MaxValue` / `Int64.MaxValue` throws `OverflowException` (`UInt32Serializer` goes through `RepresentationConverter.ToInt32`). The fixture clamps those seed values so the rest of the suite runs; the tests that compare them with the expected data (the `TestUnsignedInt32Collection` family) still fail with a value mismatch | 34 |
| EF-X549 | A JSON-owned collection that is indexed (`ElementAt`/indexer), projected or composed in a subquery is not translated (`The LINQ expression 'EF.Property<List<JsonOwned...>>(...)' could not be translated`) | 81 |
| EF-X600 | Query translates natively but returns different data than the expected result (found when re-baselining the port against `release`; not yet triaged) | 203 |
| EF-X601 | The server rejects the generated pipeline (`MongoCommandException`) | 5 |
| EF-X602 | Serialization/format error (`FormatException`) while translating or materializing | 38 |
| EF-X603 | `ArgumentException` / `ArgumentNullException` while translating or materializing | 67 |
| EF-X604 | `InvalidOperationException` from query execution or materialization | 83 |
| EF-X605 | Other unexpected exception from query execution | 119 |
| EF-X606 | One `SaveChanges` removes a document and adds another with the same value in a unique-indexed property (`GraphUpdates.Save_required_many_to_one_dependents_with_alternate_key`, `useExistingEntities: false`: a `RequiredAk1` is removed and a new one takes its `AlternateId`). The insert is rejected with a duplicate key error (`E11000`) because the old document is still there when it reaches the server. Relational providers order the delete first. Surfaced by #77: the old reseed dropped the database and with it the model's unique indexes, so the test got further and failed later with a `NullReferenceException` (EF-X605, which the `useExistingEntities: true` rows still show) | 1 |
| EF-X700 | **Withdrawn.** Was reported as a regression (`InheritanceQuery` base-type queries returning an extra row). It was test pollution in the port: `Setting_foreign_key_to_a_different_type_throws` expects a foreign key violation, MongoDB has none, so its `SaveChanges` persisted a Kiwi that later count-based tests saw, depending on the run order. The class now drops and re-seeds its database after the two tests that write (`ResetDatabaseAsync`); the three tests pass | 0 |
| EF-X701 | **Fixed:** an unset list parameter (a context member used by a query filter, or a null captured list) is serialized as an empty list instead of throwing `NullReferenceException` | 0 |
| EF-X702 | **Fixed:** `DateTimeOffset.TimeOfDay` is computed from the stored `Ticks`, so sub-millisecond ticks are kept | 0 |
| EF-X703 | **Fixed:** a derived-type query filtered on a top-level discriminator even when the discriminator is part of the composite key (`_id._t`) | 0 |
| EF-X704 | **Deliberate (EF-255):** `Guid.NewGuid()` in a query is refused (cannot be evaluated once per row); the test asserts the refusal | 0 |
| EF-X705 | Exception type only, not a regression: an unsupported query throws `FormatException` / `ArgumentException` where the upstream test expects `InvalidOperationException` (not contract, see `AGENTS.md`) | 5 |
| EF-X710 | EF11 only: the generated compiled model throws `InvalidCastException` from a static constructor on a background thread and crashes the test host; `CompiledModelTestBase` is excluded on EF11 | 0 |
| EF-X711 | EF11 only: xUnit v3 rejects a class that hides an inherited test with a `new` method ("has overloads"), so the hiding methods used on EF8 to EF10 for non-virtual upstream tests are not compiled on EF11 and the inherited tests that fail as base are skipped at run time (`Ef11TestSkips`); the reason names the ticket | 0 |
| EF-X720 | Time zone dependent: a `DateTime` of kind Unspecified is treated as local time (EF-X006), so these `ComplexNavigations*` (and some Translations) tests return values shifted by an hour wherever the machine zone has daylight saving time on the data dates (Europe/London), and pass in UTC, as on the CI runners. `SpecFails.Either` requires a pass in a UTC environment and accepts the known failure elsewhere. Reproduced on Linux (WSL) with and without `TZ=UTC`: the OS makes no difference | 167 |
| EF-X721 | Non-deterministic: the entity graphs of the `ManyToManyTracking`, `StoreGenerated` and `Updates` tests all have default key 0 (EF-X101), so which entity hits the duplicate key first, and so the exception (or whether one is thrown), depends on the traversal order and varies from run to run (hash randomization). Their exact-exception assertions on EF11 use `SpecFails.AnyOutcome`. Seen on EF11 only; not seen on EF8 to EF10 | 125 |
| EF-X800 | Convert/Parse of a string to byte/decimal/double/short/int/long (`Parse(ToString(b.Int))`) is not supported by the driver LINQ translator (EF11 `*_Parse` tests) | 6 |
| EF-X801 | `Random.Shared.Next`, `new Random().Next` and `EF.Functions.Random` cannot be evaluated or translated | 7 |
| EF-X802 | `Convert.ToBoolean` on a bool is not supported | 1 |
| EF-X803 | `Convert.ToByte` is not supported | 1 |
| EF-X804 | `Convert.ToDecimal` is not supported | 1 |
| EF-X805 | `Convert.ToDouble` is not supported | 1 |
| EF-X806 | `Convert.ToInt16`/`ToInt32`/`ToInt64` are not supported | 3 |
| EF-X807 | `Convert.ToString` is not supported | 1 |
| EF-X808 | `DateTime.Compare`/`CompareTo` against an Unspecified-Kind DateTime returns different rows than client evaluation (local-time shift; depends on the machine time zone) | 1 |
| EF-X809 | Boolean `^` (Xor over boolean) is not supported: MongoDB has no boolean `$xor` operator | 1 |
| EF-X810 | `<<` (left shift) is not supported by the driver | 1 |
| EF-X811 | `>>` (right shift) is not supported by the driver | 1 |
| EF-X812 | **Wrong results.** EF11 `Primitive_collection_in_query` (`array.Count(a => a.Equals(value)) == 2`) silently matches no document for TimeSpan, SByte, UShort, Char, Decimal, DateTimeOffset, UInt, Float, ULong and byte[] element types (int, long, short, string, bool, double, DateTime and the enum pass) | 10 |
| EF-X813 | DateOnly/TimeOnly array elements in a primitive collection query are serialized through the object serializer and rejected (`BsonSerializationException`) | 2 |
| EF-X814 | Guid array elements in a primitive collection query cannot be serialized (GuidRepresentation Unspecified) | 1 |
| EF-X815 | A `byte` primitive collection (`byte[]` field) cannot be queried as an array: `ByteArraySerializer` is not an `IBsonArraySerializer` | 1 |
| EF-X850 | Navigations suites: query results have null associate navigations (documents in the other collection are not loaded or fixed up). Not root-caused; may mix several causes | 32 |
| EF-X851 | Unsupported cross-DbSet query: the associate is a separate collection and the query needs a join | 14 |
| EF-X852 | Query over associate documents in a separate collection cannot be translated (subquery/SelectMany/aggregate) | 11 |
| EF-X853 | Different exception type than the relational providers throw (NullReference / ExpressionNotSupported / NotSupported) | 5 |
| EF-X854 | Unmapped associate property: driver `ExpressionNotSupportedException` instead of EF `InvalidOperationException` | 1 |
| EF-X855 | Untranslatable client method in projection: driver `ExpressionNotSupportedException` instead of client evaluation | 2 |
| EF-X856 | SelectMany over a nested collection of an optional/required associate cannot be translated | 3 |
| EF-X857 | EF11-only tests: null associate navigation in the result is not populated | 4 |
| EF-X858 | Distinct/Select over an owned collection (`EF.Property<List<T>>.AsQueryable()`) cannot be translated | 3 |
| EF-X859 | GroupBy over an owned collection: driver `ExpressionNotSupportedException` | 1 |
| EF-X860 | Nested aggregate over an owned collection inside a Select cannot be translated | 1 |
| EF-X861 | Projecting through an optional owned associate that is null throws `NullReferenceException` | 3 |
| EF-X862 | Nested collection of a null optional owned associate throws "required but not present in BsonDocument" | 1 |
| EF-X863 | Projection over an owned collection materializes different values than expected (strings differ) | 3 |
| EF-X864 | Set operation over different owned collections fails in the server aggregate (`MongoCommandException`) | 1 |
| EF-X865 | Equality between owned associates: driver `ExpressionNotSupportedException` | 3 |
| EF-X866 | Owned associate comparison with inline/parameter value does not throw as expected | 2 |
| EF-X867 | **Wrong results, not yet run in isolation (see the test pollution warning).** Associate/collection equality or Distinct returns the wrong number of rows (`NavigationsStructuralEquality.Two_nested_associates` 7 instead of 1, `Two_nested_collections` 1 instead of 2, `OwnedNavigationsCollection.Distinct_over_projected_filtered_nested_collection` 3 instead of 5) | 3 |
| EF-X900 | Ad hoc JSON data for MongoDB not ported: the abstract `Seed*` methods write raw relational JSON column values and are no-ops here (EF10 AdHocJson, and the AdHocJson tests merged into `JsonQueryTestBase<>` on EF11), so those tests find no or incomplete data | 48 |

### Shared root causes across the ported suites

The porting agents filed one ticket per failure message, so one underlying gap often appears under several tickets. The
grouping below is by symptom as each batch reported it; "same root cause" is a hypothesis until one of them is fixed.

- **No integer key generation** (the provider generates no values for `int`/`long` keys, while the relational-style upstream
  models expect identity columns): `EF-X101`, `EF-X103`, `EF-X105`, `EF-X205`, `EF-X211`, `EF-X304`, `EF-X522`. The ported
  fixtures that are not about this gap work around it with `MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true)`
  (a client-side generator). The tracking/graph suites (`GraphUpdates`, `StoreGenerated`, `StoreGeneratedFixup`, `ManyToManyTracking`,
  `Updates`, `OptimisticConcurrency`) now use it too, so `EF-X101` only counts what still fails with client-side keys.
  `EF-X306` (duplicate `_id` for value-converted keys) may be the same default-key collision; not verified.
- **Single-property keys must map to `_id`**: `EF-X201`, `EF-X307`, `EF-X405`, `EF-X547`. Upstream models name their keys freely.
- **Navigations to non-root types become owned by convention**, so relationships between root entity types are not
  discovered, `Set<T>()` is rejected, and many-to-many join entities are owned: `EF-X102`, `EF-X112`, `EF-X213`, `EF-X308`,
  `EF-X401`, `EF-X403`, `EF-X409`, `EF-X523`, `EF-X538`, `EF-X539`, `EF-X544`, `EF-X545`. Whether this is the intended
  embedding model or a gap needs a decision before it is ticketed as a bug.
- **Derived types and the discriminator**: `EF-X200`, `EF-X206`, `EF-X216`, `EF-X519`, `EF-X537`. Basic hierarchy queries and
  `Find` through a derived set fail; three batches hit it independently, so it is likely to matter for ordinary inheritance use.
- **Concurrency**: tokens on owned types are rejected (`EF-X111`, `EF-X214`, `EF-X319`); the concurrency exception is thrown
  without reaching interceptors (`EF-X400`) or reports a conflict for a partial update (`EF-X114`); message wording differs
  (`EF-X115`).
- **Many-to-many**: `Include` and `Load` of skip navigations (`EF-X110`, `EF-X208`, `EF-X209`).
- **Unique indexes over nullable columns** collide on `null` (`EF-X106`, `EF-X207`); the fixtures relax them with
  `MongoSpecModelHelpers.RelaxNullableUniqueIndexes` / `RelaxUniqueForeignKeyIndexes`.
- **Cross-collection queries** (`EF-216`, `EF-220`, `EF-X001`, `EF-X020`, and the unsplit `EF-X526`) remain the largest block.
- **Owned collections** (`EF-X512` to `EF-X517`, `EF-X520`, `EF-X521`) and **primitive collections** (`EF-X500` to `EF-X511`).
- **Time-zone dependence**: several `DateTimeTranslations` tests (`EF-X006`) return wrong results when the machine is not on
  UTC because an `Unspecified` `DateTime` constant is treated as local time; those overrides accept either outcome.

### EF-X001 — Sub-query selection across DbSets is not translated
Comment patterns: `// Fails: Subquery selection EF-X001`, `// Fails: Subqueries not supported EF-X001`, `// Fails: No subquery support EF-X001`.
Test-body patterns: `AssertTranslationFailed(() => base.X(...))`, `AssertNoMultiCollectionQuerySupport(() => base.X(...))`.
Affected: ~140 tests across `NorthwindAggregateOperators`, `NorthwindMiscellaneous`, `NorthwindWhere`, `NorthwindNavigations`, `NorthwindSetOperations`, `NorthwindJoin`, etc. Many overlap with [EF-216](https://jira.mongodb.org/browse/EF-216); a separate ticket lets cross-collection subquery support be tracked independently from raw navigation access.

### EF-X002 — Provider throws a different exception than the EF translation-failure message
Comment patterns: `// Fails: Not throwing expected translation failed exception from EF, but still throws EF-X002`, `// Fails: Not throwing expected translation failed exception from EF. EF-X002`, `// Fails: Does not throw expected unable to translate exception EF-X002`, `// Fails: Does not use translation failed message EF-X002`, `// Fails: Throws different exception, but still throws EF-X002`.
Affected: ~48 tests. EF's base tests expect `InvalidOperationException` with EF's "could not be translated" message; the provider currently throws `ExpressionNotSupportedException` / `MongoCommandException` / `NotSupportedException` instead. Aligning the messages would let EF-level diagnostics work without provider-specific overrides. Two of these are bulk-update shapes in `NorthwindBulkUpdatesMongoTest` — `Update_Concat_set_constant` / `Update_Union_set_constant`, asserted with `Assert.ThrowsAnyAsync<Exception>`. These are a **test-harness/transaction artifact rather than a bulk-path defect**: the conformance asserter's before/after snapshot query mirrors the `Concat`/`Union` source as `$unionWith`, which the server forbids inside the rollback transaction the fixture enlists (`MongoCommandException: Stage not supported inside of a multi-document transaction: $unionWith`) — so it fails before the provider's own clean bulk rejection. (The two GroupBy bulk shapes that previously sat here — `Delete_Where_predicate_with_GroupBy_aggregate`, `Delete_GroupBy_Where_Select_2` — now reject with the canonical translation-failure message via the bulk translation guard and are tracked under EF-X016.) Three further bulk shapes sit here for the same reason — `Update_with_join_set_constant` (EF9/EF10) and the EF10-only `Update_with_LeftJoin` / `Update_with_LeftJoin_via_flattened_GroupJoin`, asserted with `Assert.ThrowsAnyAsync<Exception>`: these `Join`/`LeftJoin`/`GroupJoin` sources became EF-translatable with the cross-collection-join work (EF-117), so the asserter's before/after snapshot reads the join (whose inner is a filtered subquery, not a bare collection) and the driver's LINQ provider throws `ExpressionNotSupportedException` before the bulk path's own rejection runs.

### EF-X003 — Driver-level feature gaps surfaced as test failures
Comment patterns: `// Fails: Unsupported by driver EF-X003`, `// Fails: Reverse not supported by driver EF-X003`, `// Fails: Limited support on client evaluation EF-X003`.
Affected: ~9 tests. These are MongoDB C# Driver gaps (the driver does not implement a particular LINQ-to-MQL translation). Many likely fold into existing `CSHARP-*` tickets; a single umbrella issue would let the provider track its dependency on driver work.

### EF-X004 — Float `Sum`/`Average` truncation (likely duplicate of EF-228) — **resolved**
Status: no `// Fails:` tags remain in the spec tests and the formerly-failing tests pass; the text below is historical.
Comment pattern: `// Fails: Truncation resulted in data loss EF-X004`.
Affected: 1 test (`NorthwindAggregateOperatorsQueryMongoTest.cs`). Almost certainly the same failure mode as [EF-228](https://jira.mongodb.org/browse/EF-228); recommend re-tagging with `EF-228` and dropping this ticket once confirmed.

### EF-X005 — BSON document missing nested required reference (AdHoc JSON)
Comment patterns: `// Fails: NestedRequiredReference is null in BsonDocument for entity id=6 EF-X005`, `// Fails: Entity id=5 has no RequiredReference field EF-X005`.
Affected: 2 tests in `AdHocJsonQueryMongoTest.cs`. Same family as [EF-164](https://jira.mongodb.org/browse/EF-164) but more specific — failure occurs when an owned navigation (rather than a scalar) is missing from the seeded BSON.

### EF-X006 — MongoDB `DateTimeKind` round-trip handling
Comment pattern: `// Fails: MongoDB DateTimeKind handling EF-X006`.
Affected: 1 test. BSON cannot represent `DateTimeKind.Unspecified`, so the provider normalizes to UTC; tests that compare original vs round-tripped `DateTime.Kind` diverge.

### EF-X007 — Views / `HasDefiningQuery` semantics for MongoDB collections
Comment patterns: `// Fails: Views are not supported, so this returns all entities from mapped collection. EF-X007`, `// Fails: Defining queries are not supported. EF-X007`.
Affected: 2 tests in `NorthwindKeylessEntitiesQueryMongoTest.cs`. The EF "view" / `HasDefiningQuery` notion doesn't map onto MongoDB; the provider returns the full collection instead of the view-filtered subset.

### EF-X008 — No support for nested JSON in AdHoc JSON tests
Comment pattern: `// Fails: No support for nested JSON EF-X008`.
Affected: 2 tests in `AdHocJsonQueryMongoTest.cs`. The provider's JSON-column emulation does not nest deeply enough for these EF Core AdHoc cases.

### EF-X009 — Single uncategorized failure — needs triage — **resolved**
Status: no `// Fails:` tags remain in the spec tests and the formerly-failing tests pass; the text below is historical.
Comment pattern: `// Fails: Unknown reasons EF-X009`.
Affected: 1 test. Author was unsure of root cause when adding the override.

### EF-X010 — Provider-specific Include error message differs from EF baseline
Pattern: `Include_collection_with_client_filter` across all four Include variants asserts
`Assert.Contains("ExpressionNotSupportedException", (await Assert.ThrowsAsync<ThrowsException>(...)).Message)`
— the same shape as the other `EF-X002` sites. The base test expects EF's own
`InvalidOperationException` translation-failed message for a client-evaluated member on an
Include-d query; the provider instead throws the driver's `ExpressionNotSupportedException`,
so `Assert.ThrowsAsync<InvalidOperationException>` inside the base method itself fails with a
`ThrowsException` reporting the actual type. The mechanism is identical to `EF-X002` — see that
entry for the general case; this one's trigger is specifically a client-evaluated member on an
`Include`, not a plain unsupported operator.
Affected: 4 tests (`NorthwindEFPropertyIncludeQueryMongoTest.cs`, `NorthwindIncludeNoTrackingQueryMongoTest.cs`, `NorthwindIncludeQueryMongoTest.cs`, `NorthwindStringIncludeQueryMongoTest.cs`).

Until this was fixed, each of these four suites shadowed the upstream `AssertTranslationFailed`
helper with a bare `try { await query(); } catch { return; }`, which — unlike the
`Assert.ThrowsAsync<ThrowsException>` pattern above — swallowed *any* exception, including an
assertion failure from wrong data returned by a query that didn't fail to translate at all. A
full audit of all 234 call sites of the shadowed helper (across the four suites) found this one
test as the only case actually masking a real failure; the other 230 call sites already threw a
genuine non-assertion exception and are unaffected. The shadowed helpers now delegate to a
corrected `MongoAssert.AssertTranslationFailed` (rejects any `XunitException`, so wrong data
still fails the test) instead of duplicating the swallow-everything logic in each file.

### EF-X011 — Compiled query with non-query operator — wrong exception — **fixed by [EF-233](https://jira.mongodb.org/browse/EF-233)**
Comment pattern (historical): `// Fails: Compiled query with non-query operator issue EF-X011`.
Root cause: `MongoQueryableMethodTranslatingExpressionVisitor` captured the "final" query-chain expression in its generic `Visit` override (`_finalExpression ??= expression`), which fires on whatever node is visited first. For a compiled query ending in a non-query operator (e.g. `.Count() == 1`), the wrapping `BinaryExpression` was visited before the actual `Count()` call, so the captured expression included the trailing comparison and downstream translation broke (`ArgumentException("No ultimate source found")`, or the reported `InvalidCastException` for other shapes). Fixed by moving the capture into `VisitMethodCall` (`_finalExpression ??= methodCallExpression`), which only ever fires on the true queryable-chain nodes. `NorthwindCompiledQueryMongoTest.Compiled_query_when_does_not_end_in_query_operator` now calls `base.*` directly and asserts the correct MQL baseline.

One incidental side effect: `NorthwindCompiledQueryMongoTest.Multiple_queries` (two separately-compiled queries against different `DbSet`s reusing translation state — a pre-existing [EF-216](https://jira.mongodb.org/browse/EF-216) cross-DbSet limitation) now surfaces as an `InvalidOperationException` ("was not flattened to a projection index") from `MongoProjectionBindingRemovingExpressionVisitor.GetProjectionIndex`. The test was updated to assert the new exception type; per this repo's versioning conventions, the exception type for an already-unsupported shape is not part of the public contract.

### EF-X012 — `OfType` translation unsupported — **resolved**
Status: no `// Fails:` tags remain in the spec tests and the formerly-failing tests pass; the text below is historical.
Comment pattern: `// Fails: OfType translation EF-X012`.
Affected: 2 tests in `NorthwindAggregateOperatorsQueryMongoTest.cs` (`OfType_Select`, `OfType_Select_OfType_Select`). `Queryable.OfType<T>()` is not translated; the failure surfaces as a generic `AssertTranslationFailed`.

### EF-X013 — MongoDB has no `$xor` operator
Comment pattern: `// Fails: MongoDB does not have an xor operator EF-X013`.
Affected: 1 test (`NorthwindWhereQueryMongoTest.Where_bitwise_xor`). The provider throws `ExpressionNotSupportedException` with message `"because MongoDB does not have a boolean $xor operator"`.

### EF-X014 — Server-side projection conflict with cast-to-nullable
Comment pattern: `// Fails: Server-side projection conflict with cast-to-nullable EF-X014`.
Affected: 1 test (`NorthwindSelectQueryMongoTest.Select_bool_closure_with_order_by_property_with_cast_to_nullable`). Server rejects the generated `$project` stage with `"Cannot do exclusion on field _key1 in inclusion projection"` — likely a translator bug where the projection stage mixes inclusion and exclusion.

### EF-X015 — Sub-second `DateTime` component translation (nanosecond/microsecond)
Comment pattern: `// Fails: Sub-second DateTime component translation EF-X015`.
Affected: 1 test (`NorthwindMiscellaneousQueryMongoTest.Where_nanosecond_and_microsecond_component`). `DateTime.Nanosecond` and `DateTime.Microsecond` (added in .NET 7) are not translated; the provider throws `ExpressionNotSupportedException`.

### EF-X016 — Bulk `ExecuteUpdate`/`ExecuteDelete` source restricted to a single collection scoped by `Where`
Comment pattern: `// Fails: ExecuteUpdate/ExecuteDelete source restricted to a Where predicate; <shape> unsupported EF-X016`.
Test-body pattern: `AssertTranslationFailed(() => base.X(...))`.
Affected: 61 tests: 42 in `NorthwindBulkUpdatesMongoTest.cs`, 11 in `NonSharedModelBulkUpdatesMongoTest.cs`, 4 in `InheritanceBulkUpdatesMongoTest.cs` and 4 in `FiltersInheritanceBulkUpdatesMongoTest.cs`. By design, the provider supports bulk `ExecuteUpdate`/`ExecuteDelete` only against a single collection scoped by `Where`, with constant / parameter / self-referencing scalar setters (see README "What is supported" and EF-107). The EF conformance suite exercises shapes outside that subset — joins / correlated subqueries, set operations (`Except`/`Intersect`), `GroupBy`, `SelectMany`, cross-document navigation predicates, non-entity projection sources, and multiple-collection updates — and the provider rejects them during translation with an `InvalidOperationException` whose message reports the LINQ expression could not be translated. This is a documented limitation rather than a bug to fix; the ticket tracks the boundary so any future expansion of the supported subset (or a behavior change) is detected. Most shapes are rejected by compile-time bulk-source validation; a few (e.g. a `GroupBy` subquery inside the `Where` predicate) only fail when the filter is built at execution time, where a translation guard (`TranslateBulkOrThrow` in `MongoShapedQueryCompilingExpressionVisitor`) converts the raw driver/expression exception into the same canonical non-query translation failure. A handful of sibling shapes surface differently and are tagged separately: cross-DbSet `GroupBy` rejections under [EF-216](https://jira.mongodb.org/browse/EF-216), and the `$unionWith`-in-transaction `Concat`/`Union` cases under `EF-X002`.

**Remaining unsupported shapes — grouped by root cause.** The unifying cause: bulk execution needs a source the provider can express as a single-collection filter (single command) or read to collect the target `_id`s (two-phase, selected by `Skip`/`Take` or `OfType<TDerived>()`). Every group below fails because the source shape itself cannot be translated — so neither phase 1 nor a single-command filter can be built. (Delete has the analogous cases; the `Update_*` methods are enumerated here since they are the larger set.)

- **Joins / correlated subqueries** — `RightJoin`, cross-apply, outer-apply, and the cross-/two-collection join shapes the read path cannot translate, so phase 1 cannot project `_id`s and the source is rejected at translation with EF's canonical message. Unblocking requires net-new read-side join translation (ultimately a driver `$lookup`/`$group` + `$merge` capability) and is out of scope for EF-107. Update cases: `Update_Where_Join_set_property_from_joined_single_result_scalar`, `Update_Where_Join_set_property_from_joined_single_result_table`, `Update_Where_Join_set_property_from_joined_table`, `Update_with_two_inner_joins`, `Update_with_left_join_set_constant`, `Update_with_cross_join_set_constant`, `Update_with_cross_apply_set_constant`, `Update_with_outer_apply_set_constant`, `Update_with_cross_join_cross_apply_set_constant`, `Update_with_cross_join_left_join_set_constant`, `Update_with_cross_join_outer_apply_set_constant`, `Update_with_PK_pushdown_and_join_and_multiple_setters`, plus EF10-only `Update_with_RightJoin`. (The simple single-key `Join` / `LeftJoin` / `GroupJoin` shapes — `Update_with_join_set_constant` and EF10-only `Update_with_LeftJoin` / `Update_with_LeftJoin_via_flattened_GroupJoin` — became read-translatable with the cross-collection-join work (EF-117), so they no longer fail at bulk translation; the conformance asserter's snapshot read of the filtered-inner join now throws a driver non-translation exception instead, tracked under EF-X002.)
- **Set operations — `Except` / `Intersect`** — MongoDB has no cross-collection intersect/except (only `$unionWith`), so the source cannot be expressed and is rejected at translation. Update cases: `Update_Except_set_constant`, `Update_Intersect_set_constant`.
- **`SelectMany`** — the read path has no cross-document flattening, so the source cannot be translated. Update cases: `Update_Where_SelectMany_set_null`, `Update_Where_SelectMany_subquery_set_null`.
- **Cross-document navigation predicates** — predicates traversing navigations across collections aren't translatable. Update cases: `Update_Where_using_navigation_2_set_constant`, `Update_Where_using_navigation_set_null`.
- **Multiple-collection update** — a relational table-sharing concept with no document-model meaning; rejected by design regardless of phase strategy. Update case: `Update_multiple_tables_throws`.

**Related shapes that surface as a different failure mode** (cross-referenced, not under the canonical translation-failure pattern above):

- **`Concat` / `Union` (`EF-X002`)** — these lower to `$unionWith`, which the server forbids inside the multi-document transaction the conformance asserter (and the two-phase path) run in, so they throw a runtime `MongoCommandException` *before* the provider's clean bulk rejection. Update cases: `Update_Concat_set_constant`, `Update_Union_set_constant`. Unblocking requires the server to allow `$unionWith` inside a transaction.
- **`GroupBy`-scoped (`EF-216`)** — `GroupBy` isn't translatable; these surface as the provider's `Unsupported cross-DbSet query` rejection and are asserted via `AssertNoMultiCollectionQuerySupport` rather than a `// Fails:` `AssertTranslationFailed`. Update cases: `Update_Where_GroupBy_First_set_constant`, `Update_Where_GroupBy_First_set_constant_2`, `Update_Where_GroupBy_First_set_constant_3`, `Update_Where_GroupBy_aggregate_set_constant`.

**Bulk support (EF-107, EF-361):** `Where`-scoped bulk delete and update, with or without `OrderBy`/`ThenBy`/`Distinct` (which do not change which documents match), run as a single `deleteMany`/`updateMany`. `Skip`/`Take`-scoped and `OfType<TDerived>()`-scoped ones run through a transactional two-phase strategy: phase 1 projects the matching `_id` values (including composite-key entities); phase 2 issues the `deleteMany`/`updateMany` scoped to that `_id` set. The corresponding conformance tests (`Delete_Where_OrderBy*`, `Delete_Where_Skip*`, `Delete_Where_Take*`, `Delete_Where_Distinct`, `Update_Where_OrderBy*`, `Update_Where_Skip*`, `Update_Where_Take*`, `Update_Where_Distinct_set_constant`, `Delete_Where_Skip_Take_Skip_Take_causing_subquery`, `Update_Where_OrderBy_Skip_Take_Skip_Take_set_constant`) have been promoted from `AssertTranslationFailed` to `base.*` and are no longer tracked under this ticket. So have six `OfType` wrappers and six wrappers that set a property declared only on a derived type or an interface (`Update_derived_property_on_derived_type`, `Update_base_and_derived_types`, `Update_with_interface_in_property_expression`, `Update_with_interface_in_EF_Property_in_property_expression`) in `InheritanceBulkUpdatesMongoTest` and `FiltersInheritanceBulkUpdatesMongoTest`, which now call `base`: a setter lambda typed by the narrowed source resolves a derived-only property against that type. `Cast` is still rejected.

### EF-X023 — GroupJoin shapes not translated
Comment pattern: `// Fails: GroupJoin shape not translated EF-X023`.
Affected: 6 tests in `NorthwindJoinQueryMongoTest.cs` — `GroupJoin_aggregate_anonymous_key_selectors` (+`2`, +`_one_argument`), `Unflattened_GroupJoin_composed`, `Unflattened_GroupJoin_composed_2`, `GroupJoin_on_true_equal_true` (EF9 only). These are `GroupJoin` shapes the flatten-to-`$lookup` pipeline does not yet handle (aggregate / anonymous or nested key selectors, multiple `DefaultIfEmpty`, on-`true == true`, mixed projection). Distinct from the `GroupJoin` *subquery* shapes, which are tracked under [EF-X001](#ef-x001--sub-query-selection-across-dbsets-is-not-translated), and from cross-collection `Include`, which is [EF-117](https://jira.mongodb.org/browse/EF-117).

### EF-X017 — Join shapes not translated
Comment pattern: `// Fails: Join shape not translated EF-X017`.
Affected: 4 tests — `Join_complex_condition`, `Join_with_key_selectors_being_nested_anonymous_objects`, `Join_local_collection_int_closure_is_cached_correctly` (all in `NorthwindJoinQueryMongoTest.cs`; `Join_composite_key` now translates natively, EF-436) and `Join_with_default_if_empty_on_both_sources` (in `NorthwindMiscellaneousQueryMongoTest.cs`). The provider translates simple single-key `Join`/`GroupJoin` to `$lookup`, but composite keys, complex (non-equality) conditions, nested-anonymous key selectors, joins against a local in-memory collection, and `DefaultIfEmpty` on both sources are not yet supported.

### EF-X018 — RightJoin not supported
Comment pattern: `// Fails: RightJoin not supported EF-X018`.
Affected: 5 tests (`NorthwindJoinQueryMongoTest.RightJoin` and `Include_collection_with_right_join_clause_with_filter` in the four Include suites, EF10+). `Queryable.RightJoin` (the EF9+/EF10 operator) is not translated; the provider fails translation rather than emitting the reversed `$lookup` pipeline.

### EF-X019 — Include on keyless entity not supported (no primary key for $lookup join)
Comment pattern: `// Fails: Include on keyless entity not supported (no primary key for $lookup join) EF-X019`.
Affected: 2 tests (`NorthwindKeylessEntitiesQueryMongoTest.KeylessEntity_with_included_nav`, `KeylessEntity_with_included_navs_multi_level`). A keyless entity has no primary key, so the cross-collection `$lookup` join-key cannot be resolved and keyless entities are never tracked (no `InternalEntityEntry` is emitted into the shaper). The provider now detects this in `MongoProjectionBindingRemovingExpressionVisitor.AddInclude` and throws the standard `CoreStrings.TranslationFailed` (translation-failure) message instead of the internal `Sequence contains no matching element` error. Distinct from cross-collection `Include` on keyed entities, which is implemented (formerly [EF-117](https://jira.mongodb.org/browse/EF-117)).

### EF-X020 — Cross-collection Include/join/navigation not translated on EF8/EF9 — **resolved**
Status: no `// Fails:` tags remain in the spec tests and the formerly-failing tests pass; the text below is historical.
Comment pattern: `// Fails: Cross-collection Include/join not translated on EF8/EF9 EF-X020`.
Test-body pattern: the override is wrapped in `#if EF8 || EF9` / `#else`. The `#if EF8 || EF9` branch asserts the translation failure (`AssertTranslationFailed(() => base.X(...))`); the `#else` branch keeps the working EF10 baseline (the real `base` call plus its `AssertMql(...)`).
Affected: 176 tests (plus 54 in the ported loading/proxy/field-mapping/notification bases: `LoadMongoTest` 30, `FieldsOnlyLoadMongoTest` 14, `LazyLoadProxyMongoTest` 8, `FieldMappingMongoTest` 1, `NotificationEntitiesMongoTest` 1) across `NorthwindEFPropertyIncludeQueryMongoTest`, `NorthwindStringIncludeQueryMongoTest`, `NorthwindIncludeQueryMongoTest`, `NorthwindIncludeNoTrackingQueryMongoTest`, `NorthwindNavigationsQueryMongoTest`, `NorthwindMiscellaneousQueryMongoTest`, `NorthwindJoinQueryMongoTest`, `NorthwindAggregateOperatorsQueryMongoTest`, `NorthwindAsNoTrackingQueryMongoTest`, `NorthwindKeylessEntitiesQueryMongoTest`, `NorthwindSelectQueryMongoTest`, `NorthwindSetOperationsQueryMongoTest`, `NorthwindWhereQueryMongoTest`, and `BuiltInDataTypesMongoTest`. These are the cross-collection Include/`ThenInclude`/join/navigation shapes implemented for the EF10-targeted query pipeline. On EF8/EF9 the upstream nav-expansion / query pipeline produces a different expression shape (e.g. an extra `.OrderBy(o => o.OrderID)` injected during navigation expansion) that the EF10-targeted translator does not handle, so translation fails with EF Core's `InvalidOperationException` "could not be translated" (a few also throw the provider's `ExpressionNotSupportedException`); the same query translates and runs on EF10. The provider's local `AssertTranslationFailed` helper swallows whichever exception is thrown, so both shapes are covered. Four `Include_reference_dependent_already_tracked` overrides (in the four Include suites) emit MQL from a first principal query *before* the Include sub-query fails to translate, so their `#if EF8 || EF9` branch asserts only the translation failure and omits the empty `AssertMql()`. `BuiltInDataTypesMongoTest.Can_read_back_bool_mapped_as_int_through_navigation` is split three ways (a nested `#if EF9` inside the file's `#if !EF8` branch, plus the `#else` EF8 branch) because that file uses async signatures on EF9/EF10 and sync `void` signatures on EF8.

### EF-X022 — Join/GroupJoin whose inner source is a filtered or ordered sub-query is rejected
Comment pattern: `// Fails: Join/GroupJoin inner sub-query (filtered/ordered) not supported EF-X022`.
Test-body patterns: `Assert.ThrowsAsync<MongoDB.Driver.Linq.ExpressionNotSupportedException>` asserting
`"Expression not supported"`, or `Assert.ThrowsAnyAsync<Exception>` where the EF8/EF9 failure mode differs.

The provider lowers a single-key `Join`/`GroupJoin` to `$lookup`. When the **inner** source is not a bare
collection but a sub-query — `Orders.Where(...)`, `Orders.OrderBy(...)`, or a dependent-side query filter
that lowers to the same `Where` — the driver's LINQ provider rejects the whole expression with
`ExpressionNotSupportedException: ... because expression must be a MongoDB IQueryable against a collection`.
This is an **intentional rejection**: these shapes were previously folded into the correlated `$lookup`
sub-pipeline, which is not a faithful translation — the mis-folding formerly tracked as `CSHARP-6017`.
Failing loudly is preferred over returning wrong results, so this is a documented boundary rather than a
regression to fix in the test suite.

Affected: 18 overrides, all asserting the rejection (none are `Skip`ped).

- `NorthwindJoinQueryMongoTest` — `Join_customers_orders_with_subquery`,
  `Join_customers_orders_with_subquery_predicate`, `Join_customers_orders_with_subquery_anonymous_property_method`,
  `GroupJoin_customers_employees_subquery_shadow`, `GroupJoin_SelectMany_subquery_with_filter`, plus
  EF10-only `GroupJoin_DefaultIfEmpty2` and `GroupJoin_SelectMany_subquery_with_filter_and_DefaultIfEmpty`.
- `NorthwindMiscellaneousQueryMongoTest` — `OrderBy_Join`, `Join_take_count_works`.
- `NorthwindQueryFiltersQueryMongoTest` (EF10-only) — `Entity_Equality`, `Included_many_to_one_query`,
  `Included_many_to_one_query2`, where the *query filter* on the `$lookup` target is what makes the inner
  a filtered sub-query.
- **Former `CSHARP-6017` skips**, now converted to the same assertion style rather than
  `[ConditionalTheory(Skip = ...)]` — `Join_customers_orders_with_subquery_with_take`,
  `Join_customers_orders_with_subquery_predicate_with_take`,
  `Join_customers_orders_with_subquery_anonymous_property_method_with_take`, `GroupJoin_simple_subquery`,
  `GroupJoin_Subquery_with_Take_Then_SelectMany_Where`, `GroupJoin_customers_employees_subquery_shadow_take`.
  These were skipped while the driver silently mis-folded the `Take`/sub-query inner and returned wrong
  results; the driver now rejects them, so they assert the rejection and no longer need a skip.

Interaction with [EF-352](https://jira.mongodb.org/browse/EF-352): that fix (shadow/scalar `EF.Property`
read on a joined entity in a projection) re-enabled
`Join_customers_orders_with_subquery_anonymous_property_method` against driver 3.10, but the query's inner
is an ordered sub-query, so 3.11 rejects it and the override now asserts the rejection instead. The
underlying EF-352 fix is still covered functionally by
`ShadowPropertyJoinProjectionTests` and `ShadowPropertyFlatJoinProjectionTests`; the first of those had the
`orderby` dropped from its join inner (incidental to what it asserts) so it keeps exercising the non-root
joined-entity shaper on a shape the driver still supports.

The EF10-only entries carry a second ticket for their EF8/EF9 failure mode (for example `EF-X001`, `EF-202`,
`EF-216`), because on those versions the query fails earlier in nav-expansion and logs no MQL at all —
hence the `#if EF8 || EF9` split on the `AssertMql` baseline.

Lifting this ticket requires translating the inner sub-query into the `$lookup` sub-pipeline correctly
(correlated `let`/`pipeline` form), which overlaps with [EF-X021](#ef-x021--filtered-include--query-filter-on-cross-collection-target-not-translated).

### EF-371 — self-referencing two-hop reference navigation collapses to one join — **resolved**

Status: no `// Fails:` tags remain and the test passes; the text below is historical.

**History (kept deliberately — the original diagnosis was wrong).** This entry replaces
"EF-216 — wrong-data on EF10 (cross-collection navigation), unsupported on EF8/EF9", which covered
**five** `NorthwindNavigationsQueryMongoTest` methods skipped with
`[ConditionalTheory(Skip = "EF-216: multi-hop cross-collection navigation returns wrong data")]`:
`Include_with_multiple_optional_navigations`, `Multiple_include_with_multiple_optional_navigations`,
`Navigation_from_join_clause_inside_contains`, `Navigation_inside_contains_nested` and
`Select_Where_Navigation_Null_Deep`. They were believed to share one root cause — "compound multi-hop
navigation lowering". They did not.

**All five are now fixed.** Four via EF-369 / EF-370 (see
`docs/superpowers/specs/2026-08-03-required-nav-unwind-semantics-design.md`): the composed predicate /
`Contains` filter was being *discarded* when a multi-join Include chain was flattened to root-level
`_lookup_<Nav>` fields, so the query returned every row (2155 against 112 / 112 / 352 / 40 expected).
Those four are un-skipped, run on EF10 with real `AssertMql` baselines, and formerly took the standard
`#if EF8 || EF9` **EF-X020** arm, since removed (their navigations are optional, so EF lowers them to `Queryable.LeftJoin`,
whose dispatch case did not exist before EF10; the provider now admits the EF8/EF9 internal LeftJoin shim):

- `Include_with_multiple_optional_navigations`
- `Multiple_include_with_multiple_optional_navigations`
- `Navigation_from_join_clause_inside_contains`
- `Navigation_inside_contains_nested`

The fifth, `Select_Where_Navigation_Null_Deep` (a self-referencing two-hop navigation,
`e.Manager.Manager == null`), was a **different defect**, tracked and fixed as EF-371. It filters on
`e.Manager.Manager == null` over the self-referencing `Employee.Manager` navigation and was returning
**0 rows where 6 are correct**. Two causes, both independent of the discarded-operator bug fixed by
EF-369/EF-370:

1. `MongoQueryExpression._innerCollections` is keyed by `IEntityType`, so a self-referencing two-hop
   navigation registers only **one** inner collection. `InnerCollections.Count > 1` stays false, no
   forced-unwind lookups are registered, and the query never reaches the flat `_lookup_*` path — it stays
   on the chained driver-native join path.
2. On that path `RewriteJoinNode`'s guard `if (isLeftJoin && shapedPath && outerType == oldOuterType)`
   builds the preserving pipeline only for the **first** hop; at hop 2 the outer type is already a
   `LeftJoinResult`, so it falls through to `Queryable.Join` and the driver emits a bare `$unwind` — an
   **inner** join. Rows whose grandparent is absent are dropped, so `== null` can never match.

The fix: `_innerCollections` now registers per-navigation (not just per-`IEntityType`), lookup-alias
collisions are detected by the alias string a plain lookup would produce rather than by `INavigation`
identity (so two distinct navigations sharing a name are disambiguated too), and the residual `Where`
left over from stripping a join chain for `$lookup` is rewritten against the flattened
`_lookup_<Navigation>` fields instead of being discarded (the sibling `Select` is still discarded — its
projection is reconstructed independently by the compiled client-side shaper). It is un-skipped and
asserts real data + MQL on every EF version (the former EF8/EF9 `EF-X020` translation-failure arm no longer exists).

With all five un-skipped, the full spec suite is **green on all three EF versions** (EF8/EF9/EF10:
0 failures) and carries **no skips for this work**.

---

### EF-X021 — Filtered Include / query filter on cross-collection target not translated
Comment pattern: `// Fails: Filtered Include / query-filter predicate on a cross-collection $lookup target is not translated EF-X021`.
Affected: 0 spec overrides today (no specification test currently exercises a *filtered* cross-collection Include
with a predicate, nor a `HasQueryFilter` on a cross-collection dependent — `Filtered_include_with_multiple_ordering`
only uses OrderBy/Skip/Take, which *are* translated). Tracked because the provider now **fails loudly** for these
shapes instead of silently dropping the predicate. A user filtered-Include predicate
(`.Include(c => c.Orders.Where(o => ...))`) and a dependent-side `HasQueryFilter` (soft-delete / multi-tenant) both
lower to a `Where` inside the collection-Include subquery; that `Where` is **not** the synthetic FK-correlation join
condition and is not yet translated into the `$lookup` sub-pipeline `$match`. Previously it was silently dropped
(returning *all* dependents and bypassing the filter — wrong data); the provider now throws a translation failure
(`CoreStrings.TranslationFailed`). Translating the predicate into the sub-pipeline `$match` is the follow-up feature
this ticket tracks. Functional coverage:
`CrossCollectionIncludeTests.Filtered_collection_include_predicate_is_not_silently_dropped` and
`CrossCollectionIncludeTests.Query_filter_on_collection_include_target_is_not_silently_dropped`.

### EF-X024 — `Where`/entity materialization over a flattened multi-join chain onto ambiguous same-navigation or navigation-less siblings is not translated
Comment pattern: `// Fails: Where over a flattened multi-join chain is not translated EF-X024`, or
`// Fails: two navigation-less same-target-type joins chained off root, ... EF-X024`.
Affected: 2 spec overrides — `NorthwindJoinQueryMongoTest.Join_GroupJoin_DefaultIfEmpty_Where` (there is no longer a separate EF8/EF9 arm; the former `EF-X020` rejection is gone) and
`NorthwindJoinQueryMongoTest.Join_same_collection_multiple`.
Once a query has two or more cross-collection joins it is emitted in **flat** mode (one root-level
`$lookup` + `$unwind` per join, `MongoQueryExpression.UsesDriverJoinFields == false`). A composed operator
(`Where`, a projecting `Select`, etc.) wrapped around the join chain is reattached onto the flattened
`_lookup_<Navigation>` fields by `MongoEFToLinqTranslatingExpressionVisitor.StripJoinForLookup` /
`ReattachComposedOperator`, retargeting `TransparentIdentifier` `.Outer`/`.Inner` member accesses via
`TransparentIdentifierToLookupFieldRewriter`. That reattachment is now the general case — see
`SameTargetTypeJoinTests.Filter_after_a_flattened_multi_join_chain_is_applied_not_dropped`, which asserts a
`Where` placed after **two** joins (same-typed and different-typed chains) is genuinely applied, not
dropped or rejected.
The ONE shape that still fails is two INDEPENDENT (non-chained) joins that resolve the SAME navigation with
the SAME foreign-key property — e.g. `Join_GroupJoin_DefaultIfEmpty_Where`'s two plain joins from
`Customer` to `Order` on `CustomerID`. `TransparentIdentifierToLookupFieldRewriter.ResolveLookup`'s
ambiguity fallback disambiguates by structural position (a self-referencing CHAIN, where a later hop's
`$lookup.localField` is prefixed by an earlier hop's alias — see EF-372); two siblings with no
chaining/prefix relationship between them can't be told apart that way, so `StripJoinForLookup` declines
(returns `null`) and the join chain would otherwise survive to be rendered by the driver's native
`LeftJoin` support — which itself only handles a single cross-collection join and would silently
double-nest under a second `_outer`, materializing null entities. `GuardAgainstUnstrippableMultiJoin`
(`MongoEFToLinqTranslatingExpressionVisitor`) catches exactly that combination (`stripped == null` with 2+
forced-unwind lookups already registered) and fails translation loudly instead of falling back to that
broken native rendering. Disambiguating true siblings — the remaining piece this ticket tracks — needs a
way to tell two same-navigation, same-FK, non-chained joins apart (e.g. by originating LINQ join-clause
identity) that `ResolveLookup` doesn't have today.

`Join_same_collection_multiple` hits the same `GuardAgainstUnstrippableMultiJoin` decline via a different
route: two plain (inner) `Join` calls onto `Customer` with **no corresponding model navigation** at all
(a bare key-equality `Join`, not `Include`-driven). A navigation-less hop's `$lookup` (built directly from
the raw outer/inner key property paths — see EF-377) still registers as a forced-unwind lookup and still
triggers flat mode, but `StripJoinForLookup`/`ResolveLookup` key their matching on `LookupExpression.Navigation`
and structural position, which a bare hop can't fully disambiguate either, so the strip declines here too.
Unlike the `Where`-over-a-scalar-projection case this ticket originally tracked, this query materializes a
raw entity (`c3`) directly — the guard's original narrowing (reject only when a declined join is
left-outer, since a scalar/anonymous projection's baked-in field paths tolerate native-rendered inner-join
chains) doesn't hold for an entity-shaped result: entity materialization always reads through the
`_lookup_<Alias>`/`_inner` alias keyed by `MongoQueryExpression.UsesDriverJoinFields`, which the native
fallback doesn't produce once 2+ lookups are registered, regardless of join kind. The guard now rejects
every declined multi-lookup shape when the result is entity-shaped (reached via `Translate`, not
`TranslateProjected`), not only the left-outer ones.

### EF-X400 — `DbUpdateConcurrencyException` is thrown without invoking `ThrowingConcurrencyException`
Comment pattern: `// Fails: DbUpdateConcurrencyException is thrown without invoking ISaveChangesInterceptor.ThrowingConcurrencyException EF-X400`.
`SaveChangesInterceptionMongoTest.Intercept_SaveChanges_failed` (concurrencyError: true) and `Intercept_to_suppress_concurrency_exception` (16 cases per EF version). `MongoDatabaseWrapper.AssertWritesApplied` throws `DbUpdateConcurrencyException` directly rather than going through `IDiagnosticsLogger<DbLoggerCategory.Update>.OptimisticConcurrencyException`, so `ISaveChangesInterceptor.ThrowingConcurrencyException` is never called and an interceptor cannot observe or suppress the failure. The non-concurrency `Intercept_SaveChanges_failed` cases pass.

### EF-X401 — Ambiguous foreign key discovery between a join/owned type and its owner
`ModelBuilding101MongoTest.ManyToManyWith*NavsToJoinClassTest` (3 tests) and the ModelBuilder `OwnedTypes` `Can_configure_another_relationship_to_owner` shapes. Model finalization throws `Both relationships between 'PostTag' and 'Post.PostTags' and between 'PostTag' and 'Post' could use {'PostId'} as the foreign key`. Likely a consequence of the same navigation-implies-owned behavior as EF-X403.

### EF-X402 — Projecting an owned collection navigation directly is not translated
`MaterializationInterceptionMongoTest.Intercept_query_materialization_with_owned_types_projecting_collection` (4 cases on EF9/EF10, 2 on EF8). The query projects the owned collection itself and fails with `The LINQ expression ... could not be translated`.

### EF-X403 — Navigation targets are made owned, so ModelBuilder relationship tests see a different model
The ModelBuilder test bases (`MongoModelBuilder{Generic,NonGeneric}Test`, EF9+) configure relationships between regular entity types and then assert on the foreign keys and navigations. With the provider's conventions, types reachable only via a navigation become owned (ownership foreign key, `Ownership` flag), so the finalized model has a different shape and the base assertions hit a `NullReferenceException` (164 cases across OneToMany, ManyToOne, OneToOne and Inheritance). This is the dominant root cause for the relationship-test failures; EF-X401, EF-X405 and EF-X407 are related symptoms. Not necessarily a defect: it is the provider's documented embedding model, but the base tests assume top-level entities.

### EF-X404 — Collection-typed properties fail model validation in ModelBuilder tests
`Primitive_collections_*`, `Element_types_*`, `Can_set_unicode_for_primitive_collections` and similar (68 cases): `The 'List<int>' property 'CollectionQuarks.Charm' could not be mapped because the database provider does not support this type`. Affects `List<T>`, `T[]` and `ObservableCollection<T>` primitive collections when configured through the ModelBuilder API with the test helper's conventions. Real `List<T>` properties work in functional use, so this needs triage: it may be a gap in how primitive collections are registered in the model-building-only test path.

### EF-X405 — Primary key must be mapped to `_id`
Composite keys, shadow keys and field-backed keys configured by the relationship tests are rejected by `MongoModelValidator.ValidateEntityPrimaryKey` (`primary key property 'X' must be mapped to element '_id'`). 36 cases.

### EF-X406 — `HasData` on owned types fails seed validation
12 cases in the `OwnedTypes` ModelBuilder tests: `The seed entity for entity type 'OrderDetails' cannot be added because no value was provided for the required property`.

### EF-X407 — Owned type used from multiple navigations with different element names
12 cases in the `OwnedTypes` ModelBuilder tests: `Owned entity type 'Order' is used in multiple navigations ('Customer.Orders#Order' and 'OrderDetails.Order#Order') with different BSON element ...` is thrown by `MongoModelValidator.ValidateOwnedTypeMappingConsistency`.

### EF-X408 — Expected EF convention/validation exceptions are not thrown
8 cases (e.g. `Navigation_to_shared_type_is_not_discovered_by_convention`): `Assert.Throws() Failure: No exception was thrown`. The base test expects model finalization to reject a shape that the provider accepts.

### EF-X409 — Many-to-many `[ForeignKey]` navigation shapes cannot be determined
`ForeignKeyAttribute_configures_the_properties` (4 cases): `Unable to determine the relationship represented by navigation ...`.

### EF-X410 — Multiple row-version properties on one entity are rejected
`Properties_can_set_row_version` (2 cases): `NotSupportedException: Entity 'Quarks' has multiple properties 'Charm, Up' configured as row versions. Only one row version property per entity is supported.`

### EF-X411 — Model metadata differs from the relational baseline
24 cases across `NonRelationship` and `OwnedTypes` ModelBuilder tests where a metadata assertion differs: owned-type key/name shapes (`Strings differ`, `Collections differ`, `Sequence contains ...`) and `ValueGenerated` (`Expected: OnAdd, Actual: Never`) for properties the relational providers default to generating.

### EF-X412 — `CompiledModelTestBase`: unmapped CLR types are discovered as owned entity types
`MongoCompiledModelTest.No_NativeAOT`, `BigModel` and `ComplexTypes` (3 tests). The base model has `IPAddress`, `PhysicalAddress` and similar properties that the relational providers map with value converters; the Mongo provider discovers them as owned entity types and the model build fails with `No suitable constructor was found for ...`. `SimpleModel` passes. The test project needed `<PreserveCompilationContext>true</PreserveCompilationContext>` and extra Roslyn references to the provider and driver assemblies for the generated model to compile.
### Batch C — conversions, mapping and serialization (`EF-X300`..`EF-X324`)

Ported in `Mapping/`, `Metadata/` and `Query/Translations/` (EF10 only for the `Translations` bases): `ConvertToProviderTypes`, `CustomConverters`,
`ValueConvertersEndToEnd`, `KeysWithConverters`, `JsonTypes`, `BadDataJsonDeserialization`, `Serialization`, `DataAnnotation`, `MusicStore`,
`ConferencePlanner`, `Spatial` and the `ByteArray`/`Enum`/`Guid`/`DateOnly`/`TimeOnly`/`DateTime`/`DateTimeOffset`/`TimeSpan` `*TranslationsTestBase`
classes. Failing tests assert today's failure through `SpecFails.Async/Sync` (Utilities/SpecFails.cs) and carry a `// Fails: <reason> <ticket>` comment.
Counts are the number of overridden test methods carrying the tag (a method split across `#if` arms counts once). Where a theory only fails for some
data rows (or depends on the machine time zone) the override uses `SpecFails.*SomeRows`, which accepts success or the expected failure.

- **Fixture-level failures.** `CustomConverters`, `Serialization` and `Spatial` used to fail while the shared fixture seeded/built its model (EF-X300, EF-X319, EF-X320); their fixtures now work around the cause, so the tests run.
  The fixture swallows that exception so the tests run and each asserts its own failure; `ValueConvertersEndToEnd` fails the same way inside its single test.

- **EF-X300** (`Unsupported ValueConverter for 'Nullable<...>' ... Null conversion must be left to EF Core`): value converters declared via `HasConversion` with inline
  expressions on nullable properties throw `NotSupportedException` when the property is first serialized. For `CustomConverters` this breaks seeding, so the tests also
  see an empty collection.

- **EF-X301/X302/X303**: queries over a property with a value converter (parameter/literal comparison, `Where` on a shadow/enum-converted property, collection `Contains`)
  fail in the driver binding of `ValueConverterSerializer` (`InvalidCastException`, `NotSupportedException` for `IHasRepresentationSerializer`, `ArgumentException`).

- **EF-X304**: the provider only generates `ObjectId`/`Guid`/string keys, while the spec models use integer keys. `MongoSpecModelHelpers.MapSingleKeysToId` (test infrastructure)
  maps every root single-property key to `_id` and registers a test-only sequential generator for `int`/`long`/`short` keys so these models can be seeded. No test is tagged.

- **EF-X305/X306**: `KeysWithConverters`: querying a struct-typed key fails with `The binary operator Equal is not defined`; the insert tests (35) raise
  `MongoBulkWriteException` (duplicate `_id`) for value-converted keys, which needs investigation as it hits common converted-key shapes.

- **EF-X307**: `DataAnnotation` models whose key property is not named `Id` fail model validation (`primary key property 'X' must be mapped to element '_id'`).

- **EF-X308**: `DataAnnotation` relationship tests (inverse property / foreign key attribute / `NotMapped` ambiguity) behave differently because reference navigations
  are configured as owned by convention (`...is an ownership`, `...marked as owned`, abstract owned types, `NullReferenceException` when building the model). Not triaged individually.

- **EF-X309**: no database-side constraints, so inserting a null/too-long value does not raise `DbUpdateException`.

- **EF-X310/X311/X315/X319**: failures inside the test body that were not triaged further (`Sequence contains no elements`, `Operation is not valid...`,
  value differences). **EF-X317** (the JSON round-trip tests) is triaged: the Mongo type mappings have no `JsonValueReaderWriter`.

- **EF-X312**: `[Column(TypeName = ...)]` is raised as an error (`ColumnAttributeWithTypeUsed`) unless the warning is ignored; `DataAnnotation` models use it.

- **EF-X314**: the driver rejects the LINQ expression (`b.get_Item("IndexerVisible")`, anonymous-type projection with converted key, subquery over `Aggregate`).

- **EF-X316**: JSON-type models contain properties the provider cannot map (`List<int?>`/`IList<Guid>`/arrays of arrays of exotic elements, `Point`/`Polygon` geometry,
  `decimal[]`); the model fails to build. 121 methods across `JsonTypes` and `BadDataJsonDeserialization`.

- **EF-X318**: `BadDataJsonDeserialization` expects bad values to throw on materialization; for some row types no exception is thrown (theory rows, so `SomeRows`).

- **EF-X320**: NetTopologySuite types are discovered as entity types (`No suitable constructor was found for ...LineString`).

- **EF-X321**: `DateOnly.Year/Month/Day/...`, `TimeOnly.Hour/...`, `TimeSpan.Hours/...`, `DateTimeOffset.AddX/ToUnixTimeSeconds/Microsecond/...` and
  `FromDateTime`/`FromTimeSpan`/`ToDateTime` are not translated (`ExpressionNotSupportedException`; "serializer for System.X does not represent members as fields").

- **EF-X322/X323/X324**: `byte[]` queries: `Contains`/`First` need `IBsonArraySerializer` (`InvalidOperationException`), `Contains` over a column and `SequenceEqual`
  produce pipelines the server rejects (`$in requires an array`), and `byte[].Length` returns no rows (wrong result, 1 test).

- **Existing tickets reused**: EF-216 (cross-collection access, 13), EF-149 (`GroupBy`, 1), EF-X006 (the 5 `DateTimeTranslations` tests whose `Unspecified`-kind constants/parameters are
  shifted by the local UTC offset; they use `SomeRows` because the result depends on the machine time zone).

- **Test-data adjustments (not failures)**: the `Translations` fixture seeds `DateTime`/`DateTimeOffset` with UTC kind and millisecond precision (MongoDB `Date` has millisecond
  precision and `Unspecified` kind is treated as local time), both for the stored and the expected data. `DateTimeOffset.Now/UtcNow` do not assert MQL because it embeds the current time.

- `MusicStoreMongoTest.Music_store_project_to_mapped_entity` is skipped on EF8 only: the EF8 base test throws from an `async void` body, so the exception escapes the `SpecFails` wrapper (EF-X311).
- `ValueConvertersEndToEndTestBase` has non-public `[ConditionalFact]` helper methods that cannot be overridden, so that class has no `Check_all_tests_overridden`.
### EF-X200 — `Find`/derived-type set over a type hierarchy does not filter by discriminator
Comment pattern: `// Fails: Find through a derived-type set does not filter by discriminator, so a base-type document is materialized as the derived type EF-X200`.
Test-body pattern: `Assert.Throws<InvalidOperationException>(() => base.X())` containing `No discriminators matched`.
Affected: `FindMongoTest.Find_base_type_using_derived_set_from_store` (+ `_async`, run through the set/context/non-generic finders). `Set<DerivedType>().Find(id)` where `id` belongs to a base-type document fails to materialize instead of returning `null`.

### EF-X201 — Explicitly configured / constructor-bound primary keys are not mapped to `_id`
Not tagged on tests: ported fixtures (`FieldMapping`, `WithConstructors`, the `Load`/`ManyToManyLoad`/`LazyLoadProxy` families, `DataBinding`) add the test convention `MongoSpecModelHelpers.MapSinglePropertyKeysToElementId` (or `FixExternalModel`) which sets the key element name to `_id`. Without it model validation throws `The entity type 'X' primary key property 'Id' must be mapped to element '_id'` for models that call `HasKey(...)` explicitly or whose key is found by constructor/field binding. Count 0 because no test asserts it.

### EF-X202 — Explicit transaction cannot be shared by a second context
**Resolved** — no `// Fails:` tags remain and the tests pass. Formerly: the `Update_*` tests in `FieldMappingMongoTest` (EF9/EF10) enlisted a second context by assigning the owner's transaction to `CurrentTransaction` directly, and the session was disposed with the first context (`ObjectDisposedException`, `CoreSessionHandle`). The spec fixtures now call `Database.UseTransaction(transaction)`, which makes the second context a non-owning participant of the shared transaction.

### EF-X203 — Property hiding conflicts in the BSON class map
Comment pattern: `// Fails: Property hiding (new Id on derived type) conflicts in BSON class map EF-X203`.
Affected: the `*_hiding_props` tests in `FieldMappingMongoTest`. `BlogHiding`/`PostHiding` redeclare `Id` with `new`; the BSON class map throws `BsonSerializationException ... cannot use element name '_id' because it is already being used`.

### EF-X205 — `int` keys are not store-generated
Comment pattern: `// Fails: int keys are not store-generated, so a second Id=0 insert hits a duplicate key EF-X205`.
Relational-style upstream models expect `int` keys to be generated on add. The provider has no generator for them, so seeding more than one instance gives duplicate `{Id: 0}`. Ported fixtures that need it (`Load`, `ManyToManyLoad`, `ManyToManyFieldsLoad`, `LazyLoadProxy`, `WithConstructors`) enable `generateIntKeys` in the test convention (an in-process counter generator). Tagged directly on `FieldMappingMongoTest.Can_define_a_backing_field_for_a_navigation_and_query_and_update_it` (EF10), which inserts a second `Id=0` row.

### EF-X206 — Lazy-load/`Single` query over a type hierarchy returns more than one element
Comment pattern: `// Fails: Lazy-load/Single query over a type hierarchy returns more than one element EF-X206`.
Affected: `LoadMongoTest.Lazy_loading_uses_field_access_when_abstract_base_class_navigation`, `LazyLoadProxyMongoTest.Lazy_loading_shares_service_*_property_on_derived_types` (the method name differs between EF versions). Possibly the same root cause as EF-X200 (no discriminator filter); not verified.

### EF-X207 — Unique index over a nullable one-to-one foreign key collides on `null`
Not tagged on tests: the `ManyToManyLoad`/`ManyToManyFieldsLoad`/`DataBinding` fixtures call `ModelBuilder.RelaxUniqueForeignKeyIndexes()` (test plumbing). Without it `createIndexes` fails with `E11000 duplicate key ... { ReferenceInverseId: null }`, because MongoDB unique indexes treat missing/`null` as a value (a partial index is needed).

### EF-X208 — `Include` of a many-to-many skip navigation is not supported
Comment pattern: `// Fails: Include of a many-to-many skip navigation is not supported EF-X208`.
Affected: `Load_collection_already_loaded*`/`*_using_Query_already_loaded*` shapes in `ManyToManyLoadMongoTest` and `ManyToManyFieldsLoadMongoTest` (`Including navigation 'X' is not supported`).

### EF-X209 — Many-to-many skip navigation `Load`/`Query` not translated
Comment pattern: `// Fails: Many-to-many skip navigation SelectMany/NotQuiteInclude (Load/Query) not translated EF-X209`.
Affected: `Load_collection*` tests in `ManyToManyLoadMongoTest`/`ManyToManyFieldsLoadMongoTest`; `CollectionEntry.Load/Query` builds `SelectMany(skip).NotQuiteInclude(...)`, which is not translated (same on EF8/EF9/EF10).

### EF-X211 — Owned-collection ordinal keys not generated for some owned shapes
Not tagged on tests: the `generateIntKeys` test convention also gives non-FK `int` key parts of owned types a counter generator. Without it `Add` of an owner with two owned items throws `The instance of entity type 'Owner.Items#Item' cannot be tracked because another instance with the key value '{Id: 0}'` (seen with `[Owned]`/`OwnsMany` collections in `LazyLoadProxyTestBase` and `WithConstructorsTestBase`, e.g. `List<OwnedAddress>`/lazy-loader owned collections). Cause not isolated; may be limited to these shapes.

### EF-X212 — Reading a document with nested owned types reports a required element missing
Comment pattern: `// Fails: Reading a document with nested owned types reports a required element missing EF-X212`.
Affected: most `LazyLoadProxyMongoTest` tests (57 methods), all `InvalidOperationException: Document element is missing for required non-nullable property 'Rating'`. Querying `Blog` (with nested owned `Person`/`Culture`/`Manufacturer`/`License` types, some reached through several navigations and with proxies) fails on read even though the seed saved without error. Cause not isolated (possibly the same owned type mapped under two navigations with different element names, or a write that omits the nested values). Treat as a possible write/read round-trip bug for nested owned types.

### EF-X213 — A type reached only through a navigation is auto-owned, so `Set<T>()` is rejected
Comment pattern: `// Fails: Type reached only through a navigation is auto-owned, so Set<T>() is rejected EF-X213`.
Affected: `WithConstructorsMongoTest` (`Attaching_entity_sets_lazy_loader*`, `Detaching_entity_resets_lazy_loader*`, ...) and one `LazyLoadProxyMongoTest` test. The tests call `Set<T>()` for a type the provider treats as owned (relational providers treat it as an entity type).

### EF-X214 — Concurrency token on an owned type is rejected
Not tagged on tests: the F1 model (`DataBindingTestBase`) puts concurrency tokens on owned `Location`; the validator throws `NotSupportedException` by design. The ported fixture drops the owned tokens via `ModelBuilder.FixExternalModel(dropOwnedConcurrencyTokens: true)`.

### EF-X215 — `UseSeeding`/`UseAsyncSeeding` delegates are not run by `EnsureCreated`
Not tagged on tests: the F1 fixture seeds through `UseSeeding`/`UseAsyncSeeding`; `MongoTestStore` only seeds via the fixture's `SeedAsync`, and the provider's `EnsureCreated` did not run the delegates (the tests saw 0/2 rows). `DataBindingMongoTest`'s fixture seeds explicitly.

### EF-X216 — Materialization interception creates the base proxy type for derived-type documents
Comment pattern: `// Fails: Materialization interception creates the base proxy type (DriverProxy) for derived-type documents, so the cast to TestDriver fails EF-X216`.
Affected: `DataBindingMongoTest.LocalView_is_initialized_with_entities_from_the_context` (the base declares it non-virtual, so the test is hidden with `new` and skipped; the inherited method fails with `InvalidCastException: DriverProxy -> TestDriver`). `Check_all_tests_overridden` is skipped for that class for the same reason.

### EF-X217 — Keyless query type maps to its own empty collection
Comment pattern: `// Fails: Keyless query type maps to its own empty collection ... EF-X217`.
Affected: `WithConstructorsMongoTest.Query_with_keyless_type` (`BlogQuery` is `HasNoKey()` over the blog table in relational tests; Mongo reads an empty `BlogQuery` collection).
### Tracking / update / graph test bases (EF-X101 – EF-X115)

Ported by `Tracking/*MongoTest.cs` (`CompositeKeyEndToEnd`, `PropertyValues`, `StoreGenerated`, `StoreGeneratedFixup`,
`GraphUpdates`, `ProxyGraphUpdates`, `ManyToManyTracking`, `MonsterFixup`, `OptimisticConcurrency`), `Query/NullKeysMongoTest.cs`
and `Update/UpdatesMongoTest.cs`. The `// Fails:` overrides are inline in each class and
use `SpecFails.Throws[Async]<TException>(...)`, which asserts the exception type and a message fragment and also fails if the test
starts passing. Counts below are distinct override methods (a method split across `#if EF8/EF9/EF10` arms counts once; a theory counts
once however many data rows it has). Only about 40 individual test cases pass as the upstream base on each EF version, out of ~3,250.

Common mechanics, none of which are provider changes: `MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true)` maps every single-property key of the
upstream models to `_id` (otherwise model validation rejects every model) with a client-side integer generator; `RelaxNullableUniqueIndexes` and
`RelaxUniqueForeignKeyIndexes` drop the uniqueness that MongoDB cannot honour for `null` (EF-X207); `DoNotOwnTypes` stops relationship discovery
owning types that the tests model as entities (EF-X102/EF-X538/EF-X539); and `GraphUpdatesMongoTest` and `ProxyGraphUpdatesMongoTest` isolate each test the way the relational providers do: EF begins a transaction on one context and never
commits it, and the `UseTransaction` hook enlists the other contexts a test creates in it (`facade.UseTransaction(transaction)`, #77/#87). Nothing is reseeded, so the
seed data and the model's unique indexes stay in place and the two suites take about 35 seconds per EF version. The other tracking fixtures still reseed before each test.
A failed write aborts the whole MongoDB transaction, so a test that carries on after an expected `SaveChanges` exception would fail on its next operation (unlike SQL Server). `InitFailureGuard` is still available for a fixture whose model/seed/store
creation fails (it re-throws that failure from the first `CreateContext()`); no ported fixture needs it now. `ProxyGraphUpdates` additionally registers
the EF proxy services and uses lazy-loading and change-tracking proxies. A few upstream tests are `public` but not `virtual` (e.g. `PropertyValuesTestBase.Reload_when_entity_deleted_in_store_can_happen_for_any_state`),
so they cannot be overridden; where they fail they are hidden by a `new` method with `Skip` carrying the ticket id.

### EF-X101 — Entities added without a key get `Id` 0, so tracking/saving collides
`// Fails: Entities added without a key get Id 0 (no int/long key value generation), so tracking/saving them collides on the same key EF-X101`.
The upstream models use `int`/`long` keys that relational providers generate. The provider has no generator for them, so a second
entity added with the default key throws `The instance of entity type 'X' cannot be tracked because another instance with the key value '{Id: 0}' is already being tracked`.
This one cause fails the whole of `GraphUpdates` (EF8: 117 methods, EF9/EF10: 127; 1,787 test cases on EF9/EF10), `PropertyValues` (all
tests, via the fixture seed), the `Updates` and `ManyToManyTracking` tests that add generated-key entities.

### EF-X102 — Relationship discovery between root entity types fails at model build
`Unable to determine the relationship represented by navigation 'GameEntity.Game#Game.Levels' of type 'ICollection<Level>'`
(`StoreGeneratedFixup`, all 118 methods) and `'RecordCar.Owner' of type 'RecordPerson'` (`ProxyGraphUpdates`, all 85 methods). The models use
navigations between root entity types that the provider's convention does not pick up as relationships. Related to [EF-216](https://jira.mongodb.org/browse/EF-216) but surfaces at model build rather than in a query.

### EF-X103 — No value generator for `int`/`long`/enum/converted keys
`The property 'Gumball.Id' does not have a value set and no value generator is available for properties of type 'int'`
(`StoreGenerated`; also the `long`-to-`int`, enum, wrapped-int and composite-key-cycle tests, `CompositeKeyEndToEnd` on EF8).

### EF-X104 — Dependent key is null because the principal key was not generated
`Unable to track an entity of type 'UriDependentOptional' because its primary key property 'PrincipalId' is null` and the wrapped `string`/`Uri`/`Guid`
variants (`StoreGenerated`, 4 tests).

### EF-X105 — Duplicate `_id` bulk write
`MongoBulkWriteException ... DuplicateKey ... dup key: { _id: 0 }` when several entities with default `int` keys are saved
(`StoreGenerated` 10 methods, `Updates` and others). Same root as EF-X101 but fails at the store instead of in the change tracker.

### EF-X106 — Unique index build fails on null values
`Command createIndexes failed: Index build failed ... dup key: { SelfFk: null }` while creating the `NullKeys` indexes (unique indexes over nullable
foreign keys with several null values). All 5 `NullKeys` tests.

### EF-X107 — Entity types missing from the model
`The entity type 'WithNoBackingFields' was not found` (also `WithNullableBackingFields`, `WithObjectBackingFields`): the tests expect model types that the
fixture's model does not contain under the provider (8 `StoreGenerated` tests).

### EF-X108 — Store-generated / computed value and update semantics differ
Assertion failures (`Assert.Equal`/`Assert.True`) in `StoreGenerated` (computed/identity values and `After_save_*` behaviors) and `CompositeKeyEndToEnd`
(EF10): the provider has no database-generated defaults or computed columns, so the value the test expects after `SaveChanges` is not populated. Needs per-test triage.

### EF-X110 — `Include` of a skip navigation is not supported
`Including navigation 'Navigation' is not supported.` Every `ManyToManyTracking` test that loads a many-to-many relationship through `Include` (31 methods).

### EF-X111 — Concurrency token on an owned entity
`Property 'Latitude' on entity type 'Location' is configured as a concurrency token, but it belongs to an owned entity.` The `OptimisticConcurrency` (F1) model and the
`MonsterFixup` model both put a concurrency token on an owned type, so the model is rejected and every test fails (31 + 12 methods).

### EF-X112 — Owned many-to-many join entities
`Cannot save instance of 'JoinOneToTwo' because it is an owned entity without any reference to its owner` and `Cannot create a DbSet for 'JoinOneToTwo' because it is configured as an owned entity type` (4 `ManyToManyTracking` tests).

### EF-X113 — Indexer property access not translated
`Expression not supported: p.get_Item("Id")` / `j.get_Item("OneId")`: queries over shared-type (property-bag) entity types use the indexer, which the driver cannot translate (2 `ManyToManyTracking` tests).

### EF-X114 — Partial update reports a concurrency conflict
`DbUpdateConcurrencyException: Conflicts were detected when performing updates to '...Products'` in `Updates.Save_partial_update` (1 test); possibly a real bug in how a partial update of a tracked entity is matched, not just a missing feature.

### EF-X115 — Concurrency message wording
`UpdatesTestBase.Remove_partial_*`/`Update_*` compare the full exception message with the relational text (`The database operation was expected to affect 1 row(s)...`); the provider says `Conflicts were detected when performing updates to ...` (4 tests).

### EF-X116 — Conflicting writes between two contexts are not reported as a concurrency conflict
`OptimisticConcurrencyTestBase.ConcurrencyTestAsync` changes the row through a second context and then saves the first. Here the first save fails with `MongoDB.Driver.MongoCommandException: Command update failed: Caused by :: Write conflict during plan execution and yielding is disabled`, so the `DbUpdateConcurrencyException` the 15 tests expect is never thrown. The 15 wrappers pin the server's `Write conflict` message, so they fail if the provider ever starts translating it. The cause was not investigated further: both contexts write inside the test store's transaction, so it may be how the spec fixtures share a transaction rather than provider behaviour, and a write conflict is in any case not translated to a concurrency exception.

### EF-X117 — Duplicate key is not reported as `DbUpdateException`
`OptimisticConcurrencyMongoTest.Adding_the_same_entity_twice_results_in_DbUpdateException` expects a `DbUpdateException`; the provider lets the driver's `MongoBulkWriteException` (duplicate key) escape. The cause was not investigated further.

Tests that also appear under existing tickets: cross-document queries in `ManyToManyTracking` carry [EF-216](https://jira.mongodb.org/browse/EF-216) (8 methods) and the EF8/EF9
`The LINQ expression 'DbSet<...>' could not be translated` failures in `Updates`/`StoreGenerated` carry EF-X020 (6 methods).

## Audit findings — tagging hygiene

The following inconsistencies were observed while building this inventory and have since been resolved.

### 1. `EF-232` was reused for two distinct failure modes — **fixed**

`Sum_with_no_data_cast_to_nullable` kept `EF-232` (since fixed and removed from the table above); `Compiled_query_when_does_not_end_in_query_operator` was re-tagged with the new temp ticket `EF-X011`.

### 2. Sibling `#if` branches missing the `// Fails:` tag — **fixed**

Both branches of `Any_on_distinct`, `Contains_on_distinct`, `All_on_distinct`, `IQueryable_captured_variable`, and `Where_Order_First` in `NorthwindMiscellaneousQueryMongoTest.cs` now carry the `EF-216` tag. (`Filtered_include_with_multiple_ordering` in `NorthwindStringIncludeQueryMongoTest.cs` was verified to pass — it does not need a tag despite the sibling-file convention; the audit's flag was a false positive there.)

### 3. Duplicate `EF-243` references on the same method — **fixed**

The duplicate inline `// ... See EF-243.` mentions in the three StartsWith/Contains/EndsWith overrides were dropped; the `// Fails: ... EF-243` lines remain.

### 4. Single-mode helpers tag at the helper, not at each call site — **fixed**

The convention: when a helper method wraps a single failure mode, the `// Fails:` line goes above the helper declaration, not at every call site. Applied to:

- `AssertNoMultiCollectionQuerySupport` (single mode: `EF-216`) — definition now tagged in every file that declares it; call sites left untagged. This was already the dominant pattern in 8 of 9 files; the one missing tag in `NorthwindGroupByQueryMongoTest.cs` was added.
- `AssertGroupByUnsupported` (single mode: `EF-149`) — already tagged at the helper.

The generic `AssertTranslationFailed` helper is **not** single-mode (it's used for many distinct failure causes) and is therefore tagged per call site.

### 5. Untagged `AssertTranslationFailed` and Throws callers — **fixed**

All ~70 `AssertTranslationFailed` call sites now carry a `// Fails:` tag — categorized as `EF-117` (joins/Include), `EF-149` (GroupBy), `EF-216` (cross-document), `EF-X001` (sub-query selection), or one of the new EF-X011–EF-X015 tickets. Three additional `Assert.Throws*` blocks (`Type_casting_inside_sum`, `Late_subquery_pushdown`, `Where_nanosecond_and_microsecond_component`) were also tagged. Two malformed tags (`// Fails ` without colon) were corrected. `Assert.Throws<...>` blocks that preserve EF Core's own expected throws (`Max_on_empty_sequence_throws`, `Client_code_using_instance_*_throws`, `VectorSearch_throws_if_num_candidates_set_for_exact_search`) intentionally remain untagged — they assert the provider matches EF behavior, not that it fails.

### Verification

After all edits:

```
grep -rEho "(EF-X?[0-9]+|CSHARP-[0-9]+)" tests/.../SpecificationTests --include="*.cs" \
  | sort | uniq -c | sort -rn
```

returns the ids tabulated above, including the temporary `EF-X###` placeholders (`EF-X016` is the `NorthwindBulkUpdatesMongoTest` out-of-subset rejections; `EF-X023` is GroupJoin, previously a duplicate use of `EF-X016`). The static audit (lookback-5 search for `// Fails:` above any `AssertTranslationFailed` / `Assert.Throws*` / `AssertGroupByUnsupported` callsite, excluding helper definitions and single-mode helper callers) reports zero remaining holes among Mongo-failure cases.
